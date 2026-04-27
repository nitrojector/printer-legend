using UnityEngine;
using UnityEngine.UI;

namespace Desktop.WindowSystem
{
	[ExecuteAlways]
	public abstract class WindowContent : MonoBehaviour, ILayoutController
	{
		public Vector2 MinContentSize => EnforceMinSize ? minContentSize : Vector2.zero;
		public Vector2 MaxContentSize => EnforceMaxSize ? maxContentSize : new Vector2(float.PositiveInfinity, float.PositiveInfinity);

		public virtual string WindowTitle { get; protected set; } = "Window";

		[Header("Window Controls")]
		[SerializeField] private bool allowMaximize = true;
		public virtual bool AllowMaximize => allowMaximize;

		[SerializeField] private bool allowMinimize = true;
		public virtual bool AllowMinimize => allowMinimize;

		[Header("Window Size Constraints")]
		[field:SerializeField] public bool EnforceMinSize { get; private set; } = true;
		[SerializeField] private Vector2 minContentSize = new Vector2(100, 100);

		[SerializeField] private bool enforceMaxSize = false;
		public bool EnforceMaxSize
		{
			get => enforceMaxSize;
			set
			{
				enforceMaxSize = value;
				AttachedWindow?.NotifyConfigChanged();
			}
		}
		[SerializeField] private Vector2 maxContentSize = new Vector2(1000, 1000);

		protected Window AttachedWindow;
		
		/// <summary>
		/// Called when the window is initialized with this content.
		/// </summary>
		public virtual void OnInitialize() {}
		
		/// <summary>
		/// Called when the window GameObject is shown (set active) via <see cref="Window.Show"/>.
		/// </summary>
		public virtual void OnShow() {}
		
		/// <summary>
		/// Called when the window GameObject is hidden (set inactive) or destroyed via <see cref="Window.Minimize"/>.
		/// </summary>
		public virtual void OnMinimize() {}

		/// <summary>
		/// Called when the window content area is resized.
		/// This can happen when the window is resized, maximized, or when the screen size changes.
		/// </summary>
		public virtual void OnResize() {}

		/// <summary>
		/// Called when window is trying to be destroyed via <see cref="Window.Quit"/> or window close button.
		/// Return true to allow the window to be closed, or false to cancel the close action.
		/// </summary>
		public virtual bool OnQuit()
		{
			return true;
		}

		/// <summary>
		/// Convenience method to set the window title from this content and notify the parent window of the change.
		/// </summary>
		/// <param name="title">title to set</param>
		protected void SetTitle(string title)
		{
			WindowTitle = title;
			AttachedWindow?.NotifyWindowInformationChanged();
		}
		
		/// <summary>
		/// Convenience method to close the parent window from this content.
		/// </summary>
		protected void CloseWindow() { AttachedWindow?.Quit(); }

		private DrivenRectTransformTracker _tracker;
		private RectTransform _rt;
		
		public RectTransform RectTransform => _rt ??= GetComponent<RectTransform>();
		
		internal void SetAttachedWindow(Window window)
		{
			AttachedWindow = window;
			if (window != null) window.Title = WindowTitle;
		}

		private void OnEnable()
		{
			_tracker.Add(this, RectTransform,
				DrivenTransformProperties.Anchors |
				DrivenTransformProperties.AnchoredPosition |
				DrivenTransformProperties.SizeDelta |
				DrivenTransformProperties.Pivot);
			SetLayoutHorizontal();
			SetLayoutVertical();
		}

		private void OnDisable()
		{
			_tracker.Clear();
		}
		
#if UNITY_EDITOR
		protected void OnValidate()
		{
			// Sync backing fields to the effective virtual value so the Inspector is never misleading.
			// If a subclass overrides AllowMaximize/AllowMinimize the field reverts to the override value.
			allowMaximize = AllowMaximize;
			allowMinimize = AllowMinimize;

			if (RectTransform == null) return;
			SetLayoutHorizontal();
			SetLayoutVertical();
			// Push control/constraint changes to the parent window in the editor.
			GetComponentInParent<Window>()?.SyncFromContent();
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

	}
}