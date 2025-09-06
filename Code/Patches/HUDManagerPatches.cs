using HarmonyLib;

namespace InteriorTitleCards.Patches
{
    /// <summary>
    /// Harmony patches for HUDManager to initialize title card system.
    /// </summary>
    [HarmonyPatch(typeof(HUDManager))]
    public class HUDManagerPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        public static void HUDManagerAwakePostfix(HUDManager __instance)
        {
            try
            {
                // Validate instance
                if (__instance == null)
                {
                    Plugin.Log.LogWarning("HUDManager instance is null in HUDManagerAwakePostfix");
                    return;
                }

                Plugin.Log.LogInfo("Initializing Interior Title Card Mod.");

                if (Plugin.Instance != null)
                {
                    Plugin.Instance.InitializeTitleCard();
                }
                else
                {
                    Plugin.Log.LogWarning("Plugin instance is null in HUDManagerAwakePostfix");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error in HUDManagerAwakePostfix: {ex.Message}");
            }
        }
    }
}