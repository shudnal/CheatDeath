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
        
        public const string vfx_CheatDeathName = "vfx_CheatDeath";
        public static readonly int vfx_CheatDeathHash = vfx_CheatDeathName.GetStableHashCode();

        public static Sprite iconStatusEffect;

        public static GameObject vfx_CheatDeath;

        [NonSerialized]
        public bool m_freeProc = false;

        [NonSerialized]
        public bool m_initialized = false;

        public void RollFreeProc() => m_freeProc = UnityEngine.Random.Range(0f, 1f) <= chanceForReproc.Value;

        public override void ResetTime()
        {
            base.ResetTime();

            RollFreeProc();

            MessageCharacter();

            TriggerStartEffects();
        }

        public override void UpdateStatusEffect(float dt)
        {
            if (!modEnabled.Value)
                Stop();

            base.UpdateStatusEffect(dt);

            if (m_cooldownIcon != (m_cooldownIcon = m_time > protectionSeconds.Value) && m_cooldownIcon && m_freeProc)
                CooldownData.SetCooldown(m_ttl - (m_time = m_ttl));

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

                MessageCharacter();

                TriggerStartEffects();
            }
        }

        private void MessageCharacter()
        {
            if (!string.IsNullOrEmpty(m_startMessage))
            {
                List<string> messages = new List<string>();

                if (m_freeProc)
                {
                    messages.AddRange(reprocMessage.Value.Split(new string[] { "&&" }, StringSplitOptions.RemoveEmptyEntries));
                }
                else
                {
                    messages.AddRange(statusEffectStartMessageServer.Value.Split(new string[] { "&&" }, StringSplitOptions.RemoveEmptyEntries));
                    messages.AddRange(statusEffectStartMessageClient.Value.Split(new string[] { "&&" }, StringSplitOptions.RemoveEmptyEntries));
                }

                if (messages.Count == 0)
                    m_character.Message(m_startMessageType, m_startMessage);
                else
                    m_character.Message(m_startMessageType, messages[UnityEngine.Random.Range(0, messages.Count)]);
            }
        }

        public override void Setup(Character character)
        {
            RollFreeProc();

            SetStatusEffectProperties(this);

            m_character = character;
        }

        public override bool CanAdd(Character character)
        {
            return !character.GetSEMan().HaveStatusEffect(statusEffectHash);
        }

        public float GetCharacterHealth() => healToThreshold.Value ? GetHealthThresholdValue(m_character) : Mathf.Min(m_character.GetHealth(), GetHealthThresholdValue(m_character));

        private static float GetHealthThresholdValue(Character character) => healthThreshold.Value == HealthThreshold.Percent ? character.GetMaxHealth() * (healthThresholdPercent.Value / 100f) : healthThresholdValue.Value;

        public static void RegisterEffects()
        {
            if (!ZNetScene.instance)
                return;

            if (!(bool)vfx_CheatDeath)
            {
                bool defaultEffect = string.IsNullOrEmpty(vfxOverride.Value);

                vfx_CheatDeath = CustomPrefabs.InitPrefabClone(ZNetScene.instance.GetPrefab(defaultEffect ? "vfx_HealthUpgrade" : vfxOverride.Value), vfx_CheatDeathName);

                if (defaultEffect)
                {
                    vfx_CheatDeath.transform.localPosition = Vector3.zero;
                    for (int i = vfx_CheatDeath.transform.childCount - 1; i >= 0; i--)
                    {
                        Transform child = vfx_CheatDeath.transform.GetChild(i);
                        switch (child.name)
                        {
                            case "Particle System _expl":
                            case "smoke _expl":
                            case "trails _expl":
                            case "sfx_expl":
                                child.parent = null;
                                UnityEngine.Object.Destroy(child.gameObject);
                                break;
                            case "Particle System":
                            case "trails":
                            case "smoke":
                                child.localPosition -= new Vector3(0f, 1f, 0f);
                                break;
                        }
                    }
                }
            }

            if ((bool)vfx_CheatDeath && !ZNetScene.instance.m_namedPrefabs.ContainsKey(vfx_CheatDeathHash))
            {
                ZNetScene.instance.m_prefabs.Add(vfx_CheatDeath);
                ZNetScene.instance.m_namedPrefabs[vfx_CheatDeathHash] = vfx_CheatDeath;
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
                RegisterEffects();

                if (odb?.m_StatusEffects.Count > 0)
                {
                    if (!odb.m_StatusEffects.Any(se => se?.NameHash() == statusEffectHash))
                    {
                        SE_CheatDeath statusEffect = ScriptableObject.CreateInstance<SE_CheatDeath>();
                        statusEffect.name = statusEffectName;
                        statusEffect.m_nameHash = statusEffectHash;
                        statusEffect.m_icon = iconStatusEffect;
                        statusEffect.m_tooltip = "$tutorial_death_topic";

                        statusEffect.m_startMessageType = MessageHud.MessageType.Center;
                        statusEffect.m_startMessage = "$tutorial_death_topic";

                        if (vfx_CheatDeath)
                        {
                            statusEffect.m_startEffects.m_effectPrefabs = statusEffect.m_startEffects.m_effectPrefabs.AddToArray(new EffectList.EffectData
                            {
                                m_prefab = vfx_CheatDeath,
                                m_enabled = true,
                                m_variant = -1,
                                m_attach = true,
                                m_scale = true
                            });
                        }

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
        public static class ObjectDB_CopyOtherDB_AddStatusEffects
        {
            private static void Postfix(ObjectDB __instance)
            {
                ObjectDB_Awake_AddStatusEffects.AddCustomStatusEffects(__instance);
            }
        }
    }
}
