using BepInEx.Logging;
using InteriorTitleCards.Config;
using LethalLevelLoader;
using System.Collections;
using TMPro;
using UnityEngine;

namespace InteriorTitleCards.Components
{
    public class TitleCardManager
    {
        private readonly ManualLogSource logger;
        private readonly ConfigManager configManager;
        
        private GameObject titleCardObject;
        private TextMeshProUGUI titleCardText;
        private TextMeshProUGUI interiorNameText;
        private bool hasShownCard = false;
        private HUDManager hudManager;
        private Coroutine hideCardCoroutine;
        
        public TitleCardManager(ManualLogSource logger, ConfigManager configManager)
        {
            this.logger = logger;
            this.configManager = configManager;
        }
        
        public void CreateTitleCard()
        {
            // Cache HUDManager reference
            hudManager = HUDManager.Instance;
            
            // Create a new GameObject for our title card
            titleCardObject = new GameObject("InteriorTitleCard");
            titleCardObject.transform.SetParent(hudManager.HUDContainer.transform, false);
            
            // Add a CanvasRenderer component
            titleCardObject.AddComponent<CanvasRenderer>();
            
            // Add a RectTransform component and set its properties
            RectTransform rectTransform = titleCardObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400f, 120f);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, 0f); // Center of screen
            
            // Create container for the two text lines
            GameObject container = new GameObject("TitleCardContainer");
            container.transform.SetParent(titleCardObject.transform, false);
            
            // Add a RectTransform component to the container
            RectTransform containerRectTransform = container.AddComponent<RectTransform>();
            containerRectTransform.sizeDelta = new Vector2(400f, 120f);
            containerRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            containerRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            containerRectTransform.pivot = new Vector2(0.5f, 0.5f);
            containerRectTransform.anchoredPosition = Vector2.zero;
            
            // Create "NOW ENTERING..." text
            GameObject titleTextObject = new GameObject("TitleText");
            titleTextObject.transform.SetParent(container.transform, false);
            
            RectTransform titleTextRectTransform = titleTextObject.AddComponent<RectTransform>();
            titleTextRectTransform.sizeDelta = new Vector2(400f, 40f);
            titleTextRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            titleTextRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            titleTextRectTransform.pivot = new Vector2(0.5f, 0.5f);
            titleTextRectTransform.anchoredPosition = new Vector2(0f, 20f);
            
            titleCardText = titleTextObject.AddComponent<TextMeshProUGUI>();
            titleCardText.text = configManager.CustomTopText;
            titleCardText.fontSize = 20;
            titleCardText.alignment = TextAlignmentOptions.Center;
            titleCardText.color = configManager.TitleColor;
            titleCardText.fontStyle = (configManager.TopTextFontWeight >= 700) ? FontStyles.Bold : FontStyles.Normal;
            titleCardText.enableWordWrapping = false;
            
            // Create interior name text
            GameObject interiorTextObject = new GameObject("InteriorNameText");
            interiorTextObject.transform.SetParent(container.transform, false);
            
            RectTransform interiorTextRectTransform = interiorTextObject.AddComponent<RectTransform>();
            interiorTextRectTransform.sizeDelta = new Vector2(400f, 40f);
            interiorTextRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            interiorTextRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            interiorTextRectTransform.pivot = new Vector2(0.5f, 0.5f);
            interiorTextRectTransform.anchoredPosition = new Vector2(0f, -20f);
            
            interiorNameText = interiorTextObject.AddComponent<TextMeshProUGUI>();
            interiorNameText.text = "FACILITY";
            interiorNameText.fontSize = 28;
            interiorNameText.alignment = TextAlignmentOptions.Center;
            interiorNameText.color = configManager.TitleColor;
            interiorNameText.fontStyle = (configManager.InteriorTextFontWeight >= 700) ? FontStyles.Bold : FontStyles.Normal;
            interiorNameText.enableWordWrapping = false;
            
            // Hide the title card by default
            titleCardObject.SetActive(false);
        }
        
        public void OnEnterFacility()
        {
            configManager.LogDebug("OnEnterFacility called");
            
            if (hasShownCard) 
            {
                configManager.LogDebug("Card already shown, returning");
                return;
            }
            
            // Cache references
            if (hudManager == null)
                hudManager = HUDManager.Instance;
                
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
            configManager.LogDebug("ResetTitleCard called - resetting hasShownCard flag");
            hasShownCard = false;
        }
        
        private void ShowTitleCard(string interiorName)
        {
            if (titleCardObject == null || titleCardText == null || interiorNameText == null || hudManager == null)
                return;
                
            // Update text colors from config
            Color titleColor = configManager.TitleColor;
            titleCardText.color = titleColor;
            interiorNameText.color = titleColor;
                
            interiorNameText.text = interiorName;
            titleCardObject.SetActive(true);
            
            // Cancel any existing hide coroutine before starting a new one
            if (hideCardCoroutine != null)
                hudManager.StopCoroutine(hideCardCoroutine);
                
            // Hide the title card after 3 seconds
            hideCardCoroutine = hudManager.StartCoroutine(HideTitleCardAfterDelay(3f));
        }
        
        private IEnumerator HideTitleCardAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (titleCardObject != null)
                titleCardObject.SetActive(false);
                
            hideCardCoroutine = null;
        }
    }
}