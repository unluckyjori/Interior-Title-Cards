namespace InteriorTitleCards.Components
{
    /// <summary>
    /// Constants used throughout the Interior Title Cards mod to eliminate magic numbers and strings.
    /// </summary>
    public static class TitleCardConstants
    {
        #region Layout Constants
        
        /// <summary>
        /// Width of the title card in pixels.
        /// </summary>
        public const float CardWidth = 400f;
        
        /// <summary>
        /// Height of the title card in pixels.
        /// </summary>
        public const float CardHeight = 120f;
        
        /// <summary>
        /// Height of individual text elements in pixels.
        /// </summary>
        public const float TextHeight = 40f;
        
        /// <summary>
        /// Vertical offset for the top text from center.
        /// </summary>
        public const float TopTextOffset = 20f;
        
        /// <summary>
        /// Vertical offset for the bottom text from center.
        /// </summary>
        public const float BottomTextOffset = -20f;
        
        /// <summary>
        /// Center anchor value for RectTransform positioning.
        /// </summary>
        public const float CenterAnchor = 0.5f;
        
        #endregion

        #region Font Constants
        
        /// <summary>
        /// Default font size for the top text ("NOW ENTERING...").
        /// </summary>
        public const int DefaultTopTextFontSize = 20;
        
        /// <summary>
        /// Default font size for the interior name text.
        /// </summary>
        public const int DefaultBottomTextFontSize = 28;
        
        /// <summary>
        /// Font size for the top text ("NOW ENTERING...").
        /// </summary>
        public const int TopTextFontSize = 20;
        
        /// <summary>
        /// Font size for the interior name text.
        /// </summary>
        public const int BottomTextFontSize = 28;
        
        #endregion

        #region Configuration Constants
        
        /// <summary>
        /// Prefix for custom dungeon names in config files.
        /// </summary>
        public const string CustomDungeonPrefix = "Custom Dungeon:";
        
        /// <summary>
        /// Prefix for vanilla dungeon names in config files.
        /// </summary>
        public const string VanillaDungeonPrefix = "Vanilla Dungeon:";
        
        /// <summary>
        /// Default display duration for the title card in seconds.
        /// </summary>
        public const float DefaultDisplayDuration = 3f;
        
        /// <summary>
        /// Default fade duration for text elements in seconds.
        /// </summary>
        public const float DefaultFadeDuration = 0.5f;
        
        /// <summary>
        /// Default font weight for normal text (400 = Normal).
        /// </summary>
        public const int DefaultFontWeightNormal = 400;
        
        /// <summary>
        /// Default font weight for bold text (700 = Bold).
        /// </summary>
        public const int DefaultFontWeightBold = 700;
        
        /// <summary>
        /// Default title color in hex format (orange).
        /// </summary>
        public const string DefaultTitleColor = "#fe6001";
        
        /// <summary>
        /// Default top text color in hex format (orange).
        /// </summary>
        public const string DefaultTopTextColor = "#fe6001";
        
        /// <summary>
        /// Default interior text color in hex format (orange).
        /// </summary>
        public const string DefaultInteriorTextColor = "#fe6001";
        
        /// <summary>
        /// Maximum number of retry attempts for config generation.
        /// </summary>
        public const int MaxConfigRetryAttempts = 3;
        
        /// <summary>
        /// Base delay for config retry in seconds.
        /// </summary>
        public const float ConfigRetryBaseDelay = 3f;
        
        /// <summary>
        /// Maximum delay for config retry in seconds.
        /// </summary>
        public const float ConfigRetryMaxDelay = 30f;
        
        /// <summary>
        /// Delay for config initialization in seconds.
        /// </summary>
        public const float ConfigInitializationDelay = 5f;
        
        /// <summary>
        /// Plugin GUID for BepInEx and Harmony identification.
        /// </summary>
        public const string PluginGuid = "com.github.interiortitlecards";
        
        #endregion
    }
}