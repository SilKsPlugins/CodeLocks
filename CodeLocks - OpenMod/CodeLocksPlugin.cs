using CodeLocks.Locks;
using CodeLocks.UI;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using OpenMod.Core.Helpers;
using OpenMod.Unturned.Plugins;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ObjectManager = CodeLocks.Locks.ObjectManager;

[assembly: PluginMetadata("CodeLocks", DisplayName = "CodeLocks", Author = "SilK")]
namespace CodeLocks
{
    public class CodeLocksPlugin : OpenModUnturnedPlugin
    {
        private readonly IConfiguration _configuration;
        private readonly IStringLocalizer _stringLocalizer;
        private readonly ILogger<CodeLocksPlugin> _logger;
        private readonly CodeLockManager _codeLockManager;
        private readonly UIManager _uiManager;
        private readonly ObjectManager _objectManager;

        private readonly List<Attempt> _attempts;

        public CodeLocksPlugin(
            IConfiguration configuration,
            IStringLocalizer stringLocalizer,
            ILogger<CodeLocksPlugin> logger,
            CodeLockManager codeLockManager,
            UIManager uiManager,
            ObjectManager objectManager,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _configuration = configuration;
            _stringLocalizer = stringLocalizer;
            _logger = logger;
            _codeLockManager = codeLockManager;
            _uiManager = uiManager;
            _objectManager = objectManager;

            _attempts = new List<Attempt>();
        }

        protected override async UniTask OnLoadAsync()
        {
            await _codeLockManager.LoadAsync(WorkingDirectory);

            await UniTask.SwitchToMainThread();

            _uiManager.Load();
            _objectManager.Load();

            UnturnedPatches.OnBarricadesSave += OnBarricadesSave;
            UnturnedPatches.OnCheckingDoorAccess += OnCheckingAccess;
            UnturnedPatches.OnCheckingStorageAccess += OnCheckingAccess;

            Level.onPostLevelLoaded += OnPostLevelLoaded;
            if (Level.isLoaded) OnPostLevelLoaded(0);
        }

        protected override async UniTask OnUnloadAsync()
        {
            await UniTask.SwitchToMainThread();

            // ReSharper disable DelegateSubtraction
            UnturnedPatches.OnBarricadesSave -= OnBarricadesSave;
            UnturnedPatches.OnCheckingDoorAccess -= OnCheckingAccess;
            UnturnedPatches.OnCheckingStorageAccess -= OnCheckingAccess;
            Level.onPostLevelLoaded -= OnPostLevelLoaded;
            // ReSharper restore DelegateSubtraction

            _uiManager.Unload();
            _objectManager.Unload();

            await UniTask.SwitchToThreadPool();

            await _codeLockManager.SaveAsync();
        }

        private void OnBarricadesSave()
        {
            AsyncHelper.RunSync(() => _codeLockManager.SaveAsync());
        }

        private void OnEnteredCode(Player player, CodeLockInfo codeLock, ushort enteredCode)
        {
            var steamId = player.channel.owner.playerID.steamID;

            if (codeLock.Code == enteredCode)
            {
                codeLock.Users.Add(steamId.m_SteamID);

                _attempts.RemoveAll(a => a.SteamId == steamId && a.InstanceId == codeLock.InstanceId);

                _objectManager.UpdateBarricade(codeLock, steamId);

                if (_objectManager.TryGetBarricade(codeLock.InstanceId,
                    out var x, out var y, 
                    out var plant, out var index, out _,
                    out var drop) && drop.interactable is InteractableStorage)
                {
                    BarricadeManager.instance.askStoreStorage(steamId, x, y, plant, index, false);
                }
            }
            else
            {
                var attemptsConfig = _configuration.GetValue("attempts", new AttemptsConfig());

                _attempts.RemoveAll(a =>
                    a.SteamId == steamId
                    && a.InstanceId == codeLock.InstanceId
                    && Time.time - attemptsConfig.Cooldown > a.Time);

                _attempts.Add(new Attempt(steamId, codeLock.InstanceId));

                var count = _attempts.Count(a =>
                    a.SteamId == steamId
                    && a.InstanceId == codeLock.InstanceId);

                var damages = (attemptsConfig.Damages != null && attemptsConfig.Damages.Length > 0)
                    ? attemptsConfig.Damages : new byte[] {0};

                var damage = count > damages.Length ? damages[0] : attemptsConfig.Damages![count - 1];

                player.life.askDamage(damage, Vector3.up, EDeathCause.INFECTION, ELimb.SPINE, Provider.server, out _);
            }
        }

        private void OnCheckingAccess(CSteamID steamId, Interactable storage, ref bool intercept, ref bool shouldAllow)
        {
            BarricadeManager.tryGetInfo(storage.transform, 
                out _, out _, out _, out _, out _,
                out var drop);
            
            var codeLock = _codeLockManager.GetCodeLock(drop.instanceID);
            if (codeLock == null) return;

            intercept = true;

            if (!codeLock.Users.Contains(steamId.m_SteamID))
            {
                var player = PlayerTool.getPlayer(steamId);
                if (player == null) return;

                _uiManager.ShowKeypad(player, codeLock, OnEnteredCode);

                shouldAllow = false;

                return;
            }

            intercept = true;
            shouldAllow = true;
        }

        private void OnPostLevelLoaded(int level)
        {
            var codeLocks = _codeLockManager.GetAllCodeLocks().ToList();

            foreach (var codeLock in codeLocks)
            {
                if (_objectManager.UpdateBarricade(codeLock)) continue;

                _codeLockManager.RemoveCodeLock(codeLock.InstanceId);
            }
        }
    }
}
