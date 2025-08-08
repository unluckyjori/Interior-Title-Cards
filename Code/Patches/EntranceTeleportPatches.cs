using HarmonyLib;

namespace InteriorTitleCards.Patches
{
    [HarmonyPatch(typeof(EntranceTeleport))]
    public class EntranceTeleportPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("TeleportPlayer")]
        public static void TeleportPlayerPostfix(EntranceTeleport __instance)
        {
            // When player enters the facility (not the ship)
            if (__instance.isEntranceToBuilding)
            {
                Plugin.Instance.OnPlayerEnterFacility();
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch("TeleportPlayerClientRpc")]
        public static void TeleportPlayerClientRpcPostfix(EntranceTeleport __instance)
        {
            // When player enters the facility (not the ship)
            if (__instance.isEntranceToBuilding)
            {
                Plugin.Instance.OnPlayerEnterFacility();
            }
        }
    }
}