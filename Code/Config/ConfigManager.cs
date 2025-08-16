using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using InteriorTitleCards.Components;
using LethalLevelLoader;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace InteriorTitleCards.Config
{
    /// <summary>
    /// Manages configuration loading and provides access to configuration settings with fallback values.
    /// </summary>
    public class ConfigManager
    {
        #region Private Fields
        
        private readonly ConfigFile config;
        private readonly ManualLogSource logger;
        private readonly Plugin plugin;
        
        // Config entries
        private ConfigEntry<string> topTextColorConfig;
        private ConfigEntry<string> interiorTextColorConfig;
        private ConfigEntry<bool> debugLoggingConfig;
        private ConfigEntry<string> customTopTextConfig;
        private ConfigEntry<int> topTextFontWeightConfig;
        private ConfigEntry<int> interiorTextFontWeightConfig;
// Animation timing configs
        private ConfigEntry<float> topTextDisplayDurationConfig;
        private ConfigEntry<float> interiorTextDisplayDurationConfig;
        private ConfigEntry<float> topTextFadeInDurationConfig;
        private ConfigEntry<float> topTextFadeOutDurationConfig;
        private ConfigEntry<float> interiorTextFadeInDurationConfig;
        private ConfigEntry<float> interiorTextFadeOutDurationConfig;
        
        // New visual effects configs
        private ConfigEntry<bool> topTextFadeEnabledConfig;
        private ConfigEntry<bool> interiorTextFadeEnabledConfig;
        
        private Dictionary<string, ConfigEntry<string>> interiorNameOverrideConfigs = new Dictionary<string, ConfigEntry<string>>();
        
        // Mapping from variant names to base names for facility variants
        private static readonly Dictionary<string, string> variantToBaseMapping = new Dictionary<string, string>
        {
            { "Facility (Level1Flow)", "Facility" },
            { "Facility (Level1Flow3Exits)", "Facility" },
            { "Facility (Level1FlowExtraLarge)", "Facility" },
            { "Haunted Mansion (Level2Flow)", "Haunted Mansion" },
            { "Mineshaft (Level3Flow)", "Mineshaft" }
        };
        
        private int retryAttempt = 0;
        
        #endregion

        #region Public Properties
        
        public Color TitleColor
        {
            get
            {
                // Fallback to orange if no text colors are available
                return ParseColorFromString(TitleCardConstants.DefaultTitleColor, Color.white);
            }
        }
        
        public Color TopTextColor
        {
            get
            {
                Color defaultColor = ParseColorFromString(TitleCardConstants.DefaultTopTextColor, ParseColorFromString(TitleCardConstants.DefaultTitleColor, Color.white));
                return ParseColorFromConfig(topTextColorConfig, defaultColor);
            }
        }
        
        public Color InteriorTextColor
        {
            get
            {
                Color defaultColor = ParseColorFromString(TitleCardConstants.DefaultInteriorTextColor, ParseColorFromString(TitleCardConstants.DefaultTitleColor, Color.white));
                return ParseColorFromConfig(interiorTextColorConfig, defaultColor);
            }
        }
        
        public bool DebugLoggingEnabled => debugLoggingConfig?.Value ?? false;
        public string CustomTopText => customTopTextConfig?.Value ?? "NOW ENTERING...";
        public int TopTextFontWeight => topTextFontWeightConfig?.Value ?? TitleCardConstants.DefaultFontWeightNormal;
        public int InteriorTextFontWeight => interiorTextFontWeightConfig?.Value ?? TitleCardConstants.DefaultFontWeightBold;
// Animation timing properties
        public float TopTextDisplayDuration => topTextDisplayDurationConfig?.Value ?? TitleCardConstants.DefaultDisplayDuration;
        public float InteriorTextDisplayDuration => interiorTextDisplayDurationConfig?.Value ?? TitleCardConstants.DefaultDisplayDuration;
        public float TopTextFadeInDuration => topTextFadeInDurationConfig?.Value ?? TitleCardConstants.DefaultFadeDuration;
        public float TopTextFadeOutDuration => topTextFadeOutDurationConfig?.Value ?? TitleCardConstants.DefaultFadeDuration;
        public float InteriorTextFadeInDuration => interiorTextFadeInDurationConfig?.Value ?? TitleCardConstants.DefaultFadeDuration;
        public float InteriorTextFadeOutDuration => interiorTextFadeOutDurationConfig?.Value ?? TitleCardConstants.DefaultFadeDuration;
        
        // New visual effects properties
        public bool TopTextFadeEnabled => topTextFadeEnabledConfig?.Value ?? true;
        public bool InteriorTextFadeEnabled => interiorTextFadeEnabledConfig?.Value ?? true;
        
        #endregion

        #region Constructor
        
        public ConfigManager(ConfigFile config, ManualLogSource logger, Plugin plugin)
        {
            this.config = config;
            this.logger = logger;
            this.plugin = plugin;
        }
        
        #endregion
        
        #region Public Methods
        
        public void Initialize()
        {
            LogDebug($"{nameof(Initialize)} called - starting config initialization");
            // Initialize configs after LethalLevelLoader has loaded its content
            plugin.StartCoroutine(InitializeConfigsDelayed());
        }
        
        // Cache for GetInteriorNameOverride results
        private readonly Dictionary<string, string> nameOverrideCache = new Dictionary<string, string>();
        
        public string GetInteriorNameOverride(string dungeonName)
        {
            // Check cache first
            if (nameOverrideCache.TryGetValue(dungeonName, out string cachedResult))
            {
                return cachedResult;
            }
            
            LogDebug($"{nameof(GetInteriorNameOverride)} called with dungeon: {dungeonName}");
            
            // Check if this dungeon name has a base name mapping (for facility variants)
            string baseName = dungeonName;
            if (variantToBaseMapping.ContainsKey(dungeonName))
            {
                baseName = variantToBaseMapping[dungeonName];
                LogDebug($"Mapped variant '{dungeonName}' to base name '{baseName}'");
            }
            
            // Look up config using the base name (or original name if no mapping)
            if (interiorNameOverrideConfigs.ContainsKey(baseName))
            {
                string overrideName = interiorNameOverrideConfigs[baseName].Value;
                if (!string.IsNullOrEmpty(overrideName))
                {
                    LogDebug($"Found override '{overrideName}' for base name '{baseName}'");
                    // Cache the result
                    nameOverrideCache[dungeonName] = overrideName;
                    return overrideName;
                }
            }
            
            LogDebug($"No override found for '{baseName}', using original name");
            // Cache the result
            nameOverrideCache[dungeonName] = dungeonName;
            return dungeonName;
        }
        
        public void LogDebug(string message)
        {
            if (DebugLoggingEnabled)
            {
                logger.LogInfo($"[DEBUG] {message}");
            }
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Parses a color from a config entry with fallback to default color.
        /// </summary>
        /// <param name="colorConfig">The config entry containing the color string.</param>
        /// <param name="defaultColor">The default color to use if parsing fails.</param>
        /// <returns>The parsed color or default color.</returns>
        private Color ParseColorFromConfig(ConfigEntry<string> colorConfig, Color defaultColor)
        {
            if (colorConfig != null && !string.IsNullOrEmpty(colorConfig.Value) && ColorUtility.TryParseHtmlString(colorConfig.Value, out Color color))
            {
                return color;
            }
            return defaultColor;
        }
        
        /// <summary>
        /// Parses a color from a string with fallback to default color.
        /// </summary>
        /// <param name="colorString">The color string to parse.</param>
        /// <param name="defaultColor">The default color to use if parsing fails.</param>
        /// <returns>The parsed color or default color.</returns>
        private Color ParseColorFromString(string colorString, Color defaultColor)
        {
            if (!string.IsNullOrEmpty(colorString) && ColorUtility.TryParseHtmlString(colorString, out Color color))
            {
                return color;
            }
            return defaultColor;
        }
        
        private IEnumerator InitializeConfigsDelayed()
        {
            // Wait for LethalLevelLoader to initialize
            yield return new WaitForSeconds(TitleCardConstants.ConfigInitializationDelay);
            
            BindConfig();
        }
        
        private void BindConfig()
        {
            LogDebug($"{nameof(BindConfig)} called - binding configuration entries");
            
            // Bind config entries

            debugLoggingConfig = config.Bind(
                "Debug Settings",
                "EnableDebugLogging",
                false,
                "Enable detailed debug logging for troubleshooting"
            );
            
            customTopTextConfig = config.Bind(
                "Text Appearance",
                "Top text override",
                "NOW ENTERING...",
                "Custom text displayed above the interior name (leave blank to use default 'NOW ENTERING...')"
            );
            
            topTextColorConfig = config.Bind(
                "Text Appearance",
                "TopTextColor",
                TitleCardConstants.DefaultTopTextColor,
                "Color of the top text in hex format (e.g., #fe6001). Leave blank to use the default orange color"
            );

            interiorTextColorConfig = config.Bind(
                "Text Appearance",
                "InteriorTextColor",
                TitleCardConstants.DefaultInteriorTextColor,
                "Color of the interior name text in hex format (e.g., #fe6001). Leave blank to use the default orange color"
            );

            topTextFontWeightConfig = config.Bind(
                "Text Appearance",
                "TopTextFontWeight",
                TitleCardConstants.DefaultFontWeightNormal,
                "Font weight (boldness) for the top text (e.g., 400 for normal, 700 for bold)"
            );

            interiorTextFontWeightConfig = config.Bind(
                "Text Appearance",
                "InteriorTextFontWeight",
                TitleCardConstants.DefaultFontWeightBold,
                "Font weight (boldness) for the interior name text (e.g., 400 for normal, 700 for bold)"
            );

            // Animation timing configs
            topTextDisplayDurationConfig = config.Bind(
                "Animation Timing",
                "TopTextDisplayDuration",
                TitleCardConstants.DefaultDisplayDuration,
                "How long the top text displays on screen in seconds (e.g., 3.0 for 3 seconds)"
            );
            
            interiorTextDisplayDurationConfig = config.Bind(
                "Animation Timing",
                "InteriorTextDisplayDuration",
                TitleCardConstants.DefaultDisplayDuration,
                "How long the interior text displays on screen in seconds (e.g., 3.0 for 3 seconds)"
            );
            
            topTextFadeInDurationConfig = config.Bind(
                "Animation Timing",
                "TopTextFadeInDuration",
                TitleCardConstants.DefaultFadeDuration,
                "How long the top text takes to fade in (e.g., 0.5 for half a second)"
            );
            
            topTextFadeOutDurationConfig = config.Bind(
                "Animation Timing",
                "TopTextFadeOutDuration",
                TitleCardConstants.DefaultFadeDuration,
                "How long the top text takes to fade out (e.g., 0.5 for half a second)"
            );
            
            interiorTextFadeInDurationConfig = config.Bind(
                "Animation Timing",
                "InteriorTextFadeInDuration",
                TitleCardConstants.DefaultFadeDuration,
                "How long the interior text takes to fade in (e.g., 0.5 for half a second)"
            );
            
            interiorTextFadeOutDurationConfig = config.Bind(
                "Animation Timing",
                "InteriorTextFadeOutDuration",
                TitleCardConstants.DefaultFadeDuration,
                "How long the interior text takes to fade out (e.g., 0.5 for half a second)"
            );
            
            // New visual effects configs
            topTextFadeEnabledConfig = config.Bind(
                "Visual Effects",
                "TopTextFadeEnabled",
                true,
                "Enable fade in/out effect for top text"
            );
            
            interiorTextFadeEnabledConfig = config.Bind(
                "Visual Effects",
                "InteriorTextFadeEnabled",
                true,
                "Enable fade in/out effect for interior text"
            );
            
            LogDebug("Starting config binding process");
            
            // Create config entries for all known interiors
            CreateInteriorNameOverrideConfigs();
        }
        
        /// <summary>
        /// Creates interior name override configs by trying multiple approaches.
        /// </summary>
        private void CreateInteriorNameOverrideConfigs()
        {
            LogDebug($"{nameof(CreateInteriorNameOverrideConfigs)} called");
            
            string configPath = GetLethalLevelLoaderConfigPath();
            if (string.IsNullOrEmpty(configPath))
            {
                LogDebug("Config path not found, trying runtime API approach");
                CreateInteriorNameOverrideConfigsFromRuntimeAPI();
                return;
            }
            
            var dungeonNames = ParseDungeonNamesFromConfigFile(configPath);
            if (dungeonNames.Count == 0)
            {
                LogDebug("No dungeons found in config file, trying runtime API approach");
                CreateInteriorNameOverrideConfigsFromRuntimeAPI();
                return;
            }
            
            CreateConfigEntriesForDungeons(dungeonNames);
        }
        
        /// <summary>
        /// Gets the LethalLevelLoader config file path from multiple possible locations.
        /// </summary>
        /// <returns>The config file path, or null if not found.</returns>
        private string GetLethalLevelLoaderConfigPath()
        {
            // Try primary BepInEx config path
            string configPath = Path.Combine(Paths.ConfigPath, "LethalLevelLoader.cfg");
            LogDebug($"Looking for LethalLevelLoader.cfg at: {configPath}");
            
            if (File.Exists(configPath))
            {
                LogDebug($"Found LethalLevelLoader.cfg at: {configPath}");
                return configPath;
            }
            
            // Try alternative path
            LogDebug("LethalLevelLoader.cfg not found at primary path, trying alternative path");
            configPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "BepInEx/config/LethalLevelLoader.cfg");
            LogDebug($"Looking for LethalLevelLoader.cfg at alternative path: {configPath}");
            
            if (File.Exists(configPath))
            {
                return configPath;
            }
            
            // Don't use relative path traversal for security reasons
            LogDebug("LethalLevelLoader.cfg not found at alternative path, skipping Resources directory check for security");
            
            return null;
        }
        
        /// <summary>
        /// Parses dungeon names from the LethalLevelLoader config file.
        /// </summary>
        /// <param name="configPath">Path to the config file.</param>
        /// <returns>Set of unique dungeon names.</returns>
        private HashSet<string> ParseDungeonNamesFromConfigFile(string configPath)
        {
            try
            {
                string[] lines = File.ReadAllLines(configPath);
                HashSet<string> dungeonNames = new HashSet<string>();
                
                foreach (string line in lines)
                {
                    // Look for lines like "[Custom Dungeon: Art Gallery]" or similar patterns
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        string content = line.Substring(1, line.Length - 2); // Remove brackets
                        LogDebug($"Processing config line: {content}");
                        
                        // Check for custom dungeon patterns
                        if (content.Contains(TitleCardConstants.CustomDungeonPrefix))
                        {
                            string dungeonName = ExtractDungeonName(content, TitleCardConstants.CustomDungeonPrefix);
                            if (!string.IsNullOrEmpty(dungeonName))
                            {
                                dungeonNames.Add(dungeonName);
                                LogDebug($"Found custom dungeon: {dungeonName}");
                            }
                        }
                        
                        // Check for vanilla dungeon patterns
                        if (content.Contains(TitleCardConstants.VanillaDungeonPrefix))
                        {
                            string dungeonName = ExtractDungeonName(content, TitleCardConstants.VanillaDungeonPrefix);
                            if (!string.IsNullOrEmpty(dungeonName))
                            {
                                dungeonNames.Add(dungeonName);
                                LogDebug($"Found vanilla dungeon: {dungeonName}");
                            }
                        }
                    }
                }
                
                LogDebug($"Found {dungeonNames.Count} total dungeons");
                return dungeonNames;
            }
            catch (System.Exception ex)
            {
                logger.LogError($"Error parsing dungeon names from config file: {ex.Message}");
                return new HashSet<string>();
            }
        }
        
        /// <summary>
        /// Extracts dungeon name from config line content.
        /// </summary>
        /// <param name="content">The config line content.</param>
        /// <param name="prefix">The prefix to look for.</param>
        /// <returns>The extracted dungeon name.</returns>
        private string ExtractDungeonName(string content, string prefix)
        {
            int prefixIndex = content.IndexOf(prefix);
            if (prefixIndex >= 0)
            {
                int index = prefixIndex + prefix.Length;
                return content.Substring(index).Trim();
            }
            return null;
        }
        
        /// <summary>
        /// Creates config entries for a collection of dungeon names.
        /// </summary>
        /// <param name="dungeonNames">Collection of dungeon names.</param>
        private void CreateConfigEntriesForDungeons(HashSet<string> dungeonNames)
        {
            int configEntriesCreated = 0;
            
            foreach (string dungeonName in dungeonNames)
            {
                if (!string.IsNullOrEmpty(dungeonName))
                {
                    // Use base name for facility variants, original name for others
                    string baseName = dungeonName;
                    if (variantToBaseMapping.ContainsKey(dungeonName))
                    {
                        baseName = variantToBaseMapping[dungeonName];
                        LogDebug($"Using base name '{baseName}' for variant '{dungeonName}'");
                    }
                    
                    string configKey = baseName.Replace(" ", "_");
                    
                    LogDebug($"Processing dungeon: {dungeonName} -> Base: {baseName} (Config Key: {configKey})");
                    
                    // Only create config entry if we haven't already created one for this base name
                    if (!interiorNameOverrideConfigs.ContainsKey(baseName))
                    {
                        var configEntry = config.Bind(
                            "Interior Name Overrides",
                            $"{configKey}_NameOverride",
                            "",
                            $"Override name for {baseName} interior (leave blank to use default)"
                        );
                        interiorNameOverrideConfigs[baseName] = configEntry;
                        configEntriesCreated++;
                        LogDebug($"Created config entry for: {baseName}");
                    }
                    else
                    {
                        LogDebug($"Config entry already exists for base name: {baseName}");
                    }
                }
            }
            
            LogDebug($"Finished creating {configEntriesCreated} interior name override configs");
        }
        
        private void CreateInteriorNameOverrideConfigsFromRuntimeAPI()
        {
            try
            {
                retryAttempt++;
                LogDebug($"{nameof(CreateInteriorNameOverrideConfigsFromRuntimeAPI)} called (Attempt {retryAttempt})");
                
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
                
                var dungeonNames = new HashSet<string>();
                foreach (ExtendedDungeonFlow dungeonFlow in allDungeonFlows)
                {
                    if (dungeonFlow != null && !string.IsNullOrEmpty(dungeonFlow.DungeonName))
                    {
                        dungeonNames.Add(dungeonFlow.DungeonName);
                    }
                }
                
                CreateConfigEntriesForDungeons(dungeonNames);
                retryAttempt = 0; // Reset retry counter on success
            }
            catch (System.Exception ex)
            {
                logger.LogError($"Error creating interior name override configs from runtime API: {ex.Message}");
            }
        }
        
        private IEnumerator RetryConfigGeneration(int attempt = 1)
        {
            // Implement exponential backoff
            float delay = UnityEngine.Mathf.Min(TitleCardConstants.ConfigRetryBaseDelay * UnityEngine.Mathf.Pow(2, attempt - 1), TitleCardConstants.ConfigRetryMaxDelay);
            LogDebug($"{nameof(RetryConfigGeneration)} called - retrying in {delay} seconds... (Attempt {attempt})");
            yield return new UnityEngine.WaitForSeconds(delay);
            
            if (attempt < TitleCardConstants.MaxConfigRetryAttempts)
            {
                CreateInteriorNameOverrideConfigs();
            }
            else
            {
                logger.LogError("Failed to generate configs after 3 attempts. Custom dungeons may not be available.");
            }
        }
        
        #endregion
    }
}