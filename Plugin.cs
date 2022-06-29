using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SkillManager;
using ServerSync;

namespace GatheringSkill
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class GatheringSkillPlugin : BaseUnityPlugin
    {
        internal const string ModName = "GatheringSkill";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "grisch";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";

        public static ConfigEntry<Toggle> enabled = null!;
        public static ConfigEntry<Toggle> changingDropAmmounts = null!;
        public static ConfigEntry<DropMode> mode = null!;
        public static ConfigEntry<int> maxMultiplier = null!;
        public static ConfigEntry<float> experienceGainedFactor = null!;
        public static ConfigEntry<string> includePickables = null!;
        public static ConfigEntry<Toggle> enableTimeEstimate = null!;
        public static ConfigEntry<int> showSimpleEstimateLevel = null!;
        public static ConfigEntry<int> showDetailedEstimateLevel = null!;
        
        public static readonly ManualLogSource GatheringSkillLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        
        public void Awake()
        {
            _serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            Skill gathering = new("Gathering",
                    "gathering.png"); // Skill name along with the skill icon. By default the icon is found in the icons folder. Put it there if you wish to load one.

            gathering.Description.English("Increases resources gained from foraging.");
            gathering.Configurable = false;

            enabled = config("1 - General", "Enabled", Toggle.On,"Enables/Disables the entire mod.");
            
            // Drops
            changingDropAmmounts = config("2 - Drops", "Enable changing drop amounts", Toggle.On,
                "Disable if you only want to check on the progress of pickables");
            mode = config("2 - Drops", "Drop Rate Increase Mode", DropMode.PartialRandom, "How additional drops are handled.");
            
            maxMultiplier = config("2 - Drops", "Max Multiplier", 3,
                new ConfigDescription("Maximum drop multiplier (at level 100).", new AcceptableValueRange<int>(1, 10)));
            
            // Progression
            experienceGainedFactor = config("3 - Progression", "Skill Experience Gain Factor", 1f, new ConfigDescription("Factor for experience gained for the gathering skill.", new AcceptableValueRange<float>(0.01f, 5f)));
            includePickables = config("3 - Progression", "Pickables to Include", "Pickable_Dandelion, Pickable_Flint, Pickable_Mushroom, Pickable_Mushroom_yellow, Pickable_Stone, Pickable_Thistle, Pickable_Branch, RaspberryBush, BlueBerryBush, CloudberryBush, apple_tree, apple_tree_1",
                "List of comma separated pickables to include"); //figure out how we can do this better
            // Time Estimate
            enableTimeEstimate = config("4 - Time Estimate", "Time Estimate", Toggle.On, "Enable showing estimates. Disable this if you have another mod you want to use estimates from.");
            showSimpleEstimateLevel = config("4 - Simple Estimate", "Simple Estimate Level", 1,
                new ConfigDescription("Level at which to show simple time estimates",
                    new AcceptableValueRange<int>(1, 100)));
            showDetailedEstimateLevel = config("4 - Detailed Estimate", "Detailed Estimate Level", 1,
                new ConfigDescription("Level at which to show detailed time estimates",
                    new AcceptableValueRange<int>(1, 100)));
            
            Assembly assembly = Assembly.GetExecutingAssembly();
            Harmony harmony = new(ModGUID);
            harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                GatheringSkillLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                GatheringSkillLogger.LogError($"There was an issue loading your {ConfigFileName}");
                GatheringSkillLogger.LogError("Please check your config entries for spelling and format!");
            }
        }
        
        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public enum DropMode
        {
            Linear,
            Random,
            PartialRandom
        }


        #region ConfigOptions

        private static ConfigEntry<bool>? _serverConfigLocked;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }

        #endregion
    }
}