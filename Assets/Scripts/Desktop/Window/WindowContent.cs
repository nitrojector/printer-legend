using UnityEngine;
using UnityEngine.UI;
using Utility;

namespace Desktop.Window
{
	[ExecuteAlways]
	public abstract class WindowContent : MonoBehaviour, ILayoutController
	{
		
		private DrivenRectTransformTracker tracker;
		private RectTransform rt;

		protected RectTransform RectTransform => rt ??= GetComponent<RectTransform>();

		private void OnEnable()
		{
			tracker.Add(this, RectTransform,
				DrivenTransformProperties.Anchors |
				DrivenTransformProperties.AnchoredPosition |
				DrivenTransformProperties.SizeDelta |
				DrivenTransformProperties.Pivot);
			SetLayoutHorizontal();
			SetLayoutVertical();
		}

		private void OnDisable()
		{
			tracker.Clear();
		}
		
#if UNITY_EDITOR
		protected void OnValidate()
		{
			if (RectTransform == null) return;
			SetLayoutHorizontal();
			SetLayoutVertical();
			Logr.Info($"Validated layout for {name}");
		}
#endif

		public void SetLayoutHorizontal()
		{
			RectTransform.anchorMin = new Vector2(0f, RectTransform.anchorMin.y);
			RectTransform.anchorMax = new Vector2(1f, RectTransform.anchorMax.y);
			RectTransform.offsetMin = new Vector2(0f, RectTransform.offsetMin.y);
			RectTransform.offsetMax = new Vector2(0f, RectTransform.offsetMax.y);
		}

		public void SetLayoutVertical()
		{
			RectTransform.anchorMin = new Vector2(RectTransform.anchorMin.x, 0f);
			RectTransform.anchorMax = new Vector2(RectTransform.anchorMax.x, 1f);
			RectTransform.offsetMin = new Vector2(RectTransform.offsetMin.x, 0f);
			RectTransform.offsetMax = new Vector2(RectTransform.offsetMax.x, 0f);
		}

		/// <summary>
		/// Called when the window is initialized with this content.
		/// </summary>
		public virtual void OnInitialize() {}
		
		/// <summary>
		/// Called when the window GameObject is shown (set active) via <see cref="Window.Show"/>.
		/// </summary>
		public virtual void OnShow() {}
		
		/// <summary>
		/// Called when the window GameObject is hidden (set inactive) via <see cref="Window.Minimize"/>.
		/// </summary>
		public virtual void OnMinimize() {}

		/// <summary>
		/// Called when the window content area is resized.
		/// This can happen when the window is resized, maximized, or when the screen size changes.
		/// </summary>
		public virtual void OnResize() {}
	}
}