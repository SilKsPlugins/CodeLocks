using CodeLocks.Locks;
using SDG.NetTransport;
using SDG.Unturned;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace CodeLocks.UI
{
    public class UISession : MonoBehaviour
    {
        private UIManager _uiManager = null!;
        private string _code = "";
        private SessionEnded? _sessionEndedCallback;

        public Player Player { get; private set; } = null!;

        public ITransportConnection TransportConnection => Player.channel.owner.transportConnection;

        public CodeLockInfo CodeLock { get; private set; } = null!;

        public UIManager.CodeEnteredCallback CodeEnteredCallback { get; private set; } = null!;

        public delegate void SessionEnded(UISession session);
        
        public void StartSession(UIManager uiManager, Player player, CodeLockInfo codeLock, UIManager.CodeEnteredCallback codeEnteredCallback, SessionEnded sessionEndedCallback)
        {
            _uiManager = uiManager;
            _sessionEndedCallback = sessionEndedCallback;
            Player = player;
            CodeLock = codeLock;
            CodeEnteredCallback = codeEnteredCallback;

            EffectManager.sendUIEffect(_uiManager.GetUIEffectId(), _uiManager.GetUIEffectKey(), TransportConnection, true);

            Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, true);
            Player.setPluginWidgetFlag(EPluginWidgetFlags.ForceBlur, true);
        }

        private void OnDestroy()
        {
            Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, false);
            Player.setPluginWidgetFlag(EPluginWidgetFlags.ForceBlur, false);

            EffectManager.askEffectClearByID(_uiManager.GetUIEffectId(), TransportConnection);

            _sessionEndedCallback?.Invoke(this);
        }

        public void EndSession()
        {
            DestroyImmediate(this);
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
                        IEnumerator DelayedEnd()
                        {
                            SetInputText(3, "*");

                            void SetAllInputTexts(string text)
                            {
                                for (int i = 0; i < 4; i++)
                                    SetInputText(i, text);
                            }


                            yield return new WaitForSeconds(0.1f);

                            var code = GetEnteredCode()!.Value;

                            var success = code == CodeLock.Code;

                            var text = success
                                ? "<color=green>*</color>"
                                : "<color=red>*</color>";

                            var soundEffectId = success ? _uiManager.GetSuccessEffectId() : _uiManager.GetFailureEffectId();

                            SetAllInputTexts(text);

                            yield return new WaitForSeconds(0.15f);

                            SetAllInputTexts("*");

                            yield return new WaitForSeconds(0.15f);

                            SetAllInputTexts(text);

                            EffectManager.sendUIEffect(soundEffectId, (short)soundEffectId, TransportConnection, true);

                            yield return new WaitForSeconds(0.4f);

                            EffectManager.askEffectClearByID(soundEffectId, TransportConnection);

                            CodeEnteredCallback(Player, CodeLock, parsed.Value);

                            EndSession();
                        }

                        StartCoroutine(DelayedEnd());
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
