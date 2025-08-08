using HarmonyLib;

namespace InteriorTitleCards.Patches
{
    /// <summary>
    /// Harmony patches for StartOfRound to handle title card state resets.
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound))]
    public class StartOfRoundPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("openingDoorsSequence")]
        public static void OpeningDoorsSequencePostfix()
        {
            // Reset the title card when the ship doors open (landing on a moon)
            Plugin.Log.LogInfo("Ship doors opening - resetting title card state");
            Plugin.Instance?.ResetTitleCard();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch("ShipLeave")]
        public static void ShipLeavePostfix()
        {
            // Also reset when the ship leaves
            Plugin.Log.LogInfo("Ship leaving - resetting title card state");
            Plugin.Instance?.ResetTitleCard();
        }
    }
}