using CodeLocks.Locks;
using JetBrains.Annotations;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;

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
                UnturnedChat.Say(player, CodeLocksPlugin.Instance!.Translate(key, placeholder), true);
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

            var codeLock = codeLockManager.GetCodeLock(lockable.InstanceId);
            if (codeLock == null)
            {
                // Add code lock
                codeLockManager.AddCodeLock(lockable.InstanceId, code, player.CSteamID.m_SteamID);

                Say("commands_codelock_code_added", codeStr);
                return;
            }

            if (codeLock.Users.Contains(player.CSteamID.m_SteamID))
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
