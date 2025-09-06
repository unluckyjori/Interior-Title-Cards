using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using InteriorTitleCards.Utils;
using LethalLevelLoader;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using System.ComponentModel;
using System;

// Needed for creating dropdown selectors in the config manager
internal class ConfigurationManagerAttributes
{
    public int? Order;
}

public enum ImageSourceMode
{
    [Description("Developer images only")]
    DeveloperOnly = 0,

    [Description("User-made images only")]
    UserOnly = 1,

    [Description("Both (developer prioritized)")]
    BothDeveloperPriority = 2,

    [Description("Both (user prioritized)")]
    BothUserPriority = 3
}

public enum ImageDisplayType
{
    [Description("Top text image only")]
    TopTextOnly = 0,

    [Description("Interior text image only")]
    InteriorTextOnly = 1,

    [Description("Top text image and interior text image")]
    BothSeparate = 2,

    [Description("Combined image")]
    Combined = 3
}







namespace InteriorTitleCards.Managers
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
        private ConfigEntry<int> topTextFontSizeConfig;
        private ConfigEntry<int> interiorTextFontSizeConfig;
        private ConfigEntry<int> topTextFontWeightConfig;
        private ConfigEntry<int> interiorTextFontWeightConfig;
         private ConfigEntry<string> topTextPositionConfig;
         private ConfigEntry<string> interiorTextPositionConfig;
         private ConfigEntry<string> topImagePositionConfig;
         private ConfigEntry<string> interiorImagePositionConfig;
         private ConfigEntry<string> combinedImagePositionConfig;
 // Animation timing configs
        private ConfigEntry<float> topTextDisplayDurationConfig;
        private ConfigEntry<float> interiorTextDisplayDurationConfig;
        private ConfigEntry<float> topTextFadeInDurationConfig;
        private ConfigEntry<float> topTextFadeOutDurationConfig;
        private ConfigEntry<float> interiorTextFadeInDurationConfig;
        private ConfigEntry<float> interiorTextFadeOutDurationConfig;
        
        // Delay configs
        private ConfigEntry<float> topTextStartDelayConfig;
        private ConfigEntry<float> interiorTextStartDelayConfig;
        
        // New visual effects configs
        private ConfigEntry<bool> topTextFadeEnabledConfig;
        private ConfigEntry<bool> interiorTextFadeEnabledConfig;
        
         // Custom images configs
         private ConfigEntry<ImageSourceMode> imageSourceModeConfig;
         private ConfigEntry<bool> enableCustomImagesConfig;
         private ConfigEntry<ImageDisplayType> imageDisplayTypeConfig;
         private ConfigEntry<string> imageBlacklistConfig;

         // Image resizing configs
         private ConfigEntry<int> topTextImageWidthConfig;
         private ConfigEntry<int> topTextImageHeightConfig;
         private ConfigEntry<int> interiorTextImageWidthConfig;
         private ConfigEntry<int> interiorTextImageHeightConfig;
         private ConfigEntry<int> combinedImageWidthConfig;
         private ConfigEntry<int> combinedImageHeightConfig;
        
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
                LogDebug("Accessing TopTextColor property");
                Color defaultColor = ParseColorFromString(TitleCardConstants.DefaultTopTextColor, ParseColorFromString(TitleCardConstants.DefaultTitleColor, Color.white));
                Color color = ParseColorFromConfig(topTextColorConfig, defaultColor);
                LogDebug($"TopTextColor value: {color}");
                return color;
            }
        }
        
        public Color InteriorTextColor
        {
            get
            {
                LogDebug("Accessing InteriorTextColor property");
                Color defaultColor = ParseColorFromString(TitleCardConstants.DefaultInteriorTextColor, ParseColorFromString(TitleCardConstants.DefaultTitleColor, Color.white));
                Color color = ParseColorFromConfig(interiorTextColorConfig, defaultColor);
                LogDebug($"InteriorTextColor value: {color}");
                return color;
            }
        }
        
        public bool DebugLoggingEnabled => debugLoggingConfig?.Value ?? false;
        public string CustomTopText => customTopTextConfig?.Value ?? "NOW ENTERING...";
        public int TopTextFontSize => topTextFontSizeConfig?.Value ?? TitleCardConstants.DefaultTopTextFontSize;
        public int InteriorTextFontSize => interiorTextFontSizeConfig?.Value ?? TitleCardConstants.DefaultBottomTextFontSize;
        public int TopTextFontWeight
        {
            get
            {
                int value = topTextFontWeightConfig?.Value ?? TitleCardConstants.DefaultFontWeightNormal;
                LogDebug($"TopTextFontWeight: {value}");
                return value;
            }
        }

        public int InteriorTextFontWeight
        {
            get
            {
                int value = interiorTextFontWeightConfig?.Value ?? TitleCardConstants.DefaultFontWeightBold;
                LogDebug($"InteriorTextFontWeight: {value}");
                return value;
            }
        }
        
        public Vector2 TopTextPosition
        {
            get
            {
                if (topTextPositionConfig?.Value != null)
                {
                    string[] parts = topTextPositionConfig.Value.Split(',');
                    if (parts.Length == 2 && 
                        float.TryParse(parts[0], out float x) && 
                        float.TryParse(parts[1], out float y))
                    {
                        return new Vector2(x, y);
                    }
                }
                return TitleCardConstants.DefaultTopTextPosition;
            }
        }
        
         public Vector2 InteriorTextPosition
         {
             get
             {
                 if (interiorTextPositionConfig?.Value != null)
                 {
                     string[] parts = interiorTextPositionConfig.Value.Split(',');
                     if (parts.Length == 2 &&
                         float.TryParse(parts[0], out float x) &&
                         float.TryParse(parts[1], out float y))
                     {
                         return new Vector2(x, y);
                     }
                 }
                 return TitleCardConstants.DefaultInteriorTextPosition;
             }
         }

         public Vector2 TopImagePosition
         {
             get
             {
                 if (topImagePositionConfig?.Value != null)
                 {
                     string[] parts = topImagePositionConfig.Value.Split(',');
                     if (parts.Length == 2 &&
                         float.TryParse(parts[0], out float x) &&
                         float.TryParse(parts[1], out float y))
                     {
                         return new Vector2(x, y);
                     }
                 }
                 return TitleCardConstants.DefaultTopImagePosition;
             }
         }

         public Vector2 InteriorImagePosition
         {
             get
             {
                 if (interiorImagePositionConfig?.Value != null)
                 {
                     string[] parts = interiorImagePositionConfig.Value.Split(',');
                     if (parts.Length == 2 &&
                         float.TryParse(parts[0], out float x) &&
                         float.TryParse(parts[1], out float y))
                     {
                         return new Vector2(x, y);
                     }
                 }
                 return TitleCardConstants.DefaultInteriorImagePosition;
             }
         }

         public Vector2 CombinedImagePosition
         {
             get
             {
                 if (combinedImagePositionConfig?.Value != null)
                 {
                     string[] parts = combinedImagePositionConfig.Value.Split(',');
                     if (parts.Length == 2 &&
                         float.TryParse(parts[0], out float x) &&
                         float.TryParse(parts[1], out float y))
                     {
                         return new Vector2(x, y);
                     }
                 }
                 return TitleCardConstants.DefaultCombinedImagePosition;
             }
         }
// Animation timing properties
        public float TopTextDisplayDuration => topTextDisplayDurationConfig?.Value ?? TitleCardConstants.DefaultDisplayDuration;
        public float InteriorTextDisplayDuration => interiorTextDisplayDurationConfig?.Value ?? TitleCardConstants.DefaultDisplayDuration;
        public float TopTextFadeInDuration => topTextFadeInDurationConfig?.Value ?? TitleCardConstants.DefaultFadeDuration;
        public float TopTextFadeOutDuration => topTextFadeOutDurationConfig?.Value ?? TitleCardConstants.DefaultFadeDuration;
        public float InteriorTextFadeInDuration => interiorTextFadeInDurationConfig?.Value ?? TitleCardConstants.DefaultFadeDuration;
        public float InteriorTextFadeOutDuration => interiorTextFadeOutDurationConfig?.Value ?? TitleCardConstants.DefaultFadeDuration;
        
        // Delay properties
        public float TopTextStartDelay => topTextStartDelayConfig?.Value ?? 0f;
        public float InteriorTextStartDelay => interiorTextStartDelayConfig?.Value ?? 0f;
        
        // New visual effects properties
        public bool TopTextFadeEnabled => topTextFadeEnabledConfig?.Value ?? true;
        public bool InteriorTextFadeEnabled => interiorTextFadeEnabledConfig?.Value ?? true;
        
        // Custom images properties
        public int ImageSourceMode
        {
            get
            {
                var configValue = imageSourceModeConfig?.Value;
                ImageSourceMode enumValue = configValue.HasValue ? configValue.Value : (ImageSourceMode)2;
                int value = (int)enumValue;

                // Validate the value is within valid range
                if (value < 0 || value > 3)
                {
                    LogDebug($"Invalid ImageSourceMode value: {value}, defaulting to BothDeveloperPriority (2)");
                    value = 2; // Default to BothDeveloperPriority
                }

                LogDebug($"ImageSourceMode: {value}");
                return value;
            }
        }

        public bool EnableCustomImages
        {
            get
            {
                bool value = enableCustomImagesConfig?.Value ?? true;
                LogDebug($"EnableCustomImages: {value}");
                return value;
            }
        }

        public int ImageDisplayType
        {
            get
            {
                var configValue = imageDisplayTypeConfig?.Value;
                ImageDisplayType enumValue = configValue.HasValue ? configValue.Value : (ImageDisplayType)2;
                int value = (int)enumValue;
                LogDebug($"ImageDisplayType: {value}");
                return value;
            }
        }
        
        public string ImageBlacklist
        {
            get
            {
                string value = imageBlacklistConfig?.Value ?? "";
                LogDebug($"ImageBlacklist: {value}");
                return value;
            }
        }

        // Image resizing properties
        public int TopTextImageWidth => topTextImageWidthConfig?.Value ?? 400;
        public int TopTextImageHeight => topTextImageHeightConfig?.Value ?? 40;
        public int InteriorTextImageWidth => interiorTextImageWidthConfig?.Value ?? 400;
        public int InteriorTextImageHeight => interiorTextImageHeightConfig?.Value ?? 40;
        public int CombinedImageWidth => combinedImageWidthConfig?.Value ?? 400;
        public int CombinedImageHeight => combinedImageHeightConfig?.Value ?? 40;

        #endregion

        #region Constructor
        
        public ConfigManager(ConfigFile config, ManualLogSource logger, Plugin plugin)
        {
            this.config = config;
            this.logger = logger;
            this.plugin = plugin;
        }

        /// <summary>
        /// Callback to re-initialize dependent components after config binding
        /// </summary>
        private System.Action onConfigsBound;

        /// <summary>
        /// Sets the callback to be invoked after configs are bound
        /// </summary>
        public void SetOnConfigsBoundCallback(System.Action callback)
        {
            onConfigsBound = callback;
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
            LogDebug($"{nameof(GetInteriorNameOverride)} called with dungeon: {dungeonName}");

            // Validate input
            if (string.IsNullOrEmpty(dungeonName))
            {
                LogDebug("Dungeon name is null or empty, returning 'Unknown'");
                return "Unknown";
            }

            // Check cache first
            if (nameOverrideCache.TryGetValue(dungeonName, out string cachedResult))
            {
                LogDebug($"Found cached result for '{dungeonName}': {cachedResult}");
                return cachedResult;
            }

            LogDebug($"No cached result for '{dungeonName}', processing");

            // Check if this dungeon name has a base name mapping (for facility variants)
            string baseName = dungeonName;
            if (variantToBaseMapping != null && variantToBaseMapping.ContainsKey(dungeonName))
            {
                baseName = variantToBaseMapping[dungeonName];
                LogDebug($"Mapped variant '{dungeonName}' to base name '{baseName}'");
            }

            LogDebug($"Checking for config override for base name: {baseName}");
            LogDebug($"Interior name override configs count: {interiorNameOverrideConfigs?.Count ?? 0}");

            // Look up config using the base name (or original name if no mapping)
            if (interiorNameOverrideConfigs != null && interiorNameOverrideConfigs.ContainsKey(baseName))
            {
                var configEntry = interiorNameOverrideConfigs[baseName];
                if (configEntry != null)
                {
                    string overrideName = configEntry.Value;
                    LogDebug($"Found config entry for '{baseName}', value: '{overrideName}'");

                    if (!string.IsNullOrEmpty(overrideName))
                    {
                        LogDebug($"Found override '{overrideName}' for base name '{baseName}'");
                        // Cache the result
                        nameOverrideCache[dungeonName] = overrideName;
                        LogDebug($"Cached result for '{dungeonName}': {overrideName}");
                        return overrideName;
                    }
                    else
                    {
                        LogDebug($"Override value is null or empty for '{baseName}'");
                    }
                }
                else
                {
                    LogDebug($"Config entry for '{baseName}' is null");
                }
            }
            else
            {
                LogDebug($"No config entry found for '{baseName}'");
            }

            LogDebug($"No override found for '{baseName}', using original name: {dungeonName}");
            // Cache the result
            nameOverrideCache[dungeonName] = dungeonName;
            LogDebug($"Cached original name for '{dungeonName}': {dungeonName}");
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
                new ConfigDescription(
                    "Enable detailed debug logging for troubleshooting",
                    null,
                    new ConfigurationManagerAttributes { Order = 1 }
                )
            );
            
            customTopTextConfig = config.Bind(
                "Text Appearance",
                "Top text override",
                "NOW ENTERING...",
                new ConfigDescription(
                    "Custom text displayed above the interior name (leave blank to use default 'NOW ENTERING...')",
                    null,
                    new ConfigurationManagerAttributes { Order = 1 }
                )
            );
            
            topTextFontSizeConfig = config.Bind(
                "Text Appearance",
                "TopTextFontSize",
                TitleCardConstants.DefaultTopTextFontSize,
                new ConfigDescription(
                    "Font size for the top text (e.g., 20 for default size)",
                    null,
                    new ConfigurationManagerAttributes { Order = 2 }
                )
            );
            
            interiorTextFontSizeConfig = config.Bind(
                "Text Appearance",
                "InteriorTextFontSize",
                TitleCardConstants.DefaultBottomTextFontSize,
                new ConfigDescription(
                    "Font size for the interior name text (e.g., 28 for default size)",
                    null,
                    new ConfigurationManagerAttributes { Order = 3 }
                )
            );
            
            topTextColorConfig = config.Bind(
                "Text Appearance",
                "TopTextColor",
                TitleCardConstants.DefaultTopTextColor,
                new ConfigDescription(
                    "Color of the top text in hex format (e.g., #fe6001). Leave blank to use the default orange color",
                    null,
                    new ConfigurationManagerAttributes { Order = 4 }
                )
            );

            interiorTextColorConfig = config.Bind(
                "Text Appearance",
                "InteriorTextColor",
                TitleCardConstants.DefaultInteriorTextColor,
                new ConfigDescription(
                    "Color of the interior name text in hex format (e.g., #fe6001). Leave blank to use the default orange color",
                    null,
                    new ConfigurationManagerAttributes { Order = 5 }
                )
            );

            topTextFontWeightConfig = config.Bind(
                "Text Appearance",
                "TopTextFontWeight",
                TitleCardConstants.DefaultFontWeightNormal,
                new ConfigDescription(
                    "Font weight (boldness) for the top text (400=Normal, 700=Bold)",
                    null,
                    new ConfigurationManagerAttributes { Order = 7 }
                )
            );

            interiorTextFontWeightConfig = config.Bind(
                "Text Appearance",
                "InteriorTextFontWeight",
                TitleCardConstants.DefaultFontWeightBold,
                new ConfigDescription(
                    "Font weight (boldness) for the interior name text (400=Normal, 700=Bold)",
                    null,
                    new ConfigurationManagerAttributes { Order = 8 }
                )
            );
            
            topTextPositionConfig = config.Bind(
                "Text Appearance",
                "TopTextPosition",
                $"{TitleCardConstants.DefaultTopTextPosition.x},{TitleCardConstants.DefaultTopTextPosition.y}",
                new ConfigDescription(
                    "Position of the top text as X,Y coordinates (e.g., 0,20). Default is centered horizontally with vertical offset.",
                    null,
                    new ConfigurationManagerAttributes { Order = 9 }
                )
            );
            
             interiorTextPositionConfig = config.Bind(
                 "Text Appearance",
                 "InteriorTextPosition",
                 $"{TitleCardConstants.DefaultInteriorTextPosition.x},{TitleCardConstants.DefaultInteriorTextPosition.y}",
                 new ConfigDescription(
                     "Position of the interior text as X,Y coordinates (e.g., 0,-20). Default is centered horizontally with vertical offset.",
                     null,
                     new ConfigurationManagerAttributes { Order = 10 }
                 )
             );



            // Animation timing configs
            topTextDisplayDurationConfig = config.Bind(
                "Animation Timing",
                "TopTextDisplayDuration",
                TitleCardConstants.DefaultDisplayDuration,
                new ConfigDescription(
                    "How long the top text displays on screen in seconds (e.g., 3.0 for 3 seconds)",
                    null,
                    new ConfigurationManagerAttributes { Order = 1 }
                )
            );
            
            interiorTextDisplayDurationConfig = config.Bind(
                "Animation Timing",
                "InteriorTextDisplayDuration",
                TitleCardConstants.DefaultDisplayDuration,
                new ConfigDescription(
                    "How long the interior text displays on screen in seconds (e.g., 3.0 for 3 seconds)",
                    null,
                    new ConfigurationManagerAttributes { Order = 2 }
                )
            );
            
            topTextFadeInDurationConfig = config.Bind(
                "Animation Timing",
                "TopTextFadeInDuration",
                TitleCardConstants.DefaultFadeDuration,
                new ConfigDescription(
                    "How long the top text takes to fade in (e.g., 0.5 for half a second)",
                    null,
                    new ConfigurationManagerAttributes { Order = 3 }
                )
            );
            
            topTextFadeOutDurationConfig = config.Bind(
                "Animation Timing",
                "TopTextFadeOutDuration",
                TitleCardConstants.DefaultFadeDuration,
                new ConfigDescription(
                    "How long the top text takes to fade out (e.g., 0.5 for half a second)",
                    null,
                    new ConfigurationManagerAttributes { Order = 4 }
                )
            );
            
            interiorTextFadeInDurationConfig = config.Bind(
                "Animation Timing",
                "InteriorTextFadeInDuration",
                TitleCardConstants.DefaultFadeDuration,
                new ConfigDescription(
                    "How long the interior text takes to fade in (e.g., 0.5 for half a second)",
                    null,
                    new ConfigurationManagerAttributes { Order = 5 }
                )
            );
            
            interiorTextFadeOutDurationConfig = config.Bind(
                "Animation Timing",
                "InteriorTextFadeOutDuration",
                TitleCardConstants.DefaultFadeDuration,
                new ConfigDescription(
                    "How long the interior text takes to fade out (e.g., 0.5 for half a second)",
                    null,
                    new ConfigurationManagerAttributes { Order = 6 }
                )
            );
            
            // New visual effects configs
            topTextFadeEnabledConfig = config.Bind(
                "Visual Effects",
                "TopTextFadeEnabled",
                true,
                new ConfigDescription(
                    "Enable fade in/out effect for top text",
                    null,
                    new ConfigurationManagerAttributes { Order = 1 }
                )
            );
            
            interiorTextFadeEnabledConfig = config.Bind(
                "Visual Effects",
                "InteriorTextFadeEnabled",
                true,
                new ConfigDescription(
                    "Enable fade in/out effect for interior text",
                    null,
                    new ConfigurationManagerAttributes { Order = 2 }
                )
            );
            
            // Delay configs
            topTextStartDelayConfig = config.Bind(
                "Animation Timing",
                "TopTextStartDelay",
                0f,
                new ConfigDescription(
                    "Delay in seconds before the top text starts displaying after entering (e.g., 1.0 for 1 second)",
                    null,
                    new ConfigurationManagerAttributes { Order = 7 }
                )
            );
            
            interiorTextStartDelayConfig = config.Bind(
                "Animation Timing",
                "InteriorTextStartDelay",
                0f,
                new ConfigDescription(
                    "Delay in seconds before the interior text starts displaying after entering (e.g., 1.0 for 1 second)",
                    null,
                    new ConfigurationManagerAttributes { Order = 8 }
                )
            );
            
             // Custom images configs
              enableCustomImagesConfig = config.Bind(
                  "Custom Images",
                  "EnableCustomImages",
                  true,
                  new ConfigDescription(
                      "Enable or disable all custom image functionality",
                      null,
                      new ConfigurationManagerAttributes { Order = 1 }
                  )
              );

               imageDisplayTypeConfig = config.Bind(
                   "Custom Images",
                   "ImageDisplayType",
                   (ImageDisplayType)2,
                   new ConfigDescription(
                       "Image display type",
                       null,
                       new ConfigurationManagerAttributes { Order = 2 }
                   )
               );

               imageSourceModeConfig = config.Bind(
                   "Custom Images",
                   "ImageSourceMode",
                   (ImageSourceMode)2,
                   new ConfigDescription(
                       "Image source mode",
                       null,
                       new ConfigurationManagerAttributes { Order = 3 }
                   )
               );

             imageBlacklistConfig = config.Bind(
                 "Custom Images",
                 "ImageBlacklist",
                 "",
                 new ConfigDescription(
                     "Comma-separated list of image paths to exclude (e.g., dev/name/interiortext,user/othername/toptext)",
                     null,
                     new ConfigurationManagerAttributes { Order = 4 }
                 )
             );

             topImagePositionConfig = config.Bind(
                 "Custom Images",
                 "TopImagePosition",
                 $"{TitleCardConstants.DefaultTopImagePosition.x},{TitleCardConstants.DefaultTopImagePosition.y}",
                 new ConfigDescription(
                     "Position of top images as X,Y coordinates",
                     null,
                     new ConfigurationManagerAttributes { Order = 5 }
                 )
             );

             interiorImagePositionConfig = config.Bind(
                 "Custom Images",
                 "InteriorImagePosition",
                 $"{TitleCardConstants.DefaultInteriorImagePosition.x},{TitleCardConstants.DefaultInteriorImagePosition.y}",
                 new ConfigDescription(
                     "Position of interior images as X,Y coordinates",
                     null,
                     new ConfigurationManagerAttributes { Order = 6 }
                 )
             );

             combinedImagePositionConfig = config.Bind(
                 "Custom Images",
                 "CombinedImagePosition",
                 $"{TitleCardConstants.DefaultCombinedImagePosition.x},{TitleCardConstants.DefaultCombinedImagePosition.y}",
                 new ConfigDescription(
                     "Position of combined images as X,Y coordinates",
                     null,
                     new ConfigurationManagerAttributes { Order = 7 }
                 )
             );

            // Image resizing configs
            topTextImageWidthConfig = config.Bind(
                "Custom Images",
                "TopTextImageWidth",
                400,
                new ConfigDescription(
                    "Width for top text images in pixels (0 = use original width)",
                    null,
                    new ConfigurationManagerAttributes { Order = 8 }
                )
            );

            topTextImageHeightConfig = config.Bind(
                "Custom Images",
                "TopTextImageHeight",
                40,
                new ConfigDescription(
                    "Height for top text images in pixels (0 = use original height)",
                    null,
                    new ConfigurationManagerAttributes { Order = 9 }
                )
            );

            interiorTextImageWidthConfig = config.Bind(
                "Custom Images",
                "InteriorTextImageWidth",
                400,
                new ConfigDescription(
                    "Width for interior text images in pixels (0 = use original width)",
                    null,
                    new ConfigurationManagerAttributes { Order = 10 }
                )
            );

            interiorTextImageHeightConfig = config.Bind(
                "Custom Images",
                "InteriorTextImageHeight",
                40,
                new ConfigDescription(
                    "Height for interior text images in pixels (0 = use original height)",
                    null,
                    new ConfigurationManagerAttributes { Order = 11 }
                )
            );

            combinedImageWidthConfig = config.Bind(
                "Custom Images",
                "CombinedImageWidth",
                400,
                new ConfigDescription(
                    "Width for combined images in pixels (0 = use original width)",
                    null,
                    new ConfigurationManagerAttributes { Order = 12 }
                )
            );

            combinedImageHeightConfig = config.Bind(
                "Custom Images",
                "CombinedImageHeight",
                40,
                new ConfigDescription(
                    "Height for combined images in pixels (0 = use original height)",
                    null,
                    new ConfigurationManagerAttributes { Order = 13 }
                )
            );
            
            LogDebug("Starting config binding process");
            
            // Create config entries for all known interiors
            CreateInteriorNameOverrideConfigs();

            // Notify dependent components that configs are now bound
            onConfigsBound?.Invoke();
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

                // Validate plugin reference
                if (plugin == null)
                {
                    logger.LogError("Plugin reference is null, cannot create config entries");
                    return;
                }

                // Check if LethalLevelLoader content is available
                LogDebug($"PatchedContent.VanillaExtendedDungeonFlows: {(PatchedContent.VanillaExtendedDungeonFlows != null ? "Available" : "NULL")}");
                LogDebug($"PatchedContent.CustomExtendedDungeonFlows: {(PatchedContent.CustomExtendedDungeonFlows != null ? "Available" : "NULL")}");

                if (PatchedContent.VanillaExtendedDungeonFlows == null && PatchedContent.CustomExtendedDungeonFlows == null)
                {
                    logger.LogWarning("LethalLevelLoader content not yet initialized. Will retry in a few seconds.");
                    if (plugin != null)
                    {
                        plugin.StartCoroutine(RetryConfigGeneration(retryAttempt));
                    }
                    return;
                }

                // Additional check for empty collections
                if ((PatchedContent.VanillaExtendedDungeonFlows != null && PatchedContent.VanillaExtendedDungeonFlows.Count == 0) &&
                    (PatchedContent.CustomExtendedDungeonFlows != null && PatchedContent.CustomExtendedDungeonFlows.Count == 0))
                {
                    logger.LogWarning("LethalLevelLoader content collections are empty. Will retry in a few seconds.");
                    if (plugin != null)
                    {
                        plugin.StartCoroutine(RetryConfigGeneration(retryAttempt));
                    }
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
                    if (plugin != null)
                    {
                        plugin.StartCoroutine(RetryConfigGeneration(retryAttempt));
                    }
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
                logger.LogDebug($"Exception details: {ex.GetType().Name} - {ex.StackTrace}");
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