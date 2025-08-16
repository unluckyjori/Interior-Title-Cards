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
                titleCardText.fontSize = TitleCardConstants.TopTextFontSize;
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
                interiorNameText.fontSize = TitleCardConstants.BottomTextFontSize;
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
                
            // Hide the title card after the configured duration
            hideCardCoroutine = hudManager.StartCoroutine(HideTitleCardAfterDelay(configManager.DisplayDuration));
        }
        
        private IEnumerator HideTitleCardAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
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
    }
}