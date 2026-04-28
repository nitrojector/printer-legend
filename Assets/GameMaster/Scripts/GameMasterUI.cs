using GameMaster.Scripts.UI;
using UnityEngine;
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
			
			Active = false;
			SetDisplayActive(false);
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

			input.RegisterCallback<KeyDownEvent>(evt =>
			{
				if (evt.keyCode == KeyCode.Tab)
				{
					evt.StopImmediatePropagation();
				}
			}, TrickleDown.TrickleDown);
			
			Ready = true;
		}

		public void SetActive(bool active)
		{
			Active = active;
			SetDisplayActive(Active);
			Time.timeScale = Active ? 0f : 1f;
			GameMgr.Instance.SetPaused(Active);
		}

		public void ToggleActive()
		{
			Active = !Active;
			SetDisplayActive(Active);
			Time.timeScale = Active ? 0f : 1f;
			GameMgr.Instance.SetPaused(Active);
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
	}
}