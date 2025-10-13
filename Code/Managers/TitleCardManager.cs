using BepInEx.Logging;
using InteriorTitleCards.Managers;
using InteriorTitleCards.Utils;
using LethalLevelLoader;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InteriorTitleCards.Managers
{
    /// <summary>
    /// Manages the creation, display, and behavior of the interior title card UI.
    /// </summary>
    public class TitleCardManager
    {
        #region Private Fields
        
        private readonly ManualLogSource logger;
        private readonly ConfigManager configManager;
        
        private GameObject titleCardObject;
        private TextMeshProUGUI titleCardText;
        private TextMeshProUGUI interiorNameText;
        private bool hasShownCard = false;
        private HUDManager hudManager;
        private Coroutine hideCardCoroutine;
        
        // Fade animation coroutines
        private Coroutine topTextFadeInCoroutine;
        private Coroutine topTextFadeOutCoroutine;
        private Coroutine interiorTextFadeInCoroutine;
        private Coroutine interiorTextFadeOutCoroutine;
        
        // Object pooling for title card objects
        private static readonly Queue<GameObject> titleCardPool = new Queue<GameObject>();
        private static readonly int MaxPoolSize = 10;
        
        // Custom images
        private GameObject topTextImageObject;
        private GameObject interiorNameImageObject;
        private UnityEngine.UI.Image topTextImageComponent;
        private UnityEngine.UI.Image interiorNameImageComponent;
        private Dictionary<string, Sprite> cachedSprites = new Dictionary<string, Sprite>();
        private HashSet<string> blacklistSet = new HashSet<string>();

        // Image loaded flags to prevent text fade-in when images are present
        private bool topTextImageLoaded = false;
        private bool interiorTextImageLoaded = false;

        // Memory management
        private const int MaxCachedSprites = 50;
        private Queue<string> spriteCacheOrder = new Queue<string>();

        // Directory structure initialization tracking
        private static bool baseDirectoryStructureInitialized = false;
        
        #endregion
        
        #region Constructor
        
        public TitleCardManager(ManualLogSource logger, ConfigManager configManager)
        {
            this.logger = logger;
            this.configManager = configManager;

            // Initialize blacklist set (will be re-initialized after configs are bound)
            InitializeBlacklist();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Initializes the blacklist set from the configuration.
        /// </summary>
        private void InitializeBlacklist()
        {
            string blacklist = configManager.ImageBlacklist;
            if (!string.IsNullOrEmpty(blacklist))
            {
                string[] entries = blacklist.Split(',');
                foreach (string entry in entries)
                {
                    string trimmedEntry = entry.Trim();
                    if (!string.IsNullOrEmpty(trimmedEntry))
                    {
                        // Normalize path separators and convert to lowercase for consistent comparison
                        blacklistSet.Add(trimmedEntry.Replace('\\', '/').ToLowerInvariant());
                    }
                }
            }
        }

        /// <summary>
        /// Re-initializes the blacklist set after configuration has been bound.
        /// This should be called after ConfigManager has finished initializing configs.
        /// </summary>
        public void ReInitializeBlacklist()
        {
            configManager.LogDebug("Re-initializing blacklist after config binding");
            blacklistSet.Clear();
            InitializeBlacklist();
            configManager.LogDebug($"Blacklist re-initialized with {blacklistSet.Count} entries");
        }
        
        /// <summary>
        /// Checks if an image path is blacklisted.
        /// </summary>
        /// <param name="imagePath">The image path to check.</param>
        /// <returns>True if the image is blacklisted, false otherwise.</returns>
        private bool IsImageBlacklisted(string imagePath)
        {
            configManager.LogDebug($"Checking if image is blacklisted: {imagePath}");

            if (string.IsNullOrEmpty(imagePath))
            {
                configManager.LogDebug("Image path is null or empty, considered blacklisted");
                return true;
            }

            // Normalize path separators for consistent comparison
            string normalizedPath = imagePath.Replace('\\', '/').ToLowerInvariant();

            // Check if the exact path is blacklisted
            bool isBlacklisted = blacklistSet.Contains(normalizedPath);
            configManager.LogDebug($"Image is blacklisted: {isBlacklisted}");

            // Also check if any parent directory is blacklisted
            if (!isBlacklisted)
            {
                string directoryPath = System.IO.Path.GetDirectoryName(normalizedPath);
                while (!string.IsNullOrEmpty(directoryPath))
                {
                    // Normalize directory path separators
                    directoryPath = directoryPath.Replace('\\', '/');

                    // Check both with and without trailing slash for user convenience
                    if (blacklistSet.Contains(directoryPath) || blacklistSet.Contains(directoryPath + "/"))
                    {
                        configManager.LogDebug($"Parent directory is blacklisted: {directoryPath}");
                        return true;
                    }
                    directoryPath = System.IO.Path.GetDirectoryName(directoryPath);
                }
            }

            return isBlacklisted;
        }
        
        /// <summary>
        /// Gets the base path for custom images.
        /// </summary>
        /// <returns>The base path for custom images.</returns>
        private string GetImagesBasePath()
        {
            // Get the BepInEx plugins directory
            string pluginsPath = BepInEx.Paths.PluginPath;
            if (string.IsNullOrEmpty(pluginsPath))
            {
                logger.LogError("BepInEx PluginPath is null or empty");
                return null;
            }

            string basePath = System.IO.Path.Combine(pluginsPath, "InteriorTitleCardsImages");
            configManager?.LogDebug($"GetImagesBasePath returning: {basePath}");
            return basePath;
        }

        /// <summary>
        /// Ensures the required directory structure exists for custom images.
        /// </summary>
        /// <returns>True if the directory structure is valid, false otherwise.</returns>
        private bool EnsureImageDirectoryStructure()
        {
            // Return early if already initialized
            if (baseDirectoryStructureInitialized)
            {
                return true;
            }

            try
            {
                string basePath = GetImagesBasePath();

                // Create base directory if it doesn't exist
                if (!System.IO.Directory.Exists(basePath))
                {
                    System.IO.Directory.CreateDirectory(basePath);
                    configManager.LogDebug($"Created base images directory: {basePath}");
                }

                // Create dev and user directories
                string devPath = System.IO.Path.Combine(basePath, "dev");
                string userPath = System.IO.Path.Combine(basePath, "user");

                if (!System.IO.Directory.Exists(devPath))
                {
                    System.IO.Directory.CreateDirectory(devPath);
                    configManager.LogDebug($"Created dev directory: {devPath}");
                }

                if (!System.IO.Directory.Exists(userPath))
                {
                    System.IO.Directory.CreateDirectory(userPath);
                    configManager.LogDebug($"Created user directory: {userPath}");
                }

                baseDirectoryStructureInitialized = true;
                return true;
            }
            catch (System.Exception ex)
            {
                logger.LogError($"Failed to create image directory structure: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sanitizes a dungeon name to prevent path traversal and other security issues.
        /// </summary>
        /// <param name="dungeonName">The original dungeon name.</param>
        /// <returns>A sanitized version safe for use in file paths.</returns>
        private string SanitizeDungeonName(string dungeonName)
        {
            if (string.IsNullOrEmpty(dungeonName))
            {
                return "Unknown";
            }

            // Remove or replace dangerous characters
            string sanitized = dungeonName;

            // Replace directory separators with underscores
            sanitized = sanitized.Replace("\\", "_").Replace("/", "_");

            // Remove other potentially dangerous characters
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (char invalidChar in invalidChars)
            {
                sanitized = sanitized.Replace(invalidChar, '_');
            }

            // Replace multiple underscores with single ones
            while (sanitized.Contains("__"))
            {
                sanitized = sanitized.Replace("__", "_");
            }

            // Trim whitespace and underscores
            sanitized = sanitized.Trim().Trim('_');

            // Ensure it's not empty after sanitization
            if (string.IsNullOrEmpty(sanitized))
            {
                sanitized = "Unknown";
            }

            // Limit length to prevent extremely long paths
            if (sanitized.Length > 100)
            {
                sanitized = sanitized.Substring(0, 100).TrimEnd('_');
            }

            configManager.LogDebug($"Sanitized dungeon name: '{dungeonName}' -> '{sanitized}'");
            return sanitized;
        }

        /// <summary>
        /// Validates and creates the directory path for a specific dungeon and image type.
        /// </summary>
        /// <param name="dungeonName">The name of the dungeon.</param>
        /// <param name="imageType">The type of image.</param>
        /// <param name="isDeveloper">Whether this is for developer images.</param>
        /// <returns>The validated directory path, or null if creation failed.</returns>
        private string ValidateAndCreateImageDirectory(string dungeonName, int imageType, bool isDeveloper)
        {
            try
            {
                // Sanitize the dungeon name for security
                string sanitizedDungeonName = SanitizeDungeonName(dungeonName);

                // Ensure base structure exists
                if (!EnsureImageDirectoryStructure())
                {
                    return null;
                }

                string basePath = GetImagesBasePath();
                string sourceDir = isDeveloper ? "dev" : "user";

                // Determine image type folder
                string imageTypeFolder;
                switch (imageType)
                {
                    case 0: // InteriorText
                        imageTypeFolder = "InteriorText";
                        break;
                    case 1: // TopText
                        imageTypeFolder = "TopText";
                        break;
                    case 2: // Combined
                        imageTypeFolder = "Combined";
                        break;
                    default:
                        configManager.LogDebug($"Invalid imageType: {imageType}");
                        return null;
                }

                // Construct the directory path
                string imageDirPath = System.IO.Path.Combine(
                    basePath,
                    sourceDir,
                    sanitizedDungeonName,
                    imageTypeFolder
                );

                // Create directory if it doesn't exist
                if (!System.IO.Directory.Exists(imageDirPath))
                {
                    System.IO.Directory.CreateDirectory(imageDirPath);
                    configManager.LogDebug($"Created image directory: {imageDirPath}");
                }

                return imageDirPath;
            }
            catch (System.Exception ex)
            {
                logger.LogError($"Failed to validate/create image directory for {dungeonName}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Loads a sprite from a file path.
        /// </summary>
        /// <param name="imagePath">The path to the image file.</param>
        /// <param name="imageType">The type of image (0 = InteriorText, 1 = TopText, 2 = Combined).</param>
        /// <returns>The loaded sprite, or null if loading failed.</returns>
        private Sprite LoadSpriteFromFile(string imagePath, int imageType = -1)
        {
            configManager.LogDebug($"LoadSpriteFromFile called with path: {imagePath}");

            if (string.IsNullOrEmpty(imagePath))
            {
                configManager.LogDebug("Image path is null or empty");
                return null;
            }

            if (!System.IO.File.Exists(imagePath))
            {
                configManager.LogDebug($"Image file does not exist: {imagePath}");
                return null;
            }

            // Check file size
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(imagePath);
            if (fileInfo.Length == 0)
            {
                logger.LogWarning($"Image file is empty: {imagePath}");
                return null;
            }

            if (fileInfo.Length > 10 * 1024 * 1024) // 10MB limit
            {
                logger.LogWarning($"Image file too large ({fileInfo.Length} bytes): {imagePath}");
                return null;
            }

            // Validate file extension
            string extension = System.IO.Path.GetExtension(imagePath).ToLowerInvariant();
            string[] supportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif" };
            if (!supportedExtensions.Contains(extension))
            {
                logger.LogWarning($"Unsupported image format: {extension} for file: {imagePath}");
                return null;
            }

            // Additional validation for PNG files
            if (extension == ".png")
            {
                try
                {
                    // Check if it's a valid PNG by reading the first 8 bytes (PNG signature)
                    using (System.IO.FileStream fs = new System.IO.FileStream(imagePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    {
                        byte[] pngSignature = new byte[8];
                        int bytesRead = fs.Read(pngSignature, 0, 8);
                        if (bytesRead < 8)
                        {
                            logger.LogWarning($"PNG file too small to be valid: {imagePath}");
                            return null;
                        }

                        // PNG signature: 89 50 4E 47 0D 0A 1A 0A
                        byte[] expectedSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                        for (int i = 0; i < 8; i++)
                        {
                            if (pngSignature[i] != expectedSignature[i])
                            {
                                logger.LogWarning($"Invalid PNG signature: {imagePath}");
                                return null;
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    logger.LogWarning($"Error validating PNG file {imagePath}: {ex.Message}");
                    return null;
                }
            }

            // Check cache first
            if (cachedSprites.ContainsKey(imagePath))
            {
                configManager.LogDebug("Image found in cache");
                return cachedSprites[imagePath];
            }

            Texture2D texture = null;
            try
            {
                configManager.LogDebug("Loading image from file");
                // Load image data
                byte[] imageData = System.IO.File.ReadAllBytes(imagePath);
                configManager.LogDebug($"Loaded {imageData.Length} bytes of image data");

                // Validate image data
                if (imageData.Length < 8)
                {
                    logger.LogWarning($"Image data too small to be valid: {imagePath}");
                    return null;
                }

                texture = new Texture2D(2, 2);

                // Load image into texture
                if (texture.LoadImage(imageData))
                {
                    configManager.LogDebug($"Texture loaded successfully. Format: {texture.format}, Size: {texture.width}x{texture.height}");

                    // Validate texture dimensions
                    if (texture.width <= 0 || texture.height <= 0)
                    {
                        logger.LogWarning($"Invalid texture dimensions {texture.width}x{texture.height}: {imagePath}");
                        UnityEngine.Object.Destroy(texture);
                        return null;
                    }

                    // Check for reasonable maximum dimensions
                    if (texture.width > 4096 || texture.height > 4096)
                    {
                        logger.LogWarning($"Texture dimensions too large {texture.width}x{texture.height}: {imagePath}");
                        UnityEngine.Object.Destroy(texture);
                        return null;
                    }

                    // Check for minimum dimensions
                    if (texture.width < 16 || texture.height < 16)
                    {
                        logger.LogWarning($"Texture dimensions too small {texture.width}x{texture.height}: {imagePath}");
                        UnityEngine.Object.Destroy(texture);
                        return null;
                    }

                    // Check aspect ratio (avoid extremely stretched images)
                    float aspectRatio = (float)texture.width / texture.height;
                    if (aspectRatio > 10.0f || aspectRatio < 0.1f)
                    {
                        logger.LogWarning($"Extreme aspect ratio {aspectRatio:F2}: {imagePath}");
                        UnityEngine.Object.Destroy(texture);
                        return null;
                    }

                    // Validate texture format is supported
                    if (texture.format == TextureFormat.DXT1 ||
                        texture.format == TextureFormat.DXT5 ||
                        texture.format == TextureFormat.BC7 ||
                        texture.format == TextureFormat.ETC_RGB4 ||
                        texture.format == TextureFormat.ETC2_RGB ||
                        texture.format == TextureFormat.ETC2_RGBA8)
                    {
                        // Compressed formats are generally fine
                        configManager.LogDebug($"Texture format: {texture.format} (compressed)");
                    }
                    else if (texture.format == TextureFormat.RGBA32 ||
                             texture.format == TextureFormat.RGB24 ||
                             texture.format == TextureFormat.BGRA32 ||
                             texture.format == TextureFormat.ARGB32 ||
                             texture.format == TextureFormat.RGBA4444 ||
                             texture.format == TextureFormat.RGB565)
                    {
                        // Common uncompressed formats are fine
                        configManager.LogDebug($"Texture format: {texture.format} (uncompressed)");
                    }
                    else
                    {
                        logger.LogWarning($"Unsupported texture format {texture.format}: {imagePath}");
                        UnityEngine.Object.Destroy(texture);
                        return null;
                    }

                    configManager.LogDebug($"Successfully loaded image into texture, size: {texture.width}x{texture.height}");

                    // Apply resizing based on image type
                    int maxWidth = 0, maxHeight = 0;
                    switch (imageType)
                    {
                        case 0: // InteriorText
                            maxWidth = configManager.InteriorTextImageWidth;
                            maxHeight = configManager.InteriorTextImageHeight;
                            break;
                        case 1: // TopText
                            maxWidth = configManager.TopTextImageWidth;
                            maxHeight = configManager.TopTextImageHeight;
                            break;
                        case 2: // Combined
                            maxWidth = configManager.CombinedImageWidth;
                            maxHeight = configManager.CombinedImageHeight;
                            break;
                    }

                    if (maxWidth > 0 || maxHeight > 0)
                    {
                        texture = ResizeTexture(texture, maxWidth, maxHeight);
                        configManager.LogDebug($"Resized texture to: {texture.width}x{texture.height}");
                    }

                    // Create sprite
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f) // Center pivot
                    );

                    if (sprite == null)
                    {
                        logger.LogError($"Failed to create sprite from texture: {imagePath}");
                        UnityEngine.Object.Destroy(texture);
                        return null;
                    }

                    configManager.LogDebug("Created sprite from texture");

                    // Cache the sprite with memory management
                    ManageSpriteCache(imagePath, sprite);
                    configManager.LogDebug("Cached sprite");

                    return sprite;
                }
                else
                {
                    logger.LogWarning($"Failed to load image data into texture (unsupported format?): {imagePath}");
                }
            }
            catch (System.IO.FileNotFoundException ex)
            {
                logger.LogError($"Image file not found: {imagePath} - {ex.Message}");
            }
            catch (System.IO.IOException ex)
            {
                logger.LogError($"IO error loading image: {imagePath} - {ex.Message}");
            }
            catch (System.UnauthorizedAccessException ex)
            {
                logger.LogError($"Access denied loading image: {imagePath} - {ex.Message}");
            }
            catch (System.OutOfMemoryException ex)
            {
                logger.LogError($"Out of memory loading image: {imagePath} - {ex.Message}");
            }
            catch (System.Exception ex)
            {
                logger.LogError($"Unexpected error loading sprite from {imagePath}: {ex.Message}");
                configManager.LogDebug($"Exception details: {ex.GetType().Name} - {ex.Message}");
            }
            finally
            {
                // Clean up texture if sprite creation failed
                if (texture != null && cachedSprites.ContainsKey(imagePath) == false)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            configManager.LogDebug("Failed to load sprite, returning null");
            return null;
        }

        /// <summary>
        /// Resizes a texture to fit within the specified dimensions while preserving aspect ratio.
        /// </summary>
        /// <param name="originalTexture">The original texture to resize.</param>
        /// <param name="maxWidth">Maximum width in pixels.</param>
        /// <param name="maxHeight">Maximum height in pixels.</param>
        /// <returns>The resized texture, or original if no resizing needed.</returns>
        private Texture2D ResizeTexture(Texture2D originalTexture, int maxWidth, int maxHeight)
        {
            // If either dimension is 0, use original size for that dimension
            int targetWidth = maxWidth <= 0 ? originalTexture.width : maxWidth;
            int targetHeight = maxHeight <= 0 ? originalTexture.height : maxHeight;

            // If no resizing needed, return original
            if (targetWidth >= originalTexture.width && targetHeight >= originalTexture.height)
            {
                return originalTexture;
            }

            // Calculate aspect ratio preserving dimensions
            float aspectRatio = (float)originalTexture.width / originalTexture.height;
            float targetAspectRatio = (float)targetWidth / targetHeight;

            int newWidth, newHeight;

            if (aspectRatio > targetAspectRatio)
            {
                // Image is wider than target aspect ratio
                newWidth = targetWidth;
                newHeight = Mathf.RoundToInt(targetWidth / aspectRatio);
            }
            else
            {
                // Image is taller than target aspect ratio
                newHeight = targetHeight;
                newWidth = Mathf.RoundToInt(targetHeight * aspectRatio);
            }

            // Create new texture
            Texture2D resizedTexture = new Texture2D(newWidth, newHeight, originalTexture.format, false);

            // Copy pixels with bilinear filtering for better quality
            Color[] pixels = originalTexture.GetPixels();
            Color[] resizedPixels = new Color[newWidth * newHeight];

            float xRatio = (float)originalTexture.width / newWidth;
            float yRatio = (float)originalTexture.height / newHeight;

            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    float srcX = x * xRatio;
                    float srcY = y * yRatio;

                    // Bilinear interpolation
                    int x1 = Mathf.FloorToInt(srcX);
                    int y1 = Mathf.FloorToInt(srcY);
                    int x2 = Mathf.Min(x1 + 1, originalTexture.width - 1);
                    int y2 = Mathf.Min(y1 + 1, originalTexture.height - 1);

                    float xLerp = srcX - x1;
                    float yLerp = srcY - y1;

                    Color c1 = pixels[y1 * originalTexture.width + x1];
                    Color c2 = pixels[y1 * originalTexture.width + x2];
                    Color c3 = pixels[y2 * originalTexture.width + x1];
                    Color c4 = pixels[y2 * originalTexture.width + x2];

                    Color top = Color.Lerp(c1, c2, xLerp);
                    Color bottom = Color.Lerp(c3, c4, xLerp);
                    resizedPixels[y * newWidth + x] = Color.Lerp(top, bottom, yLerp);
                }
            }

            resizedTexture.SetPixels(resizedPixels);
            resizedTexture.Apply();

            // Clean up original texture
            UnityEngine.Object.Destroy(originalTexture);

            return resizedTexture;
        }
        
        /// <summary>
        /// Gets the appropriate image path based on the image type and source.
        /// </summary>
        /// <param name="dungeonName">The name of the dungeon.</param>
        /// <param name="imageType">The type of image (0 = InteriorText, 1 = TopText, 2 = Combined).</param>
        /// <param name="isDeveloper">Whether to look in the developer directory.</param>
        /// <returns>The full path to the image, or null if not found.</returns>
        private string GetImagePath(string dungeonName, int imageType, bool isDeveloper)
        {
            configManager.LogDebug($"GetImagePath called - dungeon: {dungeonName}, imageType: {imageType}, isDeveloper: {isDeveloper}");

            // Sanitize dungeon name for security
            string sanitizedDungeonName = SanitizeDungeonName(dungeonName);

            // Validate and create directory structure
            string imageDirPath = ValidateAndCreateImageDirectory(sanitizedDungeonName, imageType, isDeveloper);
            if (string.IsNullOrEmpty(imageDirPath))
            {
                configManager.LogDebug("Failed to validate/create image directory");
                return null;
            }

            configManager.LogDebug($"Image directory path: {imageDirPath}");
            configManager.LogDebug("Directory exists, checking for image files");

            // Define possible image names to check (in order of preference)
            string[] possibleNames = {
                "image",        // Default name
                sanitizedDungeonName.ToLowerInvariant(),  // Dungeon name as filename
                sanitizedDungeonName.Replace("_", "").ToLowerInvariant(),  // Dungeon name without underscores
                "titlecard",    // Alternative name
                "card"          // Short alternative
            };

            string[] supportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif" };

            // Check each possible name with each extension
            foreach (string name in possibleNames)
            {
                foreach (string ext in supportedExtensions)
                {
                    string imagePath = System.IO.Path.Combine(imageDirPath, name + ext);
                    configManager.LogDebug($"Checking for {name}{ext}: {imagePath}");
                    if (System.IO.File.Exists(imagePath))
                    {
                        configManager.LogDebug($"Found {ext.ToUpper()} image: {name}{ext}");
                        return imagePath;
                    }
                }
            }

            configManager.LogDebug("No image file found");
            return null;
        }
        
        /// <summary>
        /// Loads a custom image for a dungeon.
        /// </summary>
        /// <param name="dungeonName">The name of the dungeon.</param>
        /// <param name="imageType">The type of image (0 = InteriorText, 1 = TopText, 2 = Combined).</param>
        /// <returns>The loaded sprite, or null if not found or blacklisted.</returns>
        private Sprite LoadCustomImage(string dungeonName, int imageType)
        {
            configManager.LogDebug($"LoadCustomImage called for dungeon: {dungeonName}, imageType: {imageType}");
            
            // Check if custom images are enabled
            if (!configManager.EnableCustomImages)
            {
                configManager.LogDebug("Custom images disabled, returning null");
                return null;
            }
                
            // Check image source mode
            int sourceMode = configManager.ImageSourceMode;
            configManager.LogDebug($"Image source mode: {sourceMode}");

			// Handle different source modes with proper priority
			Sprite resolvedSprite = null;

			switch (sourceMode)
			{
				case 0: // DeveloperOnly
					resolvedSprite = TryLoadImage(dungeonName, imageType, true);
					break;

				case 1: // UserOnly
					resolvedSprite = TryLoadImage(dungeonName, imageType, false);
					break;

				case 2: // BothDeveloperPriority
					// Try developer first, then user
					resolvedSprite = TryLoadImage(dungeonName, imageType, true)
						?? TryLoadImage(dungeonName, imageType, false);
					break;

				case 3: // BothUserPriority
					// Try user first, then developer
					resolvedSprite = TryLoadImage(dungeonName, imageType, false)
						?? TryLoadImage(dungeonName, imageType, true);
					break;

				default:
					configManager.LogDebug($"Invalid source mode: {sourceMode}, defaulting to developer priority");
					resolvedSprite = TryLoadImage(dungeonName, imageType, true)
						?? TryLoadImage(dungeonName, imageType, false);
					break;
			}

			if (resolvedSprite == null)
			{
				configManager.LogDebug("No valid image found, returning null");
			}

			return resolvedSprite;
        }
        
        /// <summary>
        /// Attempts to load an image from either developer or user directory.
        /// </summary>
        /// <param name="dungeonName">The name of the dungeon.</param>
        /// <param name="imageType">The type of image.</param>
        /// <param name="isDeveloper">Whether to look in the developer directory.</param>
        /// <returns>The loaded sprite, or null if not found or failed to load.</returns>
        private Sprite TryLoadImage(string dungeonName, int imageType, bool isDeveloper)
        {
            if (configManager == null)
            {
                logger.LogError("ConfigManager is null in TryLoadImage");
                return null;
            }

            string sourceType = isDeveloper ? "developer" : "user";
            configManager.LogDebug($"Checking {sourceType} images");

            string imagePath = GetImagePath(dungeonName, imageType, isDeveloper);
            configManager.LogDebug($"{sourceType} image path: {imagePath ?? "null"}");

            if (!string.IsNullOrEmpty(imagePath) && !IsImageBlacklisted(imagePath))
            {
                configManager.LogDebug($"{sourceType} image not blacklisted, attempting to load");
                Sprite sprite = LoadSpriteFromFile(imagePath, imageType);
                if (sprite != null)
                {
                    configManager.LogDebug($"Successfully loaded {sourceType} image");
                    return sprite;
                }
                else
                {
                    configManager.LogDebug($"Failed to load {sourceType} image");
                }
            }
            else
            {
                configManager.LogDebug($"{sourceType} image is null/empty or blacklisted");
            }

            return null;
        }

        /// <summary>
        /// Gets the image type folder name.
        /// </summary>
        /// <param name="imageType">The image type (0 = InteriorText, 1 = TopText, 2 = Combined).</param>
        /// <returns>The folder name.</returns>
        private string GetImageTypeFolder(int imageType)
        {
            switch (imageType)
            {
                case 0: return "InteriorText";
                case 1: return "TopText";
                case 2: return "Combined";
                default: return "";
            }
        }

        /// <summary>
        /// Manages the sprite cache, ensuring it doesn't exceed the maximum size.
        /// </summary>
        /// <param name="imagePath">The image path being cached.</param>
        /// <param name="sprite">The sprite to cache.</param>
        private void ManageSpriteCache(string imagePath, Sprite sprite)
        {
            // If already cached, update its position in the LRU order
            if (cachedSprites.ContainsKey(imagePath))
            {
                // Remove and re-add to update LRU order
                spriteCacheOrder = new Queue<string>(spriteCacheOrder.Where(x => x != imagePath));
            }
            else
            {
                // Check if we need to evict old sprites
                while (cachedSprites.Count >= MaxCachedSprites && spriteCacheOrder.Count > 0)
                {
                    string oldestPath = spriteCacheOrder.Dequeue();
                    if (cachedSprites.TryGetValue(oldestPath, out Sprite oldSprite))
                    {
                        // Destroy the texture to free memory
                        if (oldSprite.texture != null)
                        {
                            UnityEngine.Object.Destroy(oldSprite.texture);
                        }
                        UnityEngine.Object.Destroy(oldSprite);
                        cachedSprites.Remove(oldestPath);
                        configManager.LogDebug($"Evicted sprite from cache: {oldestPath}");
                    }
                }
            }

            // Add to cache and LRU order
            cachedSprites[imagePath] = sprite;
            spriteCacheOrder.Enqueue(imagePath);
            configManager.LogDebug($"Cached sprite. Cache size: {cachedSprites.Count}/{MaxCachedSprites}");
        }

        /// <summary>
        /// Clears the sprite cache to free memory.
        /// </summary>
        private void ClearSpriteCache()
        {
            configManager.LogDebug("Clearing sprite cache");

            foreach (var sprite in cachedSprites.Values)
            {
                if (sprite != null)
                {
                    // Destroy the texture to free memory
                    if (sprite.texture != null)
                    {
                        UnityEngine.Object.Destroy(sprite.texture);
                    }
                    UnityEngine.Object.Destroy(sprite);
                }
            }

            cachedSprites.Clear();
            spriteCacheOrder.Clear();
            configManager.LogDebug("Sprite cache cleared");
        }

        /// <summary>
        /// Gets cache statistics for debugging.
        /// </summary>
        /// <returns>A string with cache statistics.</returns>
        private string GetCacheStatistics()
        {
            long totalMemoryUsage = 0;
            int textureCount = 0;

            foreach (var sprite in cachedSprites.Values)
            {
                if (sprite != null && sprite.texture != null)
                {
                    // Estimate memory usage (4 bytes per pixel for RGBA)
                    totalMemoryUsage += (long)sprite.texture.width * sprite.texture.height * 4;
                    textureCount++;
                }
            }

            return $"Cache stats: {cachedSprites.Count} sprites, {textureCount} textures, ~{totalMemoryUsage / 1024} KB memory usage";
        }
        
        #endregion

        #region Public Methods

        /// <summary>
        /// Cleans up resources when the title card manager is being destroyed.
        /// </summary>
        public void Cleanup()
        {
            configManager.LogDebug("TitleCardManager cleanup called");
            ClearSpriteCache();
        }
        
        public void CreateTitleCard()
        {
            configManager.LogDebug("CreateTitleCard called");
            
            // Cache HUDManager reference
            hudManager = HUDManager.Instance;
            configManager.LogDebug($"HUDManager instance: {hudManager != null}");
            
            // Try to get from pool first
            if (titleCardPool.Count > 0)
            {
                configManager.LogDebug("Reusing title card from pool");
                titleCardObject = titleCardPool.Dequeue();
                titleCardObject.SetActive(false);
                
                // Reset components
                titleCardText = titleCardObject.GetComponentInChildren<TextMeshProUGUI>(true);
                interiorNameText = titleCardObject.GetComponentsInChildren<TextMeshProUGUI>(true)[1];
                
                configManager.LogDebug($"Reset components - titleCardText: {titleCardText != null}, interiorNameText: {interiorNameText != null}");
                
                // Reset text colors and alpha values for fade effects
                titleCardText.text = configManager.CustomTopText;
                titleCardText.color = configManager.TopTextColor;
                interiorNameText.color = configManager.InteriorTextColor;
                
                // Initialize text elements with transparent alpha if fade is enabled or if there are start delays
                if (configManager.TopTextFadeEnabled || configManager.TopTextStartDelay > 0)
                {
                    titleCardText.color = new Color(titleCardText.color.r, titleCardText.color.g, titleCardText.color.b, 0f);
                }
                
                if (configManager.InteriorTextFadeEnabled || configManager.InteriorTextStartDelay > 0)
                {
                    interiorNameText.color = new Color(interiorNameText.color.r, interiorNameText.color.g, interiorNameText.color.b, 0f);
                }
            }
            else
            {
                configManager.LogDebug("Creating new title card");
                // Create a new GameObject for our title card
                titleCardObject = new GameObject("InteriorTitleCard");
                titleCardObject.transform.SetParent(hudManager.HUDContainer.transform, false);
                
                // Add a CanvasRenderer component
                titleCardObject.AddComponent<CanvasRenderer>();
                
                // Setup main title card RectTransform
                SetupRectTransform(
                    titleCardObject, 
                    new Vector2(TitleCardConstants.CardWidth, TitleCardConstants.CardHeight), 
                    Vector2.zero
                );
                
                // Create container for the two text lines
                GameObject container = new GameObject("TitleCardContainer");
                container.transform.SetParent(titleCardObject.transform, false);
                
                // Setup container RectTransform
                SetupRectTransform(
                    container,
                    new Vector2(TitleCardConstants.CardWidth, TitleCardConstants.CardHeight),
                    Vector2.zero
                );
                
                // Create "NOW ENTERING..." text
                GameObject titleTextObject = new GameObject("TitleText");
                titleTextObject.transform.SetParent(container.transform, false);
                
                SetupRectTransform(
                    titleTextObject,
                    new Vector2(TitleCardConstants.CardWidth, TitleCardConstants.TextHeight),
                    configManager.TopTextPosition
                );
                
                titleCardText = titleTextObject.AddComponent<TextMeshProUGUI>();
                titleCardText.text = configManager.CustomTopText;
                titleCardText.fontSize = configManager.TopTextFontSize;
                titleCardText.alignment = TextAlignmentOptions.Center;
                titleCardText.fontStyle = (configManager.TopTextFontWeight >= TitleCardConstants.DefaultFontWeightBold) ? FontStyles.Bold : FontStyles.Normal;
                titleCardText.enableWordWrapping = false;
                
                // Create interior name text
                GameObject interiorTextObject = new GameObject("InteriorNameText");
                interiorTextObject.transform.SetParent(container.transform, false);
                
                SetupRectTransform(
                    interiorTextObject,
                    new Vector2(TitleCardConstants.CardWidth, TitleCardConstants.TextHeight),
                    configManager.InteriorTextPosition
                );
                
                interiorNameText = interiorTextObject.AddComponent<TextMeshProUGUI>();
                interiorNameText.text = "FACILITY";
                interiorNameText.fontSize = configManager.InteriorTextFontSize;
                interiorNameText.alignment = TextAlignmentOptions.Center;
                interiorNameText.fontStyle = (configManager.InteriorTextFontWeight >= TitleCardConstants.DefaultFontWeightBold) ? FontStyles.Bold : FontStyles.Normal;
                interiorNameText.enableWordWrapping = false;
                
                // Create image objects for custom images
                CreateImageObjects(container);
            }
            
            // Hide the title card by default
            titleCardObject.SetActive(false);
            configManager.LogDebug("Title card created and hidden");
        }
        
        /// <summary>
        /// Creates the image objects for displaying custom images with dynamic sizing.
        /// </summary>
        /// <param name="container">The container to parent the image objects to.</param>
        private void CreateImageObjects(GameObject container)
        {
            configManager.LogDebug("CreateImageObjects called");

            // Create top text image object
            topTextImageObject = new GameObject("TopTextImage");
            topTextImageObject.transform.SetParent(container.transform, false);
            topTextImageObject.SetActive(false);
            configManager.LogDebug("Created topTextImageObject");

            // Use configured size for top text images
            Vector2 topTextSize = new Vector2(
                configManager.TopTextImageWidth > 0 ? configManager.TopTextImageWidth : TitleCardConstants.CardWidth,
                configManager.TopTextImageHeight > 0 ? configManager.TopTextImageHeight : TitleCardConstants.TextHeight
            );

             SetupRectTransform(
                 topTextImageObject,
                 topTextSize,
                 configManager.TopImagePosition
             );

            topTextImageComponent = topTextImageObject.AddComponent<UnityEngine.UI.Image>();
            topTextImageComponent.color = new Color(1f, 1f, 1f, 0f); // Transparent by default
            configManager.LogDebug("Added Image component to topTextImageObject");

            // Create interior name image object
            interiorNameImageObject = new GameObject("InteriorNameImage");
            interiorNameImageObject.transform.SetParent(container.transform, false);
            interiorNameImageObject.SetActive(false);
            configManager.LogDebug("Created interiorNameImageObject");

            // Use configured size for interior text images
            Vector2 interiorTextSize = new Vector2(
                configManager.InteriorTextImageWidth > 0 ? configManager.InteriorTextImageWidth : TitleCardConstants.CardWidth,
                configManager.InteriorTextImageHeight > 0 ? configManager.InteriorTextImageHeight : TitleCardConstants.TextHeight
            );

             SetupRectTransform(
                 interiorNameImageObject,
                 interiorTextSize,
                 configManager.InteriorImagePosition
             );

            interiorNameImageComponent = interiorNameImageObject.AddComponent<UnityEngine.UI.Image>();
            interiorNameImageComponent.color = new Color(1f, 1f, 1f, 0f); // Transparent by default
            configManager.LogDebug("Added Image component to interiorNameImageObject");
        }
        
        /// <summary>
        /// Sets up a RectTransform with standardized center anchoring and positioning.
        /// </summary>
        /// <param name="gameObject">The GameObject to add the RectTransform to.</param>
        /// <param name="sizeDelta">The size of the RectTransform.</param>
        /// <param name="anchoredPosition">The anchored position of the RectTransform.</param>
        /// <returns>The configured RectTransform.</returns>
        private RectTransform SetupRectTransform(GameObject gameObject, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchorMin = new Vector2(TitleCardConstants.CenterAnchor, TitleCardConstants.CenterAnchor);
            rectTransform.anchorMax = new Vector2(TitleCardConstants.CenterAnchor, TitleCardConstants.CenterAnchor);
            rectTransform.pivot = new Vector2(TitleCardConstants.CenterAnchor, TitleCardConstants.CenterAnchor);
            rectTransform.anchoredPosition = anchoredPosition;
            return rectTransform;
        }
        
        #endregion
        
        public void OnEnterFacility()
        {
            configManager.LogDebug($"{nameof(OnEnterFacility)} called");
            
            if (hasShownCard) 
            {
                configManager.LogDebug("Card already shown, returning");
                return;
            }
            
            configManager.LogDebug("Card not yet shown, proceeding");
            
            // Cache references
            hudManager ??= HUDManager.Instance;
            configManager.LogDebug($"HUDManager cached: {hudManager != null}");
                
            if (hudManager == null)
            {
                configManager.LogDebug("HUDManager is null, returning");
                return;
            }
                
            // Get the current interior name from LethalLevelLoader
            string interiorName = "FACILITY"; // Default fallback
            configManager.LogDebug($"Default interior name: {interiorName}");
            
            configManager.LogDebug($"DungeonManager.CurrentExtendedDungeonFlow: {DungeonManager.CurrentExtendedDungeonFlow != null}");
            
            if (DungeonManager.CurrentExtendedDungeonFlow != null)
            {
                string dungeonName = DungeonManager.CurrentExtendedDungeonFlow.DungeonName;
                configManager.LogDebug($"Current dungeon name: {dungeonName}");
                
                // Get the interior name (with override if configured)
                interiorName = configManager.GetInteriorNameOverride(dungeonName);
                configManager.LogDebug($"Using interior name: {interiorName}");
            }
            else
            {
                configManager.LogDebug("DungeonManager.CurrentExtendedDungeonFlow is null");
            }
            
            configManager.LogDebug($"Showing title card with name: {interiorName}");
            ShowTitleCard(interiorName);
            hasShownCard = true;
            configManager.LogDebug("OnEnterFacility completed");
        }
        
        public void OnExitFacility()
        {
            hasShownCard = false;
        }
        
        public void ResetTitleCard()
        {
            configManager.LogDebug($"{nameof(ResetTitleCard)} called - resetting hasShownCard flag");
            hasShownCard = false;
            configManager.LogDebug("hasShownCard flag reset to false");
        }
        
        private void ShowTitleCard(string interiorName)
        {
            configManager.LogDebug($"ShowTitleCard called with interior: {interiorName}");
            
            if (titleCardObject == null || titleCardText == null || interiorNameText == null || hudManager == null)
            {
                configManager.LogDebug("Required components are null, returning");
                return;
            }
                
            configManager.LogDebug("Updating text colors from config");
            // Update text colors from config
            titleCardText.color = configManager.TopTextColor;
            interiorNameText.color = configManager.InteriorTextColor;
                
            interiorNameText.text = interiorName;
            titleCardObject.SetActive(true);
            configManager.LogDebug("Title card object activated");
            
            // Reset image objects to default state (visible text, hidden images)
            if (topTextImageObject != null)
            {
                topTextImageObject.SetActive(false);
                configManager.LogDebug("Reset topTextImageObject");
            }

            if (interiorNameImageObject != null)
            {
                interiorNameImageObject.SetActive(false);
                configManager.LogDebug("Reset interiorNameImageObject");
            }

            // Reset image loaded flags
            topTextImageLoaded = false;
            interiorTextImageLoaded = false;
                
            configManager.LogDebug("Handling custom images");
            // Handle custom images
            HandleCustomImages(interiorName);
            
            // Cancel any existing hide coroutine before starting a new one
            if (hideCardCoroutine != null)
            {
                configManager.LogDebug("Stopping existing hide coroutine");
                hudManager.StopCoroutine(hideCardCoroutine);
                hideCardCoroutine = null;
            }
            
            // Cancel any existing fade coroutines
            if (topTextFadeInCoroutine != null)
            {
                hudManager.StopCoroutine(topTextFadeInCoroutine);
                topTextFadeInCoroutine = null;
            }
            
            if (interiorTextFadeInCoroutine != null)
            {
                hudManager.StopCoroutine(interiorTextFadeInCoroutine);
                interiorTextFadeInCoroutine = null;
            }
            
            // Start fade in animations with delays
            // Only start fade-in animations if no image is loaded for this text element
            if (!topTextImageLoaded)
            {
                if (configManager.TopTextFadeEnabled)
                {
                    // Set initial alpha to 0 for fade in
                    Color initialColor = titleCardText.color;
                    titleCardText.color = new Color(initialColor.r, initialColor.g, initialColor.b, 0f);
                    topTextFadeInCoroutine = hudManager.StartCoroutine(DelayedFadeInText(titleCardText, configManager.TopTextStartDelay, configManager.TopTextFadeInDuration));
                }
                else
                {
                    // For non-fade text, we need to hide it initially if there's a delay, or show it immediately
                    if (configManager.TopTextStartDelay > 0)
                    {
                        // Set initial alpha to 0 for delayed show
                        Color initialColor = titleCardText.color;
                        titleCardText.color = new Color(initialColor.r, initialColor.g, initialColor.b, 0f);
                        topTextFadeInCoroutine = hudManager.StartCoroutine(DelayedShowText(titleCardText, configManager.TopTextStartDelay));
                    }
                    // If no delay and no fade, text is already visible at full alpha
                }
            }
            
            // Only start fade-in animations if no image is loaded for this text element
            if (!interiorTextImageLoaded)
            {
                if (configManager.InteriorTextFadeEnabled)
                {
                    // Set initial alpha to 0 for fade in
                    Color initialColor = interiorNameText.color;
                    interiorNameText.color = new Color(initialColor.r, initialColor.g, initialColor.b, 0f);
                    interiorTextFadeInCoroutine = hudManager.StartCoroutine(DelayedFadeInText(interiorNameText, configManager.InteriorTextStartDelay, configManager.InteriorTextFadeInDuration));
                }
                else
                {
                    // For non-fade text, we need to hide it initially if there's a delay, or show it immediately
                    if (configManager.InteriorTextStartDelay > 0)
                    {
                        // Set initial alpha to 0 for delayed show
                        Color initialColor = interiorNameText.color;
                        interiorNameText.color = new Color(initialColor.r, initialColor.g, initialColor.b, 0f);
                        interiorTextFadeInCoroutine = hudManager.StartCoroutine(DelayedShowText(interiorNameText, configManager.InteriorTextStartDelay));
                    }
                    // If no delay and no fade, text is already visible at full alpha
                }
            }
                
            // Hide the title card after the configured duration
            hideCardCoroutine = hudManager.StartCoroutine(HideTitleCardAfterDelay());
        }
        
        /// <summary>
        /// Handles loading and displaying custom images for the interior.
        /// </summary>
        /// <param name="interiorName">The name of the interior.</param>
        private void HandleCustomImages(string interiorName)
        {
            configManager.LogDebug($"HandleCustomImages called for interior: {interiorName}");

            // Reset image loaded flags first to prevent race conditions
            topTextImageLoaded = false;
            interiorTextImageLoaded = false;

            // Reset image objects
            if (topTextImageObject != null)
            {
                topTextImageObject.SetActive(false);
                configManager.LogDebug("Reset topTextImageObject");
            }

            if (interiorNameImageObject != null)
            {
                interiorNameImageObject.SetActive(false);
                configManager.LogDebug("Reset interiorNameImageObject");
            }

            // Reset text visibility to default state (visible)
            if (titleCardText != null)
            {
                Color textColor = configManager.TopTextColor;
                titleCardText.color = new Color(textColor.r, textColor.g, textColor.b, 1f);
            }

            if (interiorNameText != null)
            {
                Color textColor = configManager.InteriorTextColor;
                interiorNameText.color = new Color(textColor.r, textColor.g, textColor.b, 1f);
            }

            // Check if custom images are enabled
            if (!configManager.EnableCustomImages)
            {
                configManager.LogDebug("Custom images disabled, returning");
                return;
            }

            // Get display type
            int displayType = configManager.ImageDisplayType;
            configManager.LogDebug($"Image display type: {displayType}");

            // Handle top text image only
            if (displayType == 0) // Top text image only
            {
                configManager.LogDebug("Handling top text image only mode");
                Sprite topTextSprite = LoadCustomImage(interiorName, 1); // TopText
                if (topTextSprite != null && topTextImageComponent != null)
                {
                    configManager.LogDebug("Displaying top text image");
                    topTextImageComponent.sprite = topTextSprite;
                    topTextImageComponent.color = new Color(1f, 1f, 1f, 1f); // Opaque
                    topTextImageObject?.SetActive(true);

                    // Ensure image is centered in its container
                    topTextImageComponent.rectTransform.anchoredPosition = Vector2.zero;
                    topTextImageComponent.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    topTextImageComponent.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    topTextImageComponent.rectTransform.pivot = new Vector2(0.5f, 0.5f);

                    // Hide top text
                    if (titleCardText != null)
                        titleCardText.color = new Color(titleCardText.color.r, titleCardText.color.g, titleCardText.color.b, 0f);

                    // Mark top text image as loaded
                    topTextImageLoaded = true;
                }
                // Interior text remains visible (no image loaded)
                return;
            }

            // Handle interior text image only
            if (displayType == 1) // Interior text image only
            {
                configManager.LogDebug("Handling interior text image only mode");
                Sprite interiorNameSprite = LoadCustomImage(interiorName, 0); // InteriorText
                if (interiorNameSprite != null && interiorNameImageComponent != null)
                {
                    configManager.LogDebug("Displaying interior name image");
                    interiorNameImageComponent.sprite = interiorNameSprite;
                    interiorNameImageComponent.color = new Color(1f, 1f, 1f, 1f); // Opaque
                    interiorNameImageObject?.SetActive(true);

                    // Ensure image is centered in its container
                    interiorNameImageComponent.rectTransform.anchoredPosition = Vector2.zero;
                    interiorNameImageComponent.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    interiorNameImageComponent.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    interiorNameImageComponent.rectTransform.pivot = new Vector2(0.5f, 0.5f);

                    // Hide interior text
                    if (interiorNameText != null)
                        interiorNameText.color = new Color(interiorNameText.color.r, interiorNameText.color.g, interiorNameText.color.b, 0f);

                    // Mark interior text image as loaded
                    interiorTextImageLoaded = true;
                }
                // Top text remains visible (no image loaded)
                return;
            }

            // Handle both separate images
            if (displayType == 2) // Both separate images
            {
                configManager.LogDebug("Handling both separate images mode");
                // Load top text image
                Sprite topTextSprite = LoadCustomImage(interiorName, 1);
                if (topTextSprite != null && topTextImageComponent != null)
                {
                    topTextImageComponent.sprite = topTextSprite;
                    topTextImageComponent.color = new Color(1f, 1f, 1f, 1f);
                    topTextImageObject?.SetActive(true);
                    if (titleCardText != null)
                        titleCardText.color = new Color(titleCardText.color.r, titleCardText.color.g, titleCardText.color.b, 0f);
                    // Mark top text image as loaded
                    topTextImageLoaded = true;
                }

                // Load interior name image
                Sprite interiorNameSprite = LoadCustomImage(interiorName, 0);
                if (interiorNameSprite != null && interiorNameImageComponent != null)
                {
                    interiorNameImageComponent.sprite = interiorNameSprite;
                    interiorNameImageComponent.color = new Color(1f, 1f, 1f, 1f);
                    interiorNameImageObject?.SetActive(true);
                    if (interiorNameText != null)
                        interiorNameText.color = new Color(interiorNameText.color.r, interiorNameText.color.g, interiorNameText.color.b, 0f);
                    // Mark interior text image as loaded
                    interiorTextImageLoaded = true;
                }
                return;
            }

            // Handle combined image
            if (displayType == 3) // Combined image
            {
                configManager.LogDebug("Handling combined image mode");
                Sprite combinedSprite = LoadCustomImage(interiorName, 2);
                 if (combinedSprite != null && interiorNameImageComponent != null)
                 {
                     configManager.LogDebug("Displaying combined image");
                     interiorNameImageComponent.sprite = combinedSprite;
                     interiorNameImageComponent.color = new Color(1f, 1f, 1f, 1f);
                     interiorNameImageObject?.SetActive(true);

                     // Ensure image is positioned according to config
                     interiorNameImageComponent.rectTransform.anchoredPosition = configManager.CombinedImagePosition;
                     interiorNameImageComponent.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                     interiorNameImageComponent.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                     interiorNameImageComponent.rectTransform.pivot = new Vector2(0.5f, 0.5f);

                    // Hide both text elements
                    if (titleCardText != null)
                        titleCardText.color = new Color(titleCardText.color.r, titleCardText.color.g, titleCardText.color.b, 0f);
                    if (interiorNameText != null)
                        interiorNameText.color = new Color(interiorNameText.color.r, interiorNameText.color.g, interiorNameText.color.b, 0f);

                    // Mark both images as loaded (combined image affects both text areas)
                    topTextImageLoaded = true;
                    interiorTextImageLoaded = true;
                }
                else
                {
                    configManager.LogDebug("No combined image found, text will remain visible");
                }
            }
        }
        
        private IEnumerator FadeInText(TextMeshProUGUI textComponent, float duration)
        {
            Color startColor = textComponent.color;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
                textComponent.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
            
            // Set final color with full alpha
            textComponent.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
            
            // Clear the coroutine reference
            if (textComponent == titleCardText)
                topTextFadeInCoroutine = null;
            else if (textComponent == interiorNameText)
                interiorTextFadeInCoroutine = null;
        }
        
private IEnumerator HideTitleCardAfterDelay()
        {
            // Start separate fade out timers for each text element based on their individual display durations
            hudManager.StartCoroutine(HideTextElementAfterDelay(titleCardText, configManager.TopTextDisplayDuration, configManager.TopTextFadeOutDuration, configManager.TopTextFadeEnabled));
            hudManager.StartCoroutine(HideTextElementAfterDelay(interiorNameText, configManager.InteriorTextDisplayDuration, configManager.InteriorTextFadeOutDuration, configManager.InteriorTextFadeEnabled));
            
            // Also handle image fade out if they exist
            if (topTextImageObject != null && topTextImageObject.activeSelf)
            {
                // For simplicity, we'll use the same fade out duration as the text
                hudManager.StartCoroutine(HideImageElementAfterDelay(topTextImageComponent, configManager.TopTextDisplayDuration, configManager.TopTextFadeOutDuration, configManager.TopTextFadeEnabled));
            }
            
            if (interiorNameImageObject != null && interiorNameImageObject.activeSelf)
            {
                // For simplicity, we'll use the same fade out duration as the text
                hudManager.StartCoroutine(HideImageElementAfterDelay(interiorNameImageComponent, configManager.InteriorTextDisplayDuration, configManager.InteriorTextFadeOutDuration, configManager.InteriorTextFadeEnabled));
            }
            
            // Wait for the longer of the two display durations before hiding the entire card
            float maxDisplayDuration = Mathf.Max(configManager.TopTextDisplayDuration, configManager.InteriorTextDisplayDuration);
            yield return new WaitForSeconds(maxDisplayDuration);
            
            // Small additional delay to ensure fade out animations complete
            float maxFadeOutDuration = Mathf.Max(configManager.TopTextFadeOutDuration, configManager.InteriorTextFadeOutDuration);
            if (maxFadeOutDuration > 0)
            {
                yield return new WaitForSeconds(maxFadeOutDuration);
            }
            
            if (titleCardObject != null)
            {
                titleCardObject.SetActive(false);
                
                // Return to pool instead of destroying
                if (titleCardPool.Count < MaxPoolSize)
                {
                    titleCardPool.Enqueue(titleCardObject);
                }
            }
            
            hideCardCoroutine = null;
        }
        
        private IEnumerator HideTextElementAfterDelay(TextMeshProUGUI textComponent, float displayDuration, float fadeOutDuration, bool fadeEnabled)
        {
            // Wait for the individual display duration
            yield return new WaitForSeconds(displayDuration);

            // Skip fade out if an image is loaded for this text element
            bool hasImage = (textComponent == titleCardText && topTextImageLoaded) ||
                           (textComponent == interiorNameText && interiorTextImageLoaded);

            if (hasImage)
            {
                // Text should already be hidden (alpha = 0) due to image, keep it hidden
                textComponent.color = new Color(textComponent.color.r, textComponent.color.g, textComponent.color.b, 0f);
                yield break;
            }

            // Start fade out animation if enabled
            if (fadeEnabled && fadeOutDuration > 0)
            {
                // Stop any existing fade coroutine for this component
                if (textComponent == titleCardText && topTextFadeOutCoroutine != null)
                {
                    hudManager.StopCoroutine(topTextFadeOutCoroutine);
                }
                else if (textComponent == interiorNameText && interiorTextFadeOutCoroutine != null)
                {
                    hudManager.StopCoroutine(interiorTextFadeOutCoroutine);
                }
                
                // Start fade out animation and wait for it to complete
                Coroutine fadeCoroutine = null;
                if (textComponent == titleCardText)
                {
                    fadeCoroutine = hudManager.StartCoroutine(FadeOutText(titleCardText, fadeOutDuration));
                    topTextFadeOutCoroutine = fadeCoroutine;
                }
                else if (textComponent == interiorNameText)
                {
                    fadeCoroutine = hudManager.StartCoroutine(FadeOutText(interiorNameText, fadeOutDuration));
                    interiorTextFadeOutCoroutine = fadeCoroutine;
                }
                
                // Wait for fade out to complete
                if (fadeCoroutine != null)
                {
                    yield return fadeCoroutine;
                }
            }
            else
            {
                // Immediate hide without fade
                textComponent.color = new Color(textComponent.color.r, textComponent.color.g, textComponent.color.b, 0f);
            }
        }
        
        /// <summary>
        /// Hides an image element after a delay with optional fade out.
        /// </summary>
        /// <param name="imageComponent">The image component to hide.</param>
        /// <param name="displayDuration">How long to display the image before starting fade out.</param>
        /// <param name="fadeOutDuration">How long the fade out animation should take.</param>
        /// <param name="fadeEnabled">Whether fade out animation is enabled.</param>
        /// <returns>IEnumerator for coroutine.</returns>
        private IEnumerator HideImageElementAfterDelay(Image imageComponent, float displayDuration, float fadeOutDuration, bool fadeEnabled)
        {
            // Wait for the individual display duration
            yield return new WaitForSeconds(displayDuration);
            
            // Start fade out animation if enabled
            if (fadeEnabled && fadeOutDuration > 0)
            {
                // Start fade out animation and wait for it to complete
                Coroutine fadeCoroutine = hudManager.StartCoroutine(FadeOutImage(imageComponent, fadeOutDuration));
                yield return fadeCoroutine;
            }
            else
            {
                // Immediate hide without fade
                if (imageComponent != null)
                    imageComponent.color = new Color(imageComponent.color.r, imageComponent.color.g, imageComponent.color.b, 0f);
            }
        }
        
        private IEnumerator FadeOutText(TextMeshProUGUI textComponent, float duration)
        {
            Color startColor = textComponent.color;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
                textComponent.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
            
            // Set final color with zero alpha
            textComponent.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
            
            // Clear the coroutine reference
            if (textComponent == titleCardText)
                topTextFadeOutCoroutine = null;
            else if (textComponent == interiorNameText)
                interiorTextFadeOutCoroutine = null;
        }
        
        /// <summary>
        /// Fades out an image component over time.
        /// </summary>
        /// <param name="imageComponent">The image component to fade out.</param>
        /// <param name="duration">The duration of the fade out animation.</param>
        /// <returns>IEnumerator for coroutine.</returns>
        private IEnumerator FadeOutImage(Image imageComponent, float duration)
        {
            if (imageComponent == null)
                yield break;
                
            Color startColor = imageComponent.color;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
                imageComponent.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
            
            // Set final color with zero alpha
            imageComponent.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        }
        
        private IEnumerator DelayedShowText(TextMeshProUGUI textComponent, float delay)
        {
            // Wait for the specified delay
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
            
            // Then show the text with full alpha
            Color color = textComponent.color;
            textComponent.color = new Color(color.r, color.g, color.b, 1f);
            
            // Clear the coroutine reference
            if (textComponent == titleCardText)
                topTextFadeInCoroutine = null;
            else if (textComponent == interiorNameText)
                interiorTextFadeInCoroutine = null;
        }
        
        private IEnumerator DelayedFadeInText(TextMeshProUGUI textComponent, float delay, float duration)
        {
            // Wait for the specified delay
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
            
            // Then start fade in animation
            Coroutine fadeCoroutine = hudManager.StartCoroutine(FadeInText(textComponent, duration));
            
            // Wait for fade animation to complete
            yield return fadeCoroutine;
        }
    }
}