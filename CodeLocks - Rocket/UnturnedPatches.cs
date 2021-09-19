using HarmonyLib;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace CodeLocks
{
    public class UnturnedPatches
    {
        public delegate void BarricadesSave();
        public static event BarricadesSave? OnBarricadesSave;

        public delegate void BarricadeRegionSending(SteamPlayer player, byte x, byte y, NetId parentNetId);
        public static event BarricadeRegionSending? OnBarricadeRegionSending;

        public delegate void BarricadeRegionSent(SteamPlayer player, byte x, byte y, NetId parentNetId);
        public static event BarricadeRegionSent? OnBarricadeRegionSent;

        public delegate void CheckingDoorAccess(
            CSteamID steamId,
            InteractableDoor door,
            ref bool intercept,
            ref bool shouldAllow);
        public static event CheckingDoorAccess? OnCheckingDoorAccess;

        public delegate void UpdatingStateInternal(
            Transform barricade,
            byte[] state, int size,
            ref bool shouldReplicate);
        public static event UpdatingStateInternal? OnUpdatingStateInternal;

        public delegate void CheckingStorageAccess(
            CSteamID steamId,
            InteractableStorage storage,
            ref bool intercept,
            ref bool shouldAllow);
        public static event CheckingStorageAccess? OnCheckingStorageAccess;

        public delegate void BarricadeDestroyed(BarricadeDrop drop);
        public static event BarricadeDestroyed? OnBarricadeDestroyed;

        [HarmonyPatch]
        private class Patches
        {
            [HarmonyPatch(typeof(BarricadeManager), "save")]
            [HarmonyPrefix]
            private static void Save()
            {
                OnBarricadesSave?.Invoke();
            }

            [HarmonyPatch(typeof(BarricadeManager), "SendRegion")]
            [HarmonyPrefix]
            private static void PreSendRegion(SteamPlayer client, byte x, byte y, NetId parentNetId)
            {
                OnBarricadeRegionSending?.Invoke(client, x, y, parentNetId);
            }

            [HarmonyPatch(typeof(BarricadeManager), "SendRegion")]
            [HarmonyPostfix]
            private static void PostSendRegion(SteamPlayer client, byte x, byte y, NetId parentNetId)
            {
                OnBarricadeRegionSent?.Invoke(client, x, y, parentNetId);
            }

            [HarmonyPatch(typeof(InteractableDoor), "checkToggle")]
            [HarmonyPrefix]
            private static bool CheckToggle(InteractableDoor __instance, CSteamID enemyPlayer, ref bool __result)
            {
                var intercept = false;
                var shouldAllow = false;

                OnCheckingDoorAccess?.Invoke(enemyPlayer, __instance, ref intercept, ref shouldAllow);

                if (!intercept) return true;

                __result = shouldAllow;
                return false;
            }

            [HarmonyPatch(typeof(BarricadeManager), "updateStateInternal")]
            [HarmonyPrefix]
            private static void UpdatingStateInternal(Transform transform, byte[] state, int size, ref bool shouldReplicate)
            {
                OnUpdatingStateInternal?.Invoke(transform, state, size, ref shouldReplicate);
            }

            [HarmonyPatch(typeof(InteractableStorage), "checkStore")]
            [HarmonyPrefix]
            private static bool CheckStore(InteractableStorage __instance, CSteamID enemyPlayer, ref bool __result)
            {
                var intercept = false;
                var shouldAllow = false;

                OnCheckingStorageAccess?.Invoke(enemyPlayer, __instance, ref intercept, ref shouldAllow);

                if (!intercept) return true;

                __result = shouldAllow;
                return false;
            }

            [HarmonyPatch(typeof(BarricadeManager), "destroyBarricade", typeof(BarricadeDrop), typeof(byte), typeof(byte), typeof(ushort))]
            [HarmonyPrefix]
            [HarmonyPriority(Priority.Last)]
            private static void DestroyBarricade(BarricadeDrop barricade)
            {
                OnBarricadeDestroyed?.Invoke(barricade);
            }
        }
    }
}
