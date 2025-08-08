using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using LethalLevelLoader;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace InteriorTitleCards.Config
{
    public class ConfigManager
    {
        private readonly ConfigFile config;
        private readonly ManualLogSource logger;
        private readonly Plugin plugin;
        
        // Config entries
        private ConfigEntry<string> titleColorConfig;
        private ConfigEntry<bool> debugLoggingConfig;
        private ConfigEntry<string> customTopTextConfig;
        private ConfigEntry<int> topTextFontWeightConfig;
        private ConfigEntry<int> interiorTextFontWeightConfig;
        private Dictionary<string, ConfigEntry<string>> interiorNameOverrideConfigs = new Dictionary<string, ConfigEntry<string>>();
        
        private int retryAttempt = 0;
        
                public Color TitleColor
        {
            get
            {
                if (titleColorConfig != null && ColorUtility.TryParseHtmlString(titleColorConfig.Value, out Color color))
                {
                    return color;
                }
                return Color.white; // Default color if parsing fails
            }
        }
        public bool DebugLoggingEnabled => debugLoggingConfig?.Value ?? false;
        public string CustomTopText => customTopTextConfig?.Value ?? "NOW ENTERING...";
        public int TopTextFontWeight => topTextFontWeightConfig?.Value ?? 400; // Default to Normal
        public int InteriorTextFontWeight => interiorTextFontWeightConfig?.Value ?? 700; // Default to Bold
        
        public ConfigManager(ConfigFile config, ManualLogSource logger, Plugin plugin)
        {
            this.config = config;
            this.logger = logger;
            this.plugin = plugin;
        }
        
        public void Initialize()
        {
            // Initialize configs after LethalLevelLoader has loaded its content
            plugin.StartCoroutine(InitializeConfigsDelayed());
        }
        
        private IEnumerator InitializeConfigsDelayed()
        {
            // Wait a few seconds for LethalLevelLoader to initialize
            yield return new WaitForSeconds(5f);
            
            BindConfig();
        }
        
        private void BindConfig()
        {
            // Bind config entries
            titleColorConfig = config.Bind(
                "Style Settings",
                "TitleColor",
                "#fe6001",
                "Color of the title card text in hex format (e.g., #fe6001)"
            );
            
            debugLoggingConfig = config.Bind(
                "Debug Settings",
                "EnableDebugLogging",
                false,
                "Enable detailed debug logging for troubleshooting"
            );
            
            customTopTextConfig = config.Bind(
                "Style Settings",
                "Top text override",
                "NOW ENTERING...",
                "Custom text displayed above the interior name (leave blank to use default 'NOW ENTERING...')"
            );

            topTextFontWeightConfig = config.Bind(
                "Style Settings",
                "TopTextFontWeight",
                400, // Corresponds to FontStyles.Normal
                "Font weight (boldness) for the top text (e.g., 400 for normal, 700 for bold)"
            );

            interiorTextFontWeightConfig = config.Bind(
                "Style Settings",
                "InteriorTextFontWeight",
                700, // Corresponds to FontStyles.Bold
                "Font weight (boldness) for the interior name text (e.g., 400 for normal, 700 for bold)"
            );
            
            LogDebug("Starting config binding process");
            
            // Create config entries for all known interiors
            CreateInteriorNameOverrideConfigs();
        }
        
        public string GetInteriorNameOverride(string dungeonName)
        {
            if (interiorNameOverrideConfigs.ContainsKey(dungeonName))
            {
                string overrideName = interiorNameOverrideConfigs[dungeonName].Value;
                if (!string.IsNullOrEmpty(overrideName))
                {
                    return overrideName;
                }
            }
            return dungeonName;
        }
        
        private void CreateInteriorNameOverrideConfigs()
        {
            try
            {
                LogDebug("Starting to create interior name override configs by reading LethalLevelLoader.cfg");
                
                // Try to read dungeon names from LethalLevelLoader.cfg using proper BepInEx config path
                string lethalLevelLoaderConfigPath = Path.Combine(Paths.ConfigPath, "LethalLevelLoader.cfg");
                
                LogDebug($"Looking for LethalLevelLoader.cfg at: {lethalLevelLoaderConfigPath}");
                
                if (!File.Exists(lethalLevelLoaderConfigPath))
                {
                    // Try alternative path
                    LogDebug("LethalLevelLoader.cfg not found at primary path, trying alternative path");
                    lethalLevelLoaderConfigPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "BepInEx/config/LethalLevelLoader.cfg");
                    LogDebug($"Looking for LethalLevelLoader.cfg at alternative path: {lethalLevelLoaderConfigPath}");
                }
                
                if (!File.Exists(lethalLevelLoaderConfigPath))
                {
                    // Try Resources directory
                    LogDebug("LethalLevelLoader.cfg not found at alternative path, trying Resources directory");
                    lethalLevelLoaderConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../Resources/LethalLevelLoader.cfg");
                    LogDebug($"Looking for LethalLevelLoader.cfg at Resources path: {lethalLevelLoaderConfigPath}");
                }
                
                if (!File.Exists(lethalLevelLoaderConfigPath))
                {
                    LogDebug("LethalLevelLoader.cfg not found, trying runtime API approach");
                    CreateInteriorNameOverrideConfigsFromRuntimeAPI();
                    return;
                }
                
                LogDebug($"Found LethalLevelLoader.cfg at: {lethalLevelLoaderConfigPath}");
                
                string[] lines = File.ReadAllLines(lethalLevelLoaderConfigPath);
                HashSet<string> dungeonNames = new HashSet<string>();
                
                // Parse the config file to find dungeon names
                foreach (string line in lines)
                {
                    // Look for lines like "[Custom Dungeon: Art Gallery]"
                    if (line.StartsWith("[​​​​​​​​​Custom Dungeon:  ") && line.EndsWith("]"))
                    {
                        string dungeonName = line.Substring(17, line.Length - 18); // Extract name between brackets
                        if (!string.IsNullOrEmpty(dungeonName))
                        {
                            dungeonNames.Add(dungeonName);
                            LogDebug($"Found custom dungeon: {dungeonName}");
                        }
                    }
                    // Look for vanilla dungeons too
                    else if (line.StartsWith("[Vanilla Dungeon:  ") && line.EndsWith("]"))
                    {
                        string dungeonName = line.Substring(18, line.Length - 19); // Extract name between brackets
                        if (!string.IsNullOrEmpty(dungeonName))
                        {
                            dungeonNames.Add(dungeonName);
                            LogDebug($"Found vanilla dungeon: {dungeonName}");
                        }
                    }
                }
                
                LogDebug($"Found {dungeonNames.Count} total dungeons");
                
                // If no dungeons found, try runtime API approach
                if (dungeonNames.Count == 0)
                {
                    LogDebug("No dungeons found in config file, trying runtime API approach");
                    CreateInteriorNameOverrideConfigsFromRuntimeAPI();
                    return;
                }
                
                int configEntriesCreated = 0;
                // Create config entries for each dungeon
                foreach (string dungeonName in dungeonNames)
                {
                    if (!string.IsNullOrEmpty(dungeonName))
                    {
                        string configKey = dungeonName.Replace(" ", "_");
                        
                        LogDebug($"Processing dungeon: {dungeonName} (Config Key: {configKey})");
                        
                        if (!interiorNameOverrideConfigs.ContainsKey(dungeonName))
                        {
                            var configEntry = config.Bind(
                                "Interior Name Overrides",
                                $"{configKey}_NameOverride",
                                "",
                                $"Override name for {dungeonName} interior (leave blank to use default)"
                            );
                            interiorNameOverrideConfigs[dungeonName] = configEntry;
                            configEntriesCreated++;
                            LogDebug($"Created config entry for: {dungeonName}");
                        }
                        else
                        {
                            LogDebug($"Config entry already exists for: {dungeonName}");
                        }
                    }
                }
                
                LogDebug($"Finished creating {configEntriesCreated} interior name override configs from config file");
            }
            catch (System.Exception ex)
            {
                logger.LogError($"Error creating interior name override configs from config file: {ex.Message}");
                logger.LogError($"Stack trace: {ex.StackTrace}");
                LogDebug("Falling back to runtime API approach");
                CreateInteriorNameOverrideConfigsFromRuntimeAPI();
            }
        }
        
        private void CreateInteriorNameOverrideConfigsFromRuntimeAPI()
        {
            try
            {
                retryAttempt++;
                LogDebug($"Starting to create interior name override configs using runtime API (Attempt {retryAttempt})");
                
                // Check if LethalLevelLoader content is available
                LogDebug($"PatchedContent.VanillaExtendedDungeonFlows: {(PatchedContent.VanillaExtendedDungeonFlows != null ? "Available" : "NULL")}");
                LogDebug($"PatchedContent.CustomExtendedDungeonFlows: {(PatchedContent.CustomExtendedDungeonFlows != null ? "Available" : "NULL")}");
                
                if (PatchedContent.VanillaExtendedDungeonFlows == null && PatchedContent.CustomExtendedDungeonFlows == null)
                {
                    logger.LogWarning("LethalLevelLoader content not yet initialized. Will retry in a few seconds.");
                    plugin.StartCoroutine(RetryConfigGeneration(retryAttempt));
                    return;
                }
                
                // Additional check for empty collections
                if ((PatchedContent.VanillaExtendedDungeonFlows != null && PatchedContent.VanillaExtendedDungeonFlows.Count == 0) && 
                    (PatchedContent.CustomExtendedDungeonFlows != null && PatchedContent.CustomExtendedDungeonFlows.Count == 0))
                {
                    logger.LogWarning("LethalLevelLoader content collections are empty. Will retry in a few seconds.");
                    plugin.StartCoroutine(RetryConfigGeneration(retryAttempt));
                    return;
                }
                
                // Get all known dungeon flows from LethalLevelLoader
                List<ExtendedDungeonFlow> allDungeonFlows = new List<ExtendedDungeonFlow>();
                
                // Add vanilla dungeons
                if (PatchedContent.VanillaExtendedDungeonFlows != null)
                {
                    LogDebug($"Found {PatchedContent.VanillaExtendedDungeonFlows.Count} vanilla dungeon flows");
                    allDungeonFlows.AddRange(PatchedContent.VanillaExtendedDungeonFlows);
                }
                
                // Add custom dungeons
                if (PatchedContent.CustomExtendedDungeonFlows != null)
                {
                    LogDebug($"Found {PatchedContent.CustomExtendedDungeonFlows.Count} custom dungeon flows");
                    allDungeonFlows.AddRange(PatchedContent.CustomExtendedDungeonFlows);
                }
                
                LogDebug($"Total dungeon flows to process: {allDungeonFlows.Count}");
                
                if (allDungeonFlows.Count == 0)
                {
                    logger.LogWarning("No dungeon flows found. Will retry in a few seconds.");
                    plugin.StartCoroutine(RetryConfigGeneration(retryAttempt));
                    return;
                }
                
                int configEntriesCreated = 0;
                // Create config entries for each dungeon
                foreach (ExtendedDungeonFlow dungeonFlow in allDungeonFlows)
                {
                    if (dungeonFlow != null && !string.IsNullOrEmpty(dungeonFlow.DungeonName))
                    {
                        string interiorName = dungeonFlow.DungeonName;
                        string configKey = interiorName.Replace(" ", "_");
                        
                        LogDebug($"Processing dungeon: {interiorName} (Config Key: {configKey})");
                        
                        if (!interiorNameOverrideConfigs.ContainsKey(interiorName))
                        {
                            var configEntry = config.Bind(
                                "Interior Name Overrides",
                                $"{configKey}_NameOverride",
                                "",
                                $"Override name for {interiorName} interior (leave blank to use default)"
                            );
                            interiorNameOverrideConfigs[interiorName] = configEntry;
                            configEntriesCreated++;
                            LogDebug($"Created config entry for: {interiorName}");
                        }
                        else
                        {
                            LogDebug($"Config entry already exists for: {interiorName}");
                        }
                    }
                    else
                    {
                        LogDebug($"Skipping invalid dungeon flow: {(dungeonFlow != null ? "Name is null/empty" : "DungeonFlow is null")}");
                    }
                }
                
                LogDebug($"Finished creating {configEntriesCreated} interior name override configs from runtime API");
                retryAttempt = 0; // Reset retry counter on success
            }
            catch (System.Exception ex)
            {
                logger.LogError($"Error creating interior name override configs from runtime API: {ex.Message}");
                logger.LogError($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private IEnumerator RetryConfigGeneration(int attempt = 1)
        {
            // Implement exponential backoff
            float delay = Mathf.Min(3f * Mathf.Pow(2, attempt - 1), 30f); // Max 30 seconds
            LogDebug($"Retrying config generation in {delay} seconds... (Attempt {attempt})");
            yield return new WaitForSeconds(delay);
            
            if (attempt < 3)
            {
                CreateInteriorNameOverrideConfigs();
            }
            else
            {
                logger.LogError("Failed to generate configs after 3 attempts. Custom dungeons may not be available.");
            }
        }
        
        public void LogDebug(string message)
        {
            if (DebugLoggingEnabled)
            {
                logger.LogInfo($"[DEBUG] {message}");
            }
        }
    }
}