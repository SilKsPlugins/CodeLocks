using CodeLocks.Locks;
using SDG.Unturned;
using Steamworks;
using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using SDG.NetTransport;

namespace CodeLocks.UI
{
    public class UISession
    {
        private readonly UIManager _uiManager;
        private string _code;
        private readonly SessionEnded _sessionEndedCallback;

        public Player Player { get; }

        public ITransportConnection TransportConnection => Player.channel.owner.transportConnection;

        public CodeLockInfo CodeLock { get; }

        public UIManager.CodeEnteredCallback CodeEnteredCallback { get; }

        public delegate void SessionEnded(UISession session);

        public UISession(UIManager uiManager, Player player, CodeLockInfo codeLock, UIManager.CodeEnteredCallback codeEnteredCallback, SessionEnded sessionEndedCallback)
        {
            _uiManager = uiManager;
            _code = "";
            _sessionEndedCallback = sessionEndedCallback;
            Player = player;
            CodeLock = codeLock;
            CodeEnteredCallback = codeEnteredCallback;
        }

        public void StartSession()
        {
            EffectManager.sendUIEffect(_uiManager.GetUIEffectId(), _uiManager.GetUIEffectKey(), TransportConnection, true);

            Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, true);
            Player.setPluginWidgetFlag(EPluginWidgetFlags.ForceBlur, true);
        }

        public void EndSession()
        {
            Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, false);
            Player.setPluginWidgetFlag(EPluginWidgetFlags.ForceBlur, false);

            EffectManager.askEffectClearByID(_uiManager.GetUIEffectId(), TransportConnection);

            _sessionEndedCallback(this);
        }

        private void SetInputText(int index, string text)
        {
            EffectManager.sendUIEffectText(_uiManager.GetUIEffectKey(), TransportConnection, true, "Input" + (index + 1), text);
        }

        public void PressedButton(string buttonName)
        {
            if (_code.Length == 4) return;

            switch (buttonName)
            {
                case "Keypad0":
                case "Keypad1":
                case "Keypad2":
                case "Keypad3":
                case "Keypad4":
                case "Keypad5":
                case "Keypad6":
                case "Keypad7":
                case "Keypad8":
                case "Keypad9":
                    var next = buttonName.Last();
                    
                    _code += next;

                    var parsed = GetEnteredCode();

                    if (_code.Length == 4 && parsed.HasValue)
                    {
                        async UniTask DelayedEnd()
                        {
                            await UniTask.SwitchToMainThread();

                            SetInputText(3, "*");

                            void SetAllInputTexts(string text)
                            {
                                for (int i = 0; i < 4; i++)
                                    SetInputText(i, text);
                            }

                            await UniTask.Delay(100);

                            var code = GetEnteredCode()!.Value;

                            var success = code == CodeLock.Code;

                            var text = success
                                ? "<color=green>*</color>"
                                : "<color=red>*</color>";

                            var soundEffectId = success ? _uiManager.GetSuccessEffectId() : _uiManager.GetFailureEffectId();

                            SetAllInputTexts(text);

                            await UniTask.Delay(150);

                            SetAllInputTexts("*");

                            await UniTask.Delay(150);

                            SetAllInputTexts(text);

                            EffectManager.sendUIEffect(soundEffectId, (short)soundEffectId, TransportConnection, true);

                            await UniTask.Delay(400);

                            EffectManager.askEffectClearByID(soundEffectId, TransportConnection);

                            CodeEnteredCallback(Player, CodeLock, parsed.Value);

                            EndSession();
                        }

                        DelayedEnd().Forget();
                    }
                    else
                    {
                        SetInputText(_code.Length - 1, "*");
                        SetInputText(_code.Length, "_");
                    }
                    
                    break;
                case "KeypadC":
                    _code = "";
                    SetInputText(0, "_");
                    SetInputText(1, "");
                    SetInputText(2, "");
                    SetInputText(3, "");
                    break;
                case "KeypadExit":
                    EndSession();
                    break;
            }
        }

        public ushort? GetEnteredCode()
        {
            if (_code.Length != 4)
                return null;

            return CodeLockInfo.ParseCode(_code);
        }
    }
}
