using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ServerSync;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CheatDeath
{
    [BepInPlugin(pluginID, pluginName, pluginVersion)]
    public class CheatDeath : BaseUnityPlugin
    {
        public const string pluginID = "shudnal.CheatDeath";
        public const string pluginName = "Cheat Death";
        public const string pluginVersion = "1.0.5";

        private readonly Harmony harmony = new Harmony(pluginID);

        internal static readonly ConfigSync configSync = new ConfigSync(pluginID) { DisplayName = pluginName, CurrentVersion = pluginVersion, MinimumRequiredVersion = pluginVersion };

        internal static CheatDeath instance;

        public static ConfigEntry<bool> modEnabled;
        internal static ConfigEntry<bool> configLocked;
        internal static ConfigEntry<bool> loggingEnabled;

        internal static ConfigEntry<CooldownTime> cooldownTime;
        internal static ConfigEntry<int> cooldown;

        internal static ConfigEntry<float> protectionSeconds;
        internal static ConfigEntry<string> statusEffectLocalization;
        internal static ConfigEntry<bool> cleanseOnProc;
        internal static ConfigEntry<string> statusEffectStartMessageServer;
        internal static ConfigEntry<string> statusEffectStartMessageClient;
        internal static ConfigEntry<float> chanceForReproc;
        internal static ConfigEntry<string> reprocMessage;
        internal static ConfigEntry<string> vfxOverride;

        internal static ConfigEntry<bool> healToThreshold;
        internal static ConfigEntry<float> healthThresholdPercent;
        internal static ConfigEntry<float> healthThresholdValue;
        internal static ConfigEntry<HealthThreshold> healthThreshold;

        internal static ConfigEntry<HitData.DamageModifier> protectionModifier;
        internal static ConfigEntry<HitData.DamageType> protectionDamageType;
        internal static ConfigEntry<float> staminaModifier;
        internal static ConfigEntry<float> healthOverTime;
        internal static ConfigEntry<float> healthPerSecond;
        internal static ConfigEntry<float> addMaxCarryWeight;
        internal static ConfigEntry<float> maxMaxFallSpeed;
        internal static ConfigEntry<float> fallDamageModifier;
        
        public enum CooldownTime
        {
            WorldTime,
            GlobalTime
        }

        public enum HealthThreshold
        {
            Percent,
            HealthPoints
        }

        private void Awake()
        {
            harmony.PatchAll();

            instance = this;

            ConfigInit();
            _ = configSync.AddLockingConfigEntry(configLocked);

            Game.isModded = true;

            LoadIcons();

            CustomConfigs.Awake();
        }

        public void ConfigInit()
        {
            config("General", "NexusID", 2854, "Nexus mod ID for updates", false);

            modEnabled = Config.Bind("General", "Enabled", defaultValue: true, "Enable the mod.");
            configLocked = config("General", "Lock Configuration", defaultValue: true, "Configuration is locked and can be changed by server admins only.");
            loggingEnabled = config("General", "Logging enabled", defaultValue: false, "Enable logging. [Not Synced with Server]", false);

            cooldownTime = config("Status effect - Cooldown", "Time", defaultValue: CooldownTime.WorldTime, "Time type to calculate cooldown between game sessions." +
                                                                                                          "\nWorld time - calculate from time passed in game world" +
                                                                                                          "\nGlobal time - calculate from real world time");
            cooldown = config("Status effect - Cooldown", "Cooldown", defaultValue: 600, "Cooldown to be set after proc");
            
            cooldown.SettingChanged += (sender, args) => SE_CheatDeath.UpdateConfigurableValues();

            protectionSeconds = config("Status effect - General", "Protection seconds", defaultValue: 10f, "Seconds of protection after activation");
            statusEffectLocalization = config("Status effect - General", "Name", defaultValue: "Cheat death", "Name of status effect");
            cleanseOnProc = config("Status effect - General", "Cleanse of DoT on proc", defaultValue: true, "Remove DoT effects on proc");
            statusEffectStartMessageServer = config("Status effect - General", "Message on proc synced", defaultValue: "$tutorial_death_topic&&YOU DIED!.. But not yet.",
                    new ConfigDescription("&& separated messages showing on effect proc which is synced from server", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings("&&") }));
            statusEffectStartMessageClient = config("Status effect - General", "Message on proc local", defaultValue: "Did it hurt? I guess so...",
            new ConfigDescription("&& separated messages showing on effect proc [Not Synced with Server]", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings("&&") }), false);
            chanceForReproc = config("Status effect - General", "Change for free proc", defaultValue: 0.05f, 
                    new ConfigDescription("Chance for status effect to not going on cooldown on proc. Effect can also proc again while current effect is active.", 
                    new AcceptableValueRange<float>(0, 1), new CustomConfigs.ConfigurationManagerAttributes { ShowRangeAsPercent = true }));
            reprocMessage = config("Status effect - General", "Message on free proc", defaultValue: "$msg_softdeath",
                    new ConfigDescription("&& separated messages showing on effect reproc", null, new CustomConfigs.ConfigurationManagerAttributes { CustomDrawer = CustomConfigs.DrawSeparatedStrings("&&") }));
            vfxOverride = config("Status effect - General", "Visual effect prefab override", defaultValue: "", "Name of visual effect prefab available via ZNetScene.instance.GetPrefab to use instead of default cheat death effect.");

            protectionSeconds.SettingChanged += (sender, args) => SE_CheatDeath.UpdateConfigurableValues();
            statusEffectLocalization.SettingChanged += (sender, args) => SE_CheatDeath.UpdateConfigurableValues();

            healthThresholdPercent = config("Status effect - Health threshold", "Health threshold in percents", defaultValue: 5f, new ConfigDescription("Percent of hp left on proc.", new AcceptableValueRange<float>(1f, 100f)));
            healthThresholdValue = config("Status effect - Health threshold", "Health threshold in health points", defaultValue: 10f, "Health points left on proc.");
            healthThreshold = config("Status effect - Health threshold", "Health threshold", defaultValue: HealthThreshold.Percent, "What value to use, percentage or fixed amount.");
            healToThreshold = config("Status effect - Health threshold", "Heal to health threshold", defaultValue: true, "If damage taken while current hp is less then threshold hp will be instantly restored to threshold value." +
                                                                                                                        "\nIf disabled then if you had more hp than threshold hp will be reduced to threshold" +
                                                                                                                        "\nand if you had less hp then threshold your HP will remain the same.");

            protectionModifier = config("Status effect - Protection", "Damage modifier", defaultValue: HitData.DamageModifier.Resistant, "Resistant = 50% VeryResistant = 75%");
            protectionDamageType = config("Status effect - Protection", "Damage types", defaultValue: (HitData.DamageType)999, "Set type of damage to be protected from");
            fallDamageModifier = config("Status effect - Protection", "Fall damage protection", defaultValue: -0.75f, "Multiplier of fall damage");
            maxMaxFallSpeed = config("Status effect - Protection", "Max fall speed", defaultValue: 6f, "Slow Fall given by Feather Cape is 5.");
            staminaModifier = config("Status effect - Protection", "Movement stamina modifier", defaultValue: -0.75f, "Multiplier of jump and run stamina drain");
            healthOverTime = config("Status effect - Protection", "Health over time", defaultValue: 0f, "Amount of health to be healed in protection duration");
            healthPerSecond = config("Status effect - Protection", "Health per second", defaultValue: 1f, "Health per second while protection is active");
            addMaxCarryWeight = config("Status effect - Protection", "Add max carry weight", defaultValue: 50f, "Amount of max carry weight added while protection is active");

            protectionModifier.SettingChanged += (sender, args) => SE_CheatDeath.UpdateConfigurableValues();
            protectionDamageType.SettingChanged += (sender, args) => SE_CheatDeath.UpdateConfigurableValues();
            fallDamageModifier.SettingChanged += (sender, args) => SE_CheatDeath.UpdateConfigurableValues();
            maxMaxFallSpeed.SettingChanged += (sender, args) => SE_CheatDeath.UpdateConfigurableValues();
            staminaModifier.SettingChanged += (sender, args) => SE_CheatDeath.UpdateConfigurableValues();
            healthOverTime.SettingChanged += (sender, args) => SE_CheatDeath.UpdateConfigurableValues();
            healthPerSecond.SettingChanged += (sender, args) => SE_CheatDeath.UpdateConfigurableValues();
            addMaxCarryWeight.SettingChanged += (sender, args) => SE_CheatDeath.UpdateConfigurableValues();

            InitCommands();
        }

        private void OnDestroy()
        {
            Config.Save();
            instance = null;
            harmony?.UnpatchSelf();
        }

        public static void InitCommands()
        {
            new Terminal.ConsoleCommand("setcheatdeathcooldown", "seconds", delegate (Terminal.ConsoleEventArgs args)
            {
                CooldownData.SetCooldown(args.TryParameterInt(1, 0));
                if (Player.m_localPlayer && Player.m_localPlayer.GetSEMan().HaveStatusEffect(SE_CheatDeath.statusEffectHash))
                    UpdateStatusEffectTime(Player.m_localPlayer.GetSEMan().GetStatusEffect(SE_CheatDeath.statusEffectHash));

            }, isCheat: true);
        }
        private void LoadIcons()
        {
            LoadIcon("CheatDeath.png", ref SE_CheatDeath.iconStatusEffect);
        }

        internal static void LoadIcon(string filename, ref Sprite icon)
        {
            Texture2D tex = new Texture2D(2, 2);
            if (LoadTexture(filename, ref tex))
                icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
        }

        internal static bool LoadTexture(string filename, ref Texture2D tex)
        {
            string fileInConfigFolder = Path.Combine(Paths.PluginPath, filename);
            if (File.Exists(fileInConfigFolder))
            {
                LogInfo($"Loaded image: {fileInConfigFolder}");
                return tex.LoadImage(File.ReadAllBytes(fileInConfigFolder));
            }

            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            string name = executingAssembly.GetManifestResourceNames().Single(str => str.EndsWith(filename));

            Stream resourceStream = executingAssembly.GetManifestResourceStream(name);

            byte[] data = new byte[resourceStream.Length];
            resourceStream.Read(data, 0, data.Length);

            tex.name = Path.GetFileNameWithoutExtension(filename);

            return tex.LoadImage(data, true);
        }

        public static void LogInfo(object data)
        {
            if (loggingEnabled.Value)
                instance.Logger.LogInfo(data);
        }

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, defaultValue, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, string description, bool synchronizedSetting = true) => config(group, name, defaultValue, new ConfigDescription(description), synchronizedSetting);

        [HarmonyPatch(typeof(Character), nameof(Character.SetHealth))]
        public static class Character_SetHealth_CheatDeathActivation
        {
            [HarmonyPriority(Priority.First)]
            public static void Prefix(Character __instance, ref float health)
            {
                if (!modEnabled.Value)
                    return;

                if (!__instance.IsPlayer() || !Player.m_localPlayer || __instance != Player.m_localPlayer)
                    return;

                if (health > 0.1f)
                    return;

                bool resetTime = false;
                if (__instance.GetSEMan().GetStatusEffect(SE_CheatDeath.statusEffectHash) is SE_CheatDeath statusEffect)
                    if (resetTime = statusEffect.m_freeProc)
                        health = statusEffect.GetCharacterHealth();

                if (__instance.GetSEMan().AddStatusEffect(SE_CheatDeath.statusEffectHash, resetTime) is SE_CheatDeath cheatDeath)
                    health = cheatDeath.GetCharacterHealth();
            }
        }

        private static void UpdateStatusEffectTime(StatusEffect statusEffect)
        {
            if (statusEffect == null)
                return;

            statusEffect.m_time = Mathf.Max(0, statusEffect.m_ttl - (float)CooldownData.GetCooldown());
            (statusEffect as SE_CheatDeath).m_initialized = true;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Save))]
        public static class Player_Save_SaveCheatDeathCooldown
        {
            private static void Prefix(Player __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (!Player.m_localPlayer || Player.m_localPlayer != __instance)
                    return;

                StatusEffect statusEffect = __instance.m_seman.GetStatusEffect(SE_CheatDeath.statusEffectHash);
                if (statusEffect == null)
                    return;

                CooldownData.SetCooldown(statusEffect.m_ttl - statusEffect.m_time);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Load))]
        public static class Player_Load_LoadCheatDeathCooldown
        {
            private static void Postfix(Player __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (!Player.m_localPlayer || Player.m_localPlayer != __instance)
                    return;

                if (!CooldownData.IsOnCooldown())
                    return;

                StatusEffect statusEffect = Player.m_localPlayer.GetSEMan().AddStatusEffect(SE_CheatDeath.statusEffectHash);
                UpdateStatusEffectTime(statusEffect);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        public static class Player_OnSpawned_LoadCheatDeathCooldown
        {
            private static void Postfix(Player __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (!Player.m_localPlayer || Player.m_localPlayer != __instance)
                    return;

                if (!CooldownData.IsOnCooldown())
                    return;

                StatusEffect statusEffect = Player.m_localPlayer.GetSEMan().AddStatusEffect(SE_CheatDeath.statusEffectHash);
                UpdateStatusEffectTime(statusEffect);
            }
        }
        
    }
}
