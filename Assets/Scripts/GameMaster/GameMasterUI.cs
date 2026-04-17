using System;
using Printer;
using UI;
using UI.UIDocs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Utility.GameMaster
{
    public class GameMasterUI : MonoBehaviour
    {
        public bool Active { get; set; } = false;
        private VisualElement _root;
        private ConsoleView _consoleView;
        private ConsoleView _stateView;
        private TextField _input;
        private GameMaster _gm;

        private void Awake()
        {
            _gm = GetComponent<GameMaster>();
            _root = GetComponent<UIDocument>().rootVisualElement;
            _consoleView = _root.Q<ConsoleView>("console");
            _stateView = _root.Q<ConsoleView>("state");
            _input = _root.Q<TextField>("input");
            
            _root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
                {
                    var text = _input.value;
                    _gm.Evaluate(text);
                    _input.value = string.Empty;
                    evt.StopImmediatePropagation();
                    _input.Focus();
                }
            }, TrickleDown.TrickleDown);

            _input.RegisterCallback<BlurEvent>(evt =>
            {
                _input.Focus();
                evt.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);

            _root.RegisterCallback<PointerDownEvent>(evt =>
            {
                var ve = evt.target as VisualElement;
                if (ve != null && ve != _input && !IsChildOf(ve, _input))
                {
                    evt.StopImmediatePropagation();
                    _input.Focus();
                }
            }, TrickleDown.TrickleDown);

            _input.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Tab)
                {
                    evt.StopImmediatePropagation();
                }
            }, TrickleDown.TrickleDown);

            Active = false;
            SetDisplayActive(Active);
        }

        private void Update()
        {
            if (IsGmTogglePressed())
            {
                Active = !Active;
                SetDisplayActive(Active);
                Time.timeScale = Active ? 0f : 1f;
                PlayerManager.SetPaused(Active);
            }

            if (Active)
                UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            _consoleView.text = _gm.GetConsoleOutput();
            _stateView.text = _gm.GetGameStateStr();
        }
        
        private static bool IsChildOf(VisualElement child, VisualElement parent)
        {
            var cur = child;
            while (cur != null)
            {
                if (cur == parent) return true;
                cur = cur.parent;
            }

            return false;
        }
        
        private void SetDisplayActive(bool active)
        {
            _root.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            if (active)
            {
                _input.Focus();
            }
        }
        
        private static bool IsGmTogglePressed()
        {
            var kb = Keyboard.current;
            if (kb == null) return false;

            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            bool mPressedThisFrame = kb.mKey.wasPressedThisFrame;

            return ctrl && shift && mPressedThisFrame;
        }
    }
}