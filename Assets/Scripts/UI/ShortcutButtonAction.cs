using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UI
{
	/// <summary>
	/// Emulates a desktop shortcut: fires <see cref="onAction"/> on double-click or
	/// Enter/Submit, and does nothing on a single mouse click.
	/// Wire all actions to <see cref="onAction"/> on this component instead of
	/// Button.onClick — leave Button.onClick empty.
	/// </summary>
	public class ShortcutButtonAction : MonoBehaviour, IPointerClickHandler, ISubmitHandler
	{
		[SerializeField] private UnityEvent onAction;
		[SerializeField] private float doubleClickThreshold = 0.3f;

		private float _lastClickTime = -1f;

		public void OnPointerClick(PointerEventData eventData)
		{
			float timeSinceLastClick = Time.unscaledTime - _lastClickTime;
			if (timeSinceLastClick <= doubleClickThreshold)
			{
				onAction.Invoke();
				_lastClickTime = -1f;
				EventSystem.current.SetSelectedGameObject(null);
				return;
			}

			_lastClickTime = Time.unscaledTime;
		}

		public void OnSubmit(BaseEventData eventData)
		{
			onAction.Invoke();
			EventSystem.current.SetSelectedGameObject(null);
		}
	}
}
