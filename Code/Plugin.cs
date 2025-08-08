using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using InteriorTitleCards.Components;
using InteriorTitleCards.Config;

namespace InteriorTitleCards
{
    [BepInPlugin(TitleCardConstants.PluginGuid, "Interior Title Cards", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        #region Private Fields
        
        internal static Plugin Instance;
        internal static ManualLogSource Log;
        
        private Harmony _harmony;
        private ConfigManager configManager;
        private TitleCardManager titleCardManager;
        
        #endregion

        #region Unity Lifecycle Methods
        
        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo("Initializing Interior Title Cards plugin");

            // Initialize configuration manager
            configManager = new ConfigManager(Config, Log, this);
            
            // Initialize title card manager
            titleCardManager = new TitleCardManager(Log, configManager);

            // Apply Harmony patches
            _harmony = new Harmony(TitleCardConstants.PluginGuid);
            _harmony.PatchAll();
            Log.LogInfo("Harmony patches applied");
        }
        
        private void Start()
        {
            // Initialize configs after LethalLevelLoader has loaded its content
            configManager?.Initialize();
        }
        
        #endregion
        
        #region Internal Methods
        
        internal void InitializeTitleCard()
        {
            titleCardManager?.CreateTitleCard();
        }
        
        internal void OnPlayerEnterFacility()
        {
            titleCardManager?.OnEnterFacility();
        }
        
        internal void OnPlayerExitFacility()
        {
            titleCardManager?.OnExitFacility();
        }
        
        internal void ResetTitleCard()
        {
            titleCardManager?.ResetTitleCard();
        }
        
        #endregion
    }
}