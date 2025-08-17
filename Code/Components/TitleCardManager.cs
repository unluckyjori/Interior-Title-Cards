using BepInEx.Logging;
using InteriorTitleCards.Config;
using LethalLevelLoader;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace InteriorTitleCards.Components
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
        
        #endregion
        
        #region Constructor
        
        public TitleCardManager(ManualLogSource logger, ConfigManager configManager)
        {
            this.logger = logger;
            this.configManager = configManager;
        }
        
        #endregion
        
        #region Public Methods
        
        public void CreateTitleCard()
        {
            // Cache HUDManager reference
            hudManager = HUDManager.Instance;
            
            // Try to get from pool first
            if (titleCardPool.Count > 0)
            {
                titleCardObject = titleCardPool.Dequeue();
                titleCardObject.SetActive(false);
                
                // Reset components
                titleCardText = titleCardObject.GetComponentInChildren<TextMeshProUGUI>(true);
                interiorNameText = titleCardObject.GetComponentsInChildren<TextMeshProUGUI>(true)[1];
                
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
                    new Vector2(0f, TitleCardConstants.TopTextOffset)
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
                    new Vector2(0f, TitleCardConstants.BottomTextOffset)
                );
                
                interiorNameText = interiorTextObject.AddComponent<TextMeshProUGUI>();
                interiorNameText.text = "FACILITY";
                interiorNameText.fontSize = configManager.InteriorTextFontSize;
                interiorNameText.alignment = TextAlignmentOptions.Center;
                interiorNameText.fontStyle = (configManager.InteriorTextFontWeight >= TitleCardConstants.DefaultFontWeightBold) ? FontStyles.Bold : FontStyles.Normal;
                interiorNameText.enableWordWrapping = false;
            }
            
            // Hide the title card by default
            titleCardObject.SetActive(false);
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
            
            // Cache references
            hudManager ??= HUDManager.Instance;
                
            if (hudManager == null)
            {
                configManager.LogDebug("HUDManager is null, returning");
                return;
            }
                
            // Get the current interior name from LethalLevelLoader
            string interiorName = "FACILITY"; // Default fallback
            configManager.LogDebug($"Default interior name: {interiorName}");
            
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
        }
        
        public void OnExitFacility()
        {
            hasShownCard = false;
        }
        
        public void ResetTitleCard()
        {
            configManager.LogDebug($"{nameof(ResetTitleCard)} called - resetting hasShownCard flag");
            hasShownCard = false;
        }
        
        private void ShowTitleCard(string interiorName)
        {
            if (titleCardObject == null || titleCardText == null || interiorNameText == null || hudManager == null)
                return;
                
            // Update text colors from config
            titleCardText.color = configManager.TopTextColor;
            interiorNameText.color = configManager.InteriorTextColor;
                
            interiorNameText.text = interiorName;
            titleCardObject.SetActive(true);
            
            // Cancel any existing hide coroutine before starting a new one
            if (hideCardCoroutine != null)
            {
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
                
            // Hide the title card after the configured duration
            hideCardCoroutine = hudManager.StartCoroutine(HideTitleCardAfterDelay());
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
        
        private IEnumerator DelayedFadeInText(TextMeshProUGUI textComponent, float delay, float duration)
        {
            // Wait for the specified delay
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
            
            // Then start the fade in animation
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
    }
}