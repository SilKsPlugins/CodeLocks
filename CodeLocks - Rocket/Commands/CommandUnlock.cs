using CodeLocks.Locks;
using JetBrains.Annotations;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;

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
                UnturnedChat.Say(player, CodeLocksPlugin.Instance!.Translate(key, placeholder), true);
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

            if (!codeLock.Users.Contains(player.CSteamID.m_SteamID))
            {
                Say("commands_codelock_no_access");
                return;
            }
         
            codeLockManager.RemoveCodeLock(lockable.InstanceId);

            Say("commands_codelock_code_removed");
        }
    }
}
