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
            Plugin.Log.LogInfo("Initializing Interior Title Card Mod.");
            Plugin.Instance?.InitializeTitleCard();
        }
    }
}