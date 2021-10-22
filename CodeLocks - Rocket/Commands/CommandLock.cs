using CodeLocks.Locks;
using JetBrains.Annotations;
using Rocket.API;
using Rocket.Core;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CodeLocks.Commands
{
    [UsedImplicitly]
    public class CommandLock : IRocketCommand
    {
        public string Name => "lock";

        public string Help => "Adds a code lock to an object, or changes if one already exists.";

        public string Syntax => "<code>";

        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>()
        {
        };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer) caller;

            void Say(string key, params object[] placeholder)
            {
                ChatManager.say(player.CSteamID, CodeLocksPlugin.Instance!.Translate(key, placeholder), Color.green,
                    true);
            }

            if (command.Length == 0 || (command.Length == 1 && command[0].ToLower() == "help"))
            {
                Say("commands_codelock_help");
                return;
            }

            if (command.Length != 1)
            {
                Say("invalid_parameters");
                return;
            }

            var codeStr = command[0];
            if (codeStr.Length != 4 || codeStr.Any(chr => !char.IsDigit(chr)))
            {
                Say("commands_codelock_invalid_code", codeStr);
                return;
            }

            var code = CodeLockInfo.ParseCode(codeStr);

            var lockable = LockableInteractable.RaycastForInteractable(player.Player);
            if (lockable == null)
            {
                Say("commands_codelock_no_lockable_object");
                return;
            }

            var codeLockManager = CodeLocksPlugin.Instance!.CodeLockManager;

            if (CodeLocksPlugin.Instance.Configuration.Instance.OnlyOwnerCanLock)
            {
                if (lockable.Barricade.GetServersideData().owner != player.CSteamID.m_SteamID)
                {
                    Say("commands_codelock_no_access");
                    return;
                }
            }

            var codeLock = codeLockManager.GetCodeLock(lockable.InstanceId);
            if (codeLock == null)
            {
                // Add code lock
                codeLockManager.AddCodeLock(lockable.InstanceId, code, player.CSteamID.m_SteamID);

                Say("commands_codelock_code_added", codeStr);
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

            if (canChangeLock)
            {
                codeLockManager.RemoveCodeLock(lockable.InstanceId);

                codeLockManager.AddCodeLock(lockable.InstanceId, code, player.CSteamID.m_SteamID);

                Say("commands_codelock_code_changed", codeStr);
            }
            else
            {
                Say("commands_codelock_no_access");
            }
        }
    }
}
