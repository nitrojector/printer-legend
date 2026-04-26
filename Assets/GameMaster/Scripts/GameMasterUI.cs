using System;
using GameMaster.Scripts.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GameMaster.Scripts
{
	public class GameMasterUI : MonoBehaviour
	{
		public bool Active { get; set; } = false;
        
		public ConsoleView ConsoleView { get; private set; }
		public ConsoleView StateView { get; private set; }
        
		private VisualElement root;
		private TextField input;
		
		private GameMaster gm;
		private UIDocument doc;

		/// <summary>
		/// If visual tree and elements are ready
		/// </summary>
		public bool Ready { get; private set; } = false;

		private void Awake()
		{
			gm = GetComponent<GameMaster>();
			doc = GetComponent<UIDocument>();
			
			BuildTree();
		}

		private void BuildTree()
		{
			root = doc.rootVisualElement;
			ConsoleView = root.Q<ConsoleView>("console");
			StateView = root.Q<ConsoleView>("state");
			input = root.Q<TextField>("input");
            
			root.RegisterCallback<KeyDownEvent>(evt =>
			{
				if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
				{
					var text = input.value;
					gm.Evaluate(text);
					input.value = string.Empty;
					evt.StopImmediatePropagation();
					input.Focus();
				}
			}, TrickleDown.TrickleDown);

			input.RegisterCallback<BlurEvent>(evt =>
			{
				input.Focus();
				evt.StopImmediatePropagation();
			}, TrickleDown.TrickleDown);

			root.RegisterCallback<PointerDownEvent>(evt =>
			{
				var ve = evt.target as VisualElement;
				if (ve != null && ve != input && !IsChildOf(ve, input))
				{
					evt.StopImmediatePropagation();
					input.Focus();
				}
			}, TrickleDown.TrickleDown);

			input.RegisterCallback<KeyDownEvent>(evt =>
			{
				if (evt.keyCode == KeyCode.Tab)
				{
					evt.StopImmediatePropagation();
				}
			}, TrickleDown.TrickleDown);

			Ready = true;
			
			Active = false;
			SetDisplayActive(Active);
		}

		public void SetActive(bool active)
		{
			Active = active;
			SetDisplayActive(Active);
			Time.timeScale = Active ? 0f : 1f;
			GameManager.Instance.SetPaused(Active);
		}

		public void ToggleActive()
		{
			Active = !Active;
			SetDisplayActive(Active);
			Time.timeScale = Active ? 0f : 1f;
			GameManager.Instance.SetPaused(Active);
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
			root.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
			if (active)
			{
				input.Focus();
			}
		}
        
		[Obsolete]
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