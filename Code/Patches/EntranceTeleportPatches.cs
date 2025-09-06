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
            try
            {
                // Validate instance
                if (__instance == null)
                {
                    Plugin.Log.LogWarning("EntranceTeleport instance is null in TeleportPlayerPostfix");
                    return;
                }

                // When player enters the facility (not the ship)
                if (__instance.isEntranceToBuilding)
                {
                    if (Plugin.Instance != null)
                    {
                        Plugin.Instance.OnPlayerEnterFacility();
                    }
                    else
                    {
                        Plugin.Log.LogWarning("Plugin instance is null in TeleportPlayerPostfix");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error in TeleportPlayerPostfix: {ex.Message}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("TeleportPlayerClientRpc")]
        public static void TeleportPlayerClientRpcPostfix(EntranceTeleport __instance, int playerObj)
        {
            try
            {
                // Validate instance
                if (__instance == null)
                {
                    Plugin.Log.LogWarning("EntranceTeleport instance is null in TeleportPlayerClientRpcPostfix");
                    return;
                }

                // Validate playerObj bounds
                if (__instance.playersManager == null ||
                    __instance.playersManager.allPlayerScripts == null ||
                    playerObj < 0 ||
                    playerObj >= __instance.playersManager.allPlayerScripts.Length)
                {
                    Plugin.Log.LogWarning($"Invalid playerObj {playerObj} in TeleportPlayerClientRpcPostfix");
                    return;
                }

                // Only show title card for the local player who entered the facility
                if (__instance.isEntranceToBuilding &&
                    __instance.playersManager.allPlayerScripts[playerObj] == GameNetworkManager.Instance.localPlayerController)
                {
                    if (Plugin.Instance != null)
                    {
                        Plugin.Instance.OnPlayerEnterFacility();
                    }
                    else
                    {
                        Plugin.Log.LogWarning("Plugin instance is null in TeleportPlayerClientRpcPostfix");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error in TeleportPlayerClientRpcPostfix: {ex.Message}");
            }
        }
    }
}