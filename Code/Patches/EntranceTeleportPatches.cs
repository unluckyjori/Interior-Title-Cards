using HarmonyLib;
using GameNetcodeStuff;

namespace InteriorTitleCards.Patches
{
    /// <summary>
    /// Harmony patches for EntranceTeleport to detect when players enter facilities.
    /// </summary>
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
                Plugin.Instance?.OnPlayerEnterFacility();
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch("TeleportPlayerClientRpc")]
        public static void TeleportPlayerClientRpcPostfix(EntranceTeleport __instance, int playerObj)
        {
            // Only show title card for the local player who entered the facility
            if (__instance.isEntranceToBuilding && 
                __instance.playersManager.allPlayerScripts[playerObj] == GameNetworkManager.Instance.localPlayerController)
            {
                Plugin.Instance?.OnPlayerEnterFacility();
            }
        }
    }
}