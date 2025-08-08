using HarmonyLib;

namespace InteriorTitleCards.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    public class HUDManagerPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        public static void HUDManagerAwakePostfix(HUDManager __instance)
        {
            Plugin.Log.LogInfo("Initializing Interior Title Card Mod.");
            Plugin.Instance.InitializeTitleCard();
        }
    }
}