using UnityEngine;
using UnityEngine.EventSystems;
using Utility;

namespace Desktop.WindowSystem
{
	public class WindowDragHandle : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
	{
		[SerializeField] private Window window;
		[SerializeField] private float doubleClickThreshold = 0.3f;
        
		private Vector2 _dragOffset;
		private bool _dragging = false;
		private float _lastClickTime = -1f;

		public void OnPointerDown(PointerEventData eventData)
		{
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

			if (window.Maximized)
			{
				var corners = new Vector3[4];
				window.RectTransform.GetWorldCorners(corners);
				var maxLeft = corners[0].x;
				var maxTop = corners[1].y;
				var maxWidth = corners[2].x - corners[0].x;

				var normalizedX = (mousePos.x - maxLeft) / maxWidth;
				var offsetFromTop = maxTop - mousePos.y;

				window.SetFloating();

				window.RectTransform.GetWorldCorners(corners);
				var floatWidth = corners[2].x - corners[0].x;
				var floatHeight = corners[1].y - corners[0].y;

				// Target top-left: X starts from mouse position scaled by float width, Y from offsetFromTop
				var targetTopLeft = new Vector3(
					mousePos.x - normalizedX * floatWidth,
					mousePos.y + offsetFromTop,
					corners[0].z
				);

				window.RectTransform.position = targetTopLeft + new Vector3(
					window.RectTransform.pivot.x * floatWidth,
					-window.RectTransform.pivot.y * floatHeight,
					0f
				);

				RectTransformUtility.ScreenPointToLocalPointInRectangle(
					window.RectTransform.parent as RectTransform,
					mousePos,
					eventData.pressEventCamera,
					out var mouseLocal
				);
				_dragOffset = window.RectTransform.anchoredPosition - mouseLocal;
				return;
			}    
			
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				window.RectTransform.parent as RectTransform,
				mousePos,
				eventData.pressEventCamera,
				out var lp
			);
			window.RectTransform.anchoredPosition = lp + _dragOffset;
		}
		
		public void OnPointerUp(PointerEventData eventData)
		{
			_dragging = false;
		}
	}
}