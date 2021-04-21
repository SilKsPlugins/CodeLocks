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
    [Command("lock", Priority = Priority.Normal)]
    [CommandDescription("Adds a code lock to an object, or changes if one already exists.")]
    [CommandSyntax("<code>")]
    [CommandActor(typeof(UnturnedUser))]
    public class CommandLock : UnturnedCommand
    {
        private readonly IConfiguration _configuration;
        private readonly IStringLocalizer _stringLocalizer;
        private readonly CodeLockManager _codeLockManager;
        private readonly IPermissionChecker _permissionChecker;

        public CommandLock(
            IConfiguration configuration,
            IStringLocalizer stringLocalizer,
            CodeLockManager codeLockManager,
            IPermissionChecker permissionChecker,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _configuration = configuration;
            _stringLocalizer = stringLocalizer;
            _codeLockManager = codeLockManager;
            _permissionChecker = permissionChecker;
        }

        protected override async UniTask OnExecuteAsync()
        {
            await UniTask.SwitchToMainThread();

            var user = (UnturnedUser)Context.Actor;

            var codeStr = (await Context.Parameters.GetAsync<string>(0)).ToLower();
            if (codeStr.Length != 4 || codeStr.Any(chr => !char.IsDigit(chr)))
                throw new UserFriendlyException(_stringLocalizer["commands:codelock:invalid_code", new {Code = codeStr}]);

            var code = CodeLockInfo.ParseCode(codeStr);

            var lockable = LockableInteractable.RaycastForInteractable(user.Player.Player);
            if (lockable == null)
                throw new UserFriendlyException(_stringLocalizer["commands:codelock:no_lockable_object"]);

            var codeLock = _codeLockManager.GetCodeLock(lockable.InstanceId);
            if (codeLock == null)
            {
                // Add code lock
                _codeLockManager.AddCodeLock(lockable.InstanceId, code, user.SteamId.m_SteamID);

                await PrintAsync(_stringLocalizer["commands:codelock:code_added", new { Code = codeStr }]);

                return;
            }

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

            if (canChangeLock)
            {
                _codeLockManager.RemoveCodeLock(lockable.InstanceId);

                _codeLockManager.AddCodeLock(lockable.InstanceId, code, user.SteamId.m_SteamID);

                await PrintAsync(_stringLocalizer["commands:codelock:code_changed", new { Code = codeStr }]);
            }
            else
            {
                throw new UserFriendlyException(_stringLocalizer["commands:codelock:no_access"]);
            }
        }
    }
}
