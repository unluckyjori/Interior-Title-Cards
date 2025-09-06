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
            try
            {
                // Reset the title card when the ship doors open (landing on a moon)
                Plugin.Log.LogInfo("Ship doors opening - resetting title card state");

                if (Plugin.Instance != null)
                {
                    Plugin.Instance.ResetTitleCard();
                }
                else
                {
                    Plugin.Log.LogWarning("Plugin instance is null in OpeningDoorsSequencePostfix");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error in OpeningDoorsSequencePostfix: {ex.Message}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("ShipLeave")]
        public static void ShipLeavePostfix()
        {
            try
            {
                // Also reset when the ship leaves
                Plugin.Log.LogInfo("Ship leaving - resetting title card state");

                if (Plugin.Instance != null)
                {
                    Plugin.Instance.ResetTitleCard();
                }
                else
                {
                    Plugin.Log.LogWarning("Plugin instance is null in ShipLeavePostfix");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error in ShipLeavePostfix: {ex.Message}");
            }
        }
    }
}