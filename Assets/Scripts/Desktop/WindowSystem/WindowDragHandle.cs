using UnityEngine;
using UnityEngine.EventSystems;
using Utility;

namespace Desktop.WindowSystem
{
	public class WindowDragHandle : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerMoveHandler
	{
		[SerializeField] private Window window;
		[SerializeField] private float doubleClickThreshold = 0.3f;
        
		private Vector2 _dragOffset;
		private bool _dragging = false;
		private float _lastClickTime = -1f;

		public void OnPointerDown(PointerEventData eventData)
		{
			WindowManager.Instance.BringToFront(window);
			
			float timeSinceLastClick = Time.unscaledTime - _lastClickTime;
			if (timeSinceLastClick <= doubleClickThreshold)
			{
				window.ToggleMaximize();
				_lastClickTime = -1f;
				return;
			}
            
			_lastClickTime = Time.unscaledTime;
			_dragging = true;
            
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				window.RectTransform.parent as RectTransform,
				eventData.position,
				eventData.pressEventCamera,
				out var localPoint
			);
			_dragOffset = window.RectTransform.anchoredPosition - localPoint;
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (!_dragging) return;

			var mousePos = eventData.position;
			if (mousePos.x < 0 || mousePos.x > Screen.width ||
			    mousePos.y < 0 || mousePos.y > Screen.height)
			{
				_dragging = false;
				return;
			}

			var parentRt = window.RectTransform.parent as RectTransform;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				parentRt, mousePos, eventData.pressEventCamera, out var mouseLocal);

			if (window.Maximized)
			{
				var parentRect = parentRt.rect;
				float normalizedX = Mathf.InverseLerp(parentRect.xMin, parentRect.xMax, mouseLocal.x);
				float offsetFromTop = parentRect.yMax - mouseLocal.y;

				window.SetFloating();

				// offsetMin/offsetMax are now restored; rect.size is valid immediately
				var floatSize = window.RectTransform.rect.size;

				// Place the window so the mouse sits at the same relative position it had
				// within the maximized window (normalizedX horizontally, offsetFromTop from the top).
				window.SetPosition(mouseLocal, new Vector2(normalizedX, 1f - offsetFromTop / floatSize.y));

				_dragOffset = window.RectTransform.anchoredPosition - mouseLocal;
				return;
			}

			window.RectTransform.anchoredPosition = mouseLocal + _dragOffset;
		}
		
		public void OnPointerUp(PointerEventData eventData)
		{
			_dragging = false;
		}

		public void OnPointerMove(PointerEventData eventData)
		{
			if (!Window.IsAnyWindowResizing)
				Window.ResetCursor();
		}
	}
}