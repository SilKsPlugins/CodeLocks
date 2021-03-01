using CodeLocks.Locks;
using Microsoft.Extensions.Configuration;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using System.Linq;

namespace CodeLocks.UI
{
    public class UIManager
    {
        private readonly IConfiguration _configuration;
        private readonly Dictionary<Player, UISession> _uiSessions;

        public UIManager(IConfiguration configuration)
        {
            _configuration = configuration;
            _uiSessions = new Dictionary<Player, UISession>();
        }

        public ushort GetUIEffectId() => _configuration.GetValue<ushort>("effects:ui", 29123);

        public short GetUIEffectKey() => (short)GetUIEffectId();

        public ushort GetSuccessEffectId() => _configuration.GetValue<ushort>("effects:success", 29124);

        public ushort GetFailureEffectId() => _configuration.GetValue<ushort>("effects:failure", 29125);

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

            var session = new UISession(this, player, codeLock, callback,
                x => _uiSessions.Remove(x.Player));

            _uiSessions.Add(player, session);

            session.StartSession();
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
