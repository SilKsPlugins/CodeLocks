using CodeLocks.Locks;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.API.Permissions;
using OpenMod.API.Prioritization;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using System;
using System.Linq;

namespace CodeLocks.Commands
{
    [Command("unlock", Priority = Priority.Normal)]
    [CommandDescription("Removes the code lock from an object.")]
    [CommandActor(typeof(UnturnedUser))]
    public class CommandUnlock : UnturnedCommand
    {
        private readonly IStringLocalizer _stringLocalizer;
        private readonly CodeLockManager _codeLockManager;
        private readonly IConfiguration _configuration;
        private readonly IPermissionChecker _permissionChecker;

        public CommandUnlock(
            IStringLocalizer stringLocalizer,
            CodeLockManager codeLockManager,
            IConfiguration configuration,
            IPermissionChecker permissionChecker,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _stringLocalizer = stringLocalizer;
            _codeLockManager = codeLockManager;
            _configuration = configuration;
            _permissionChecker = permissionChecker;
        }

        protected override async UniTask OnExecuteAsync()
        {
            await UniTask.SwitchToMainThread();

            var user = (UnturnedUser)Context.Actor;
            
            var lockable = LockableInteractable.RaycastForInteractable(user.Player.Player);

            if (lockable == null)
                throw new UserFriendlyException(_stringLocalizer["commands:codelock:no_lockable_object"]);

            var codeLock = _codeLockManager.GetCodeLock(lockable.InstanceId);

            if (codeLock == null)
                throw new UserFriendlyException(_stringLocalizer["commands:codelock:no_code"]);

            var canChangeLock = false;

            if (codeLock.Users.Contains(user.SteamId.m_SteamID))
            {
                if (codeLock.Users.First() == user.SteamId.m_SteamID)
                    canChangeLock = true;
                else if (_configuration.GetValue("nonOwnerCanChangeCode", true))
                    canChangeLock = true;
            }

            if (!canChangeLock &&
                await _permissionChecker.CheckPermissionAsync(user, "bypass") == PermissionGrantResult.Grant)
                canChangeLock = true;

            if (!canChangeLock)
                throw new UserFriendlyException(_stringLocalizer["commands:codelock:no_access"]);

            _codeLockManager.RemoveCodeLock(lockable.InstanceId);

            await PrintAsync(_stringLocalizer["commands:codelock:code_removed"]);
        }
    }
}
