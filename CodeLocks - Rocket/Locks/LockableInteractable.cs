using SDG.Unturned;
using System;
using System.Linq;
using UnityEngine;

namespace CodeLocks.Locks
{
    public class LockableInteractable
    {
        public static readonly Type[] SupportedInteractableTypes = new[]
        {
            typeof(InteractableDoor),
            typeof(InteractableStorage)
        };

        public uint InstanceId { get; }

        public Interactable Interactable { get; }

        public BarricadeDrop Barricade { get; }

        public static LockableInteractable? RaycastForInteractable(Player player)
        {
            var raycastInfo = DamageTool.raycast(
                new Ray(player.look.aim.position, player.look.aim.forward),
                3f, RayMasks.BARRICADE_INTERACT | RayMasks.STRUCTURE_INTERACT,
                player);

            var drop = BarricadeManager.FindBarricadeByRootTransform(raycastInfo.transform);

            if (drop?.interactable == null) return null;

            return SupportedInteractableTypes.Contains(drop.interactable.GetType())
                ? new LockableInteractable(drop.instanceID, drop.interactable, drop)
                : null;
        }

        private LockableInteractable(uint instanceId, Interactable interactable, BarricadeDrop barricade)
        {
            InstanceId = instanceId;
            Interactable = interactable;
            Barricade = barricade;
        }
    }
}