using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CheatDeath.CheatDeath;

namespace CheatDeath
{
    internal class SE_CheatDeath : SE_Stats
    {
        public const string statusEffectName = "CheatDeath";
        public static readonly int statusEffectHash = statusEffectName.GetStableHashCode();

        [NonSerialized]
        public bool m_initialized = false;

        public override void UpdateStatusEffect(float dt)
        {
            if (!modEnabled.Value)
                Stop();

            base.UpdateStatusEffect(dt);

            m_cooldownIcon = m_time > protectionSeconds.Value;
            m_flashIcon = !m_cooldownIcon;

            if (m_cooldownIcon)
            {
                m_runStaminaDrainModifier = 0f;
                m_jumpStaminaUseModifier = 0f;
                
                m_fallDamageModifier = 0f;
                m_maxMaxFallSpeed = 0f;
                
                m_healthOverTime = 0;
                m_healthPerTick = 0;
                m_addMaxCarryWeight = 0;

                m_mods.Clear();
            }

            if (!m_initialized && m_character != null)
            {
                m_initialized = true;

                if (cleanseOnProc.Value)
                {
                    List<StatusEffect> effectsToRemove = new List<StatusEffect>();
                    foreach (StatusEffect se in m_character.GetSEMan().GetStatusEffects())
                    {
                        if (se is SE_Burning || se is SE_Poison || se is SE_Puke)
                            effectsToRemove.Add(se);
                    }

                    foreach (StatusEffect se in effectsToRemove)
                        m_character.GetSEMan().RemoveStatusEffect(se);
                }

                if (m_character != null && m_character == Player.m_localPlayer)
                    if (CooldownData.IsOnCooldown())
                        m_time = Mathf.Max(0, m_ttl - (float)CooldownData.GetCooldown());
                    else
                        CooldownData.SetCooldown(m_ttl);
            }
        }

        public static void UpdateConfigurableValues(SE_CheatDeath statusEffect = null)
        {
            if (!ObjectDB.instance)
                return;

            bool global = statusEffect == null;
            if (global)
                statusEffect = ObjectDB.instance.GetStatusEffect(statusEffectHash) as SE_CheatDeath;

            if (statusEffect == null)
                return;

            SetStatusEffectProperties(statusEffect);

            if (global && Player.m_localPlayer && Player.m_localPlayer.GetSEMan().HaveStatusEffect(statusEffectHash))
                SetStatusEffectProperties(Player.m_localPlayer.GetSEMan().GetStatusEffect(statusEffectHash) as SE_CheatDeath);
        }

        private static void SetStatusEffectProperties(SE_CheatDeath statusEffect)
        {
            statusEffect.m_name = statusEffectLocalization.Value;
            statusEffect.m_ttl = cooldown.Value;
            statusEffect.m_healthOverTimeInterval = protectionSeconds.Value;

            statusEffect.m_runStaminaDrainModifier = staminaModifier.Value;
            statusEffect.m_jumpStaminaUseModifier = staminaModifier.Value;

            statusEffect.m_fallDamageModifier = fallDamageModifier.Value;
            statusEffect.m_maxMaxFallSpeed = maxMaxFallSpeed.Value;

            statusEffect.m_healthOverTime = healthOverTime.Value;

            statusEffect.m_healthPerTick = healthPerSecond.Value;
            statusEffect.m_tickInterval = 1f;

            statusEffect.m_addMaxCarryWeight = addMaxCarryWeight.Value;

            statusEffect.m_mods.Clear();

            Enum.GetValues(typeof(HitData.DamageType)).Cast<HitData.DamageType>().Where(e => protectionDamageType.Value.HasFlag(e)).Do(damageType =>
            {
                statusEffect.m_mods.Add(new HitData.DamageModPair()
                {
                    m_type = damageType,
                    m_modifier = protectionModifier.Value
                });
            });
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        public static class ObjectDB_Awake_AddStatusEffects
        {
            public static void AddCustomStatusEffects(ObjectDB odb)
            {
                if (odb.m_StatusEffects.Count > 0)
                {
                    StatusEffect softDeath = odb.m_StatusEffects.Find(se => se.name == "SoftDeath");

                    if (!odb.m_StatusEffects.Any(se => se.name == statusEffectName))
                    {
                        SE_CheatDeath statusEffect = ScriptableObject.CreateInstance<SE_CheatDeath>();
                        statusEffect.name = statusEffectName;
                        statusEffect.m_nameHash = statusEffectHash;
                        statusEffect.m_icon = softDeath.m_icon;
                        statusEffect.m_tooltip = "$tutorial_death_topic";

                        SetStatusEffectProperties(statusEffect);

                        odb.m_StatusEffects.Add(statusEffect);
                    }
                }
            }

            private static void Postfix(ObjectDB __instance)
            {
                AddCustomStatusEffects(__instance);
            }
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        public static class ObjectDB_CopyOtherDB_SE_Season
        {
            private static void Postfix(ObjectDB __instance)
            {
                ObjectDB_Awake_AddStatusEffects.AddCustomStatusEffects(__instance);
            }
        }
    }
}
