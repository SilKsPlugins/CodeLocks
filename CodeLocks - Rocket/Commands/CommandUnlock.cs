using CodeLocks.Locks;
using JetBrains.Annotations;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using System.Linq;
using Rocket.Core;
using UnityEngine;

namespace CodeLocks.Commands
{
    [UsedImplicitly]
    public class CommandUnlock : IRocketCommand
    {
        public string Name => "unlock";

        public string Help => "Removes the code lock from an object.";

        public string Syntax => "";

        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public List<string> Aliases => new();

        public List<string> Permissions => new();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer) caller;

            void Say(string key, params object[] placeholder)
            {
                ChatManager.say(player.CSteamID, CodeLocksPlugin.Instance!.Translate(key, placeholder), Color.green,
                    true);
            }

            var lockable = LockableInteractable.RaycastForInteractable(player.Player);

            if (lockable == null)
            {
                Say("commands_codelock_no_lockable_object");
                return;
            }

            var codeLockManager = CodeLocksPlugin.Instance!.CodeLockManager;

            var codeLock = codeLockManager.GetCodeLock(lockable.InstanceId);

            if (codeLock == null)
            {
                Say("commands_codelock_no_code");
                return;
            }

            var canChangeLock = false;

            if (codeLock.Users.Contains(player.CSteamID.m_SteamID))
            {
                if (codeLock.Users.First() == player.CSteamID.m_SteamID)
                    canChangeLock = true;
                else if (CodeLocksPlugin.Instance.Configuration.Instance.NonOwnerCanChangeCode)
                    canChangeLock = true;
            }

            if (!canChangeLock &&
                R.Permissions.HasPermission(player, "bypasslock"))
                canChangeLock = true;

            if (!canChangeLock)
            {
                Say("commands_codelock_no_access");
                return;
            }
         
            codeLockManager.RemoveCodeLock(lockable.InstanceId);

            Say("commands_codelock_code_removed");
        }
    }
}
