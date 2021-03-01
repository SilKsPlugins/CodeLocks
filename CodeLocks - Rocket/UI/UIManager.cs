using CodeLocks.Configuration;
using CodeLocks.Locks;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using System.Linq;

namespace CodeLocks.UI
{
    public class UIManager
    {
        private readonly CodeLocksConfiguration _configuration;
        private readonly Dictionary<Player, UISession> _uiSessions;

        public UIManager(CodeLocksConfiguration configuration)
        {
            _configuration = configuration;
            _uiSessions = new Dictionary<Player, UISession>();
        }

        private EffectsConfig GetEffectsConfig() => _configuration.Effects ?? new();

        public ushort GetUIEffectId() => GetEffectsConfig().UI;

        public short GetUIEffectKey() => (short)GetUIEffectId();

        public ushort GetSuccessEffectId() => GetEffectsConfig().Success;

        public ushort GetFailureEffectId() => GetEffectsConfig().Failure;

        public void Load()
        {
            EffectManager.onEffectButtonClicked += OnEffectButtonClicked;
            Provider.onEnemyDisconnected += OnDisconnected;
            PlayerLife.onPlayerDied += OnPlayerDied;
        }

        public void Unload()
        {
            // ReSharper disable DelegateSubtraction
            EffectManager.onEffectButtonClicked -= OnEffectButtonClicked;
            Provider.onEnemyDisconnected -= OnDisconnected;
            PlayerLife.onPlayerDied -= OnPlayerDied;
            // ReSharper restore DelegateSubtraction

            for (var i = _uiSessions.Count - 1; i >= 0; i--)
            {
                _uiSessions.ElementAt(i).Value.EndSession();
            }
        }

        public delegate void CodeEnteredCallback(Player player, CodeLockInfo codeLock, ushort enteredCode);

        public void ShowKeypad(Player player, CodeLockInfo codeLock, CodeEnteredCallback callback)
        {
            CloseKeypad(player);

            var session = player.gameObject.AddComponent<UISession>();

            session.StartSession(this, player, codeLock, callback, x => _uiSessions.Remove(x.Player));

            _uiSessions.Add(player, session);
        }

        public void CloseKeypad(Player player)
        {
            if (_uiSessions.TryGetValue(player, out var session))
            {
                session.EndSession();
            }
        }

        private void OnEffectButtonClicked(Player player, string buttonName)
        {
            if (_uiSessions.TryGetValue(player, out var session))
            {
                session.PressedButton(buttonName);
            }
        }

        private void OnPlayerDied(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
        {
            CloseKeypad(sender.player);
        }

        private void OnDisconnected(SteamPlayer player)
        {
            CloseKeypad(player.player);
        }
    }
}
