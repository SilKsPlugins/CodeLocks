using HarmonyLib;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CodeLocks.Locks
{
    public class ObjectManager
    {
        private static readonly ClientInstanceMethod<byte[]> s_SendUpdateState;

        private static readonly ClientInstanceMethod<ulong, ulong> s_SendOwnerAndGroup;

        static ObjectManager()
        {
            s_SendUpdateState =
                AccessTools.StaticFieldRefAccess<BarricadeDrop, ClientInstanceMethod<byte[]>>("SendUpdateState");

            s_SendOwnerAndGroup =
                AccessTools.StaticFieldRefAccess<BarricadeDrop, ClientInstanceMethod<ulong, ulong>>(
                    "SendOwnerAndGroup");
        }

        private readonly Lazy<CodeLockManager> _codeLockManager;

        public ObjectManager(Lazy<CodeLockManager> codeLockManager)
        {
            _codeLockManager = codeLockManager;
        }

        public void Load()
        {
            UnturnedPatches.OnBarricadeRegionSending += OnBarricadeRegionSending;
            UnturnedPatches.OnBarricadeRegionSent += OnBarricadeRegionSent;
            UnturnedPatches.OnUpdatingStateInternal += OnUpdatingStateInternal;
            UnturnedPatches.OnBarricadeDestroyed += OnBarricadeDestroyed;
        }

        public void Unload()
        {
            UnturnedPatches.OnBarricadeRegionSending -= OnBarricadeRegionSending;
            UnturnedPatches.OnBarricadeRegionSent -= OnBarricadeRegionSent;
            UnturnedPatches.OnUpdatingStateInternal -= OnUpdatingStateInternal;
            UnturnedPatches.OnBarricadeDestroyed -= OnBarricadeDestroyed;
        }

        public bool TryGetBarricade(uint instanceId, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion region, out BarricadeDrop drop)
        {
            x = 0;
            y = 0;
            plant = 0;
            index = 0;
            region = null!;
            drop = null!;

            foreach (var vehicleRegion in BarricadeManager.vehicleRegions)
            {
                var data = vehicleRegion.findBarricadeByInstanceID(instanceId);

                var model = vehicleRegion.drops.FirstOrDefault(d => d.instanceID == instanceId)?.model;

                if (model == null) continue;

                return BarricadeManager.tryGetInfo(model, out x, out y, out plant, out index, out region, out drop);
            }

            foreach (var barricadeRegion in BarricadeManager.regions)
            {
                var data = barricadeRegion.findBarricadeByInstanceID(instanceId);

                var model = barricadeRegion.drops.FirstOrDefault(d => d.instanceID == instanceId)?.model;

                if (model == null) continue;

                return BarricadeManager.tryGetInfo(model, out x, out y, out plant, out index, out region, out drop);
            }

            return false;
        }
        
        private void ModifyStateForClient(Interactable interactable, CSteamID steamId, byte[] state)
        {
            switch (interactable)
            {
                case InteractableStorage:
                case InteractableDoor:
                    var playerIdBytes = BitConverter.GetBytes(steamId.m_SteamID);
                    Array.Copy(playerIdBytes, 0, state, 0, 8);
                    break;

                default:
                    throw new Exception("Unsupported interactable type: " + interactable.GetType().FullName);
            }
        }

        private static readonly byte[] NullIdBytes = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};

        public byte[]? GetInteractableState(Interactable interactable)
        {
            byte[]? state = null;

            switch (interactable)
            {
                case InteractableDoor door:
                    state = new byte[17];
                    
                    Array.Copy(NullIdBytes, 0, state, 0, 16);

                    state[16] = (byte)(door.isOpen ? 1 : 0);
                    break;

                case InteractableStorage storage:

                    void OnStateRebuilt(InteractableStorage storage2, byte[] newState, int size)
                    {
                        state = newState;
                    }

                    storage.onStateRebuilt += OnStateRebuilt;

                    storage.rebuildState();

                    // ReSharper disable once DelegateSubtraction
                    storage.onStateRebuilt -= OnStateRebuilt;

                    break;

                default:
                    throw new Exception("Unsupported interactable type: " + interactable.GetType().FullName);
            }

            return state;
        }

        public bool ShouldSendRegion(Player player, byte x, byte y, ushort plant)
        {
            return plant != 65535 ||
                   Regions.checkArea(x, y,
                       player.movement.region_x,
                       player.movement.region_y,
                       BarricadeManager.BARRICADE_REGIONS);
        }

        public void SendBarricadeDataToClient(
            CSteamID steamId,
            byte x, byte y,
            ushort plant, ushort index,
            byte[] state, BarricadeDrop drop)
        {
            state = (byte[])state.Clone();

            ModifyStateForClient(drop.interactable, steamId, state);

            var transportConnection = Provider.findTransportConnection(steamId) ??
                                      throw new Exception("Could not get transport connection for player");

            s_SendUpdateState.Invoke(drop.GetNetId(), ENetReliability.Reliable, transportConnection, state);

            if (drop.interactable is not InteractableDoor)
            {
                s_SendOwnerAndGroup.Invoke(drop.GetNetId(), ENetReliability.Reliable, transportConnection,
                    steamId.m_SteamID, 0);
            }
        }

        public bool UpdateBarricade(CodeLockInfo codeLock)
        {
            if (!TryGetBarricade(codeLock.InstanceId,
                out var x, out var y,
                out var plant, out var index,
                out var region, out var drop))
                return false;

            var data = region.barricades[index];

            var state = GetInteractableState(drop.interactable);

            if (state == null) return false;

            foreach (var player in Provider.clients.Where(p => p?.player != null))
            {
                var steamId = player.playerID.steamID;

                if (!ShouldSendRegion(player.player, x, y, plant)) continue;

                SendBarricadeDataToClient(steamId, x, y, plant, index, state, drop);
            }

            return true;
        }

        public bool UpdateBarricade(CodeLockInfo codeLock, CSteamID steamId)
        {
            if (!TryGetBarricade(codeLock.InstanceId,
                out var x, out var y,
                out var plant, out var index,
                out var region, out var drop))
                return false;

            var state = GetInteractableState(drop.interactable);

            if (state == null) return false;

            SendBarricadeDataToClient(steamId, x, y, plant, index, state, drop);

            return true;
        }

        public void ChangeOwnerAndGroup(uint instanceId, ulong newOwner, ulong newGroup)
        {
            if (!TryGetBarricade(instanceId,
                out var x, out var y,
                out var plant, out var index,
                out var region, out var drop))
                return;

            BarricadeManager.changeOwnerAndGroup(drop.model, newOwner, newGroup);
        }

        private System.Action? _restoreBarricadeRegion;

        private void OnBarricadeRegionSending(SteamPlayer player, byte x, byte y, NetId parentNetId)
        {
            BarricadeRegion region;
            
            if (parentNetId == NetId.INVALID)
            {
                if (!BarricadeManager.tryGetRegion(x, y, ushort.MaxValue, out region))
                {
                    return;
                }
            }
            else
            {
                region = NetIdRegistry.Get<BarricadeRegion>(parentNetId);
                if (region == null)
                {
                    return;
                }
            }

            if (region.barricades.Count == 0 || region.drops.Count != region.barricades.Count) return;

            var changedBarricades = new List<Tuple<BarricadeData, ulong, ulong, byte[]>>();

            for (var i = 0; i < region.barricades.Count; i++)
            {
                var barricade = region.barricades[i];
                var drop = region.drops[i];

                var codeLock = _codeLockManager.Value.GetCodeLock(barricade.instanceID);
                if (codeLock == null) continue;

                changedBarricades.Add(new Tuple<BarricadeData, ulong, ulong, byte[]>(barricade, barricade.owner, barricade.group, barricade.barricade.state));

                barricade.owner = player.playerID.steamID.m_SteamID;
                barricade.group = 0;

                var state = barricade.barricade.state.ToArray();
                ModifyStateForClient(drop.interactable, player.playerID.steamID, state);

                barricade.barricade.state = state;
            }

            _restoreBarricadeRegion = () =>
            {
                foreach (var barricade in changedBarricades)
                {
                    barricade.Item1.owner = barricade.Item2;
                    barricade.Item1.group = barricade.Item3;
                    barricade.Item1.barricade.state = barricade.Item4;
                }
            };
        }

        private void OnBarricadeRegionSent(SteamPlayer player, byte x, byte y, NetId parentNetId)
        {
            _restoreBarricadeRegion?.Invoke();
        }

        private void OnUpdatingStateInternal(Transform barricade, byte[] state, int size, ref bool shouldReplicate)
        {
            if (!shouldReplicate) return;

            if (!BarricadeManager.tryGetInfo(barricade,
                out var x, out var y,
                out var plant, out var index,
                out var barricadeRegion, out var drop)) return;

            if (drop.interactable is not InteractableStorage && drop.interactable is not InteractableDoor) return;

            shouldReplicate = false;

            foreach (var player in Provider.clients.Where(p => p?.player != null))
            {
                var steamId = player.playerID.steamID;

                if (!ShouldSendRegion(player.player, x, y, plant)) continue;

                SendBarricadeDataToClient(steamId, x, y, plant, index, state, drop);
            }
        }

        private void OnBarricadeDestroyed(BarricadeDrop drop)
        {
            if (_codeLockManager.Value.GetCodeLock(drop.instanceID) != null)
                _codeLockManager.Value.RemoveCodeLock(drop.instanceID);
        }
    }
}
