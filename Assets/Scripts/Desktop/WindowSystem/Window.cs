using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utility;

namespace Desktop.WindowSystem
{
	public class Window : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
	{
		/// <summary>
		/// Internal handle for Window for use by <see cref="WindowManager"/>
		/// </summary>
		public sealed class InternalWindowHandle
		{
			public readonly Window target;

			internal InternalWindowHandle(Window win) => target = win;

			public void SetWindowId(int id) => target.WindowId = id;
		}
		
		/// <summary>
		/// Gets or sets the title of the window. This is displayed in the title bar of the window.
		/// </summary>
		public string Title { 
			get => titleText.text; 
			set => titleText.text = value; 
		}
		
		public int WindowId { get; private set; }
		
		/// <summary>
		/// Gets the content the window's container currently owns.
		/// </summary>
		public WindowContent Content => content;
		
		/// <summary>
		/// Gets the RectTransform of the window itself.
		/// </summary>
		public RectTransform RectTransform { get; private set; }

		/// <summary>
		/// Gets the RectTransform of the content container.
		/// This is the RectTransform of the GameObject where content GameObjects are attached.
		/// </summary>
		public RectTransform ContentContainerRectTransform => contentContainerRect;
		
		/// <summary>
		/// Gets the Rect of the content area.
		/// This is the area where content GameObjects are attached and should be sized to fit.
		/// </summary>
		public Rect ContentRect => contentContainerRect.rect;

		/// <summary>
		/// Gets whether the window is currently maximized. A maximized window fills its parent container and has no offset.
		/// </summary>
		public bool Maximized => maximized;

		/// <summary>
		/// Gets whether the window is currently minimized (hidden).
		/// A window can be maximized and minimized at the same time, in which case it is considered minimized.
		/// </summary>
		public bool Minimized => !shown;
		
		/// <summary>
		/// Gets or sets whether maximize is enabled.
		/// If disabled, the maximize button is hidden and the window cannot be maximized.
		/// </summary>
		public bool MaximizeEnabled {
			get => maximizeEnabled && (content == null || (content.AllowMaximize && !content.EnforceMaxSize));
			set
			{
				maximizeEnabled = value;
				RefreshMaximizeButton();
			}
		}

		/// <summary>
		/// Gets or sets whether minimize is enabled.
		/// If disabled, the minimize button is hidden and the window cannot be minimized.
		/// </summary>
		public bool MinimizeEnabled
		{
			get => minimizeEnabled && (content == null || content.AllowMinimize);
			set
			{
				minimizeEnabled = value;
				RefreshMinimizeButton();
			}
		}
		
		// Editor References
		[SerializeField] private TMP_Text titleText;
		[SerializeField] private GameObject contentContainer;
		[SerializeField] private Button closeButton;
		[SerializeField] private Button maximizeButton;
		[SerializeField] private Button minimizeButton;
		
		// Config
		[SerializeField] private bool minimizeEnabled = true;
		[SerializeField] private bool maximizeEnabled = true;
		[SerializeField] private bool startShown = false;
		[SerializeField] private float resizeEdgeSize = 8f;

		// State
		private bool shown = false;
		private bool maximized = false;
		private FloatingWindowData floatingData = default;
		private bool _pendingResizeOnShow = false;

		// Resize state
		private bool _resizing = false;
		private int _resizeEdgeMask = 0;
		private Vector2 _resizeDragStart;
		private Vector2 _resizeOffsetMinStart;
		private Vector2 _resizeOffsetMaxStart;
		private Vector2 _chromeSizeAtDragStart;

		private const int ResizeLeft = 1;
		private const int ResizeRight = 2;
		private const int ResizeTop = 4;
		private const int ResizeBottom = 8;
		
		// RTs
		private RectTransform contentContainerRect;

		// Canvas (for sort order management)
		private Canvas _canvas;

		// Content
		private WindowContent content = null;

		/// <summary>
		/// Shows the window
		/// </summary>
		public void Show()
		{
			gameObject.SetActive(true);
		}
		
		/// <summary>
		/// Sets the window to its floating state, restoring its position and size before it was maximized.
		/// </summary>
		public void SetFloating()
		{
			if (RectTransform == null) return;
			if (!maximized) return;
			
			floatingData.ApplyTo(RectTransform);
			maximized = false;
			if (content != null)
				content.OnResize();
		}
		
		/// <summary>
		/// Hides the window.
		/// Does nothing if minimizing is disabled.
		/// </summary>
		public void Minimize()
		{
			if (!MinimizeEnabled) return;
			gameObject.SetActive(false);
		}

		/// <summary>
		/// Toggles the window between floating and maximized states.
		/// Does nothing if maximizing is disabled.
		/// </summary>
		public void ToggleMaximize()
		{
			if (!MaximizeEnabled) return;
			if (RectTransform == null) return;
			
			if (!maximized)
			{
				floatingData.ReadFrom(RectTransform);
				RectTransform.anchorMin = Vector2.zero;
				RectTransform.anchorMax = Vector2.one;
				RectTransform.offsetMin = Vector2.zero;
				RectTransform.offsetMax = Vector2.zero;
				Logr.Info("Maximizing window.");
			}
			else
			{
				floatingData.ApplyTo(RectTransform);
			}
			
			if (content != null)
				content.OnResize();

			maximized = !maximized;
		}

		/// <summary>
		/// Quits the window, destroying it and any attached content.
		/// </summary>
		public void Quit()
		{
			if (!content.OnQuit()) return;
			RemoveContent();
			Destroy(gameObject);
		}

		/// <summary>
		/// Removes the currently attached content GameObject from the window. The content GameObject is destroyed.
		/// </summary>
		public void RemoveContent()
		{
			if (content != null) content.SetAttachedWindow(null);
			Destroy(content);
		}

		/// <summary>
		/// Initializes the window with the specified prefab as its content. Any existing content will be destroyed.
		/// </summary>
		/// <param name="prefab"><see cref="WindowContent"/> prefab</param>
		public void Initialize(WindowContent prefab)
		{
			if (prefab == null) return;
			if (content != null)
			{
				content.SetAttachedWindow(null);
				Destroy(content);
			}

			content = Instantiate(prefab, contentContainer.transform);
			ConfigureContent();
			content.OnInitialize();
			NotifyContentResized();
		}
		
		/// <summary>
		/// Attaches a new content GameObject to the window, replacing any existing content.
		/// The previous content is destroyed.
		/// </summary>
		/// <param name="newContent">content to attach</param>
		public void Attach(WindowContent newContent)
		{
			if (newContent == null) return;

			if (content != null)
			{
				content.SetAttachedWindow(null);
				Destroy(content);
			}

			content = newContent;
			newContent.transform.SetParent(contentContainer.transform);
			ConfigureContent();
			NotifyContentResized();
		}

		/// <summary>
		/// Attaches a new content to the window, replacing any existing content.
		/// The previous content is returned and can be reparented or destroyed by the caller.
		/// </summary>
		/// <param name="newContent">new content to attach</param>
		/// <param name="reparent">new parent for content previously attached</param>
		/// <returns>previously attached content</returns>
		public WindowContent SafeAttach(WindowContent newContent, Transform reparent = null)
		{
			if (newContent == null) return null;

			var prev = content;
			if (prev != null)
			{
				prev.SetAttachedWindow(null);
				prev.transform.SetParent(reparent);
			}

			content = newContent;
			newContent.transform.SetParent(contentContainer.transform);
			ConfigureContent();
			NotifyContentResized();
			return prev;
		}

		/// <summary>
		/// Configures <see cref="content"/>, applies its control preferences, and refreshes buttons.
		/// </summary>
		private void ConfigureContent()
		{
			RectTransform contentRt = content.RectTransform;
			if (contentRt != null)
			{
				contentRt.anchorMin = Vector2.zero;
				contentRt.anchorMax = Vector2.one;
				contentRt.offsetMin = Vector2.zero;
				contentRt.offsetMax = Vector2.zero;
			}
			else
			{
				Logr.Warn("Attached content does not have a RectTransform.");
			}

			content.SetAttachedWindow(this);

			RefreshMinimizeButton();
			RefreshMaximizeButton();
			ClampWindowToContentConstraints();
		}

		/// <summary>
		/// Notifies content of a resize. If the window is currently inactive (content Awakes may
		/// not have run yet), defers the call to the next <see cref="OnEnable"/> instead.
		/// </summary>
		private void NotifyContentResized()
		{
			if (content == null) return;
			if (gameObject.activeInHierarchy)
				content.OnResize();
			else
				_pendingResizeOnShow = true;
		}

		private void RefreshMinimizeButton() =>
			minimizeButton?.gameObject.SetActive(MinimizeEnabled);

		private void RefreshMaximizeButton() =>
			maximizeButton?.gameObject.SetActive(MaximizeEnabled);

		/// <summary>
		/// Refreshes window information such as the title from the content.
		/// Called by content when it wants to push changes to the window.
		/// </summary>
		internal void NotifyWindowInformationChanged()
		{
			if (content == null) return;
			Title = content.WindowTitle;
		}

		/// <summary>
		/// Refreshes window information such as the title and control button states from the content.
		/// </summary>
		internal void NotifyConfigChanged()
		{
			Title = content?.WindowTitle;
			RefreshMinimizeButton();
			RefreshMaximizeButton();
		}

		/// <summary>
		/// Sets the Canvas sortingOrder for this window. Called exclusively by <see cref="WindowManager"/>.
		/// </summary>
		internal void ApplySortOrder(int order) => _canvas.sortingOrder = order;

		/// <summary>
		/// Places the window so that the point <paramref name="pivot"/> (normalized on the window rect,
		/// e.g. (0,0) = bottom-left, (0.5,0.5) = centre, (1,1) = top-right) lands at
		/// <paramref name="position"/> in parent local space. Has no effect while maximized.
		/// </summary>
		public void SetPosition(Vector2 position, Vector2 pivot)
		{
			if (maximized) return;
			var parentRt = RectTransform.parent as RectTransform;
			if (parentRt == null) return;

			var r = parentRt.rect;
			var size = RectTransform.rect.size;
			var bottomLeft = new Vector2(
				r.xMin + RectTransform.anchorMin.x * r.width + RectTransform.offsetMin.x,
				r.yMin + RectTransform.anchorMin.y * r.height + RectTransform.offsetMin.y
			);
			var delta = position - (bottomLeft + pivot * size);
			RectTransform.offsetMin += delta;
			RectTransform.offsetMax += delta;
		}

		/// <summary>
		/// Same as <see cref="SetPosition"/> but <paramref name="normalizedPosition"/> is expressed
		/// as a fraction of the parent rect ((0,0) = bottom-left, (1,1) = top-right).
		/// </summary>
		public void SetPositionNormalized(Vector2 normalizedPosition, Vector2 pivot)
		{
			if (maximized) return;
			var parentRt = RectTransform.parent as RectTransform;
			if (parentRt == null) return;

			var r = parentRt.rect;
			SetPosition(new Vector2(r.xMin + normalizedPosition.x * r.width,
			                        r.yMin + normalizedPosition.y * r.height), pivot);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (maximized) return;
			
			WindowManager.Instance.BringToFront(this);

			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				RectTransform, eventData.position, eventData.pressEventCamera, out var localPoint);

			var rect = RectTransform.rect;
			int mask = 0;
			if (localPoint.x - rect.xMin <= resizeEdgeSize) mask |= ResizeLeft;
			if (rect.xMax - localPoint.x <= resizeEdgeSize) mask |= ResizeRight;
			if (localPoint.y - rect.yMin <= resizeEdgeSize) mask |= ResizeBottom;
			if (rect.yMax - localPoint.y <= resizeEdgeSize) mask |= ResizeTop;

			if (mask == 0) return;

			_resizing = true;
			_resizeEdgeMask = mask;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				RectTransform.parent as RectTransform, eventData.position, eventData.pressEventCamera, out _resizeDragStart);
			_resizeOffsetMinStart = RectTransform.offsetMin;
			_resizeOffsetMaxStart = RectTransform.offsetMax;
			_chromeSizeAtDragStart = RectTransform.rect.size - contentContainerRect.rect.size;
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (!_resizing) return;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				RectTransform.parent as RectTransform, eventData.position, eventData.pressEventCamera, out var localPoint);

			var delta = localPoint - _resizeDragStart;
			var offsetMin = _resizeOffsetMinStart;
			var offsetMax = _resizeOffsetMaxStart;

			if ((_resizeEdgeMask & ResizeLeft) != 0) offsetMin.x += delta.x;
			if ((_resizeEdgeMask & ResizeRight) != 0) offsetMax.x += delta.x;
			if ((_resizeEdgeMask & ResizeBottom) != 0) offsetMin.y += delta.y;
			if ((_resizeEdgeMask & ResizeTop) != 0) offsetMax.y += delta.y;

			if (content != null)
				ApplyContentSizeConstraints(ref offsetMin, ref offsetMax, _chromeSizeAtDragStart, _resizeEdgeMask);

			RectTransform.offsetMin = offsetMin;
			RectTransform.offsetMax = offsetMax;

			content?.OnResize();
		}

		/// <summary>
		/// Clamps the window to its content's size constraints immediately and notifies the content.
		/// Adjusts from the right and top edges, keeping the window's origin fixed.
		/// No-op while maximized or when no content is attached.
		/// </summary>
		public void EnforceContentSizeConstraints()
		{
			ClampWindowToContentConstraints();
			content?.OnResize();
		}

		/// <summary>
		/// Applies the size clamp without firing <see cref="WindowContent.OnResize"/>.
		/// Used during content configuration where the content may not be fully initialized yet.
		/// </summary>
		private void ClampWindowToContentConstraints()
		{
			if (content == null || maximized) return;
			var chromeSize = RectTransform.rect.size - contentContainerRect.rect.size;
			var offsetMin = RectTransform.offsetMin;
			var offsetMax = RectTransform.offsetMax;
			ApplyContentSizeConstraints(ref offsetMin, ref offsetMax, chromeSize, ResizeRight | ResizeTop);
			RectTransform.offsetMin = offsetMin;
			RectTransform.offsetMax = offsetMax;
		}

		/// <summary>
		/// Clamps <paramref name="offsetMin"/>/<paramref name="offsetMax"/> so the resulting window
		/// size satisfies the content's MinContentSize/MaxContentSize.
		/// <paramref name="edgeMask"/> controls which side is adjusted when a clamp fires —
		/// set to the active resize edge during a drag, or <c>ResizeRight|ResizeTop</c> to pin the origin.
		/// </summary>
		private void ApplyContentSizeConstraints(ref Vector2 offsetMin, ref Vector2 offsetMax, Vector2 chromeSize, int edgeMask)
		{
			var minContent = content.MinContentSize;
			var maxContent = content.MaxContentSize;

			var proposedW = offsetMax.x - offsetMin.x;
			var minW = minContent.x > 0f ? minContent.x + chromeSize.x : float.NegativeInfinity;
			var maxW = float.IsPositiveInfinity(maxContent.x) ? float.PositiveInfinity : maxContent.x + chromeSize.x;
			var clampedW = Mathf.Clamp(proposedW, minW, maxW);
			if (!Mathf.Approximately(clampedW, proposedW))
			{
				if ((edgeMask & ResizeLeft) != 0) offsetMin.x = offsetMax.x - clampedW;
				else offsetMax.x = offsetMin.x + clampedW;
			}

			var proposedH = offsetMax.y - offsetMin.y;
			var minH = minContent.y > 0f ? minContent.y + chromeSize.y : float.NegativeInfinity;
			var maxH = float.IsPositiveInfinity(maxContent.y) ? float.PositiveInfinity : maxContent.y + chromeSize.y;
			var clampedH = Mathf.Clamp(proposedH, minH, maxH);
			if (!Mathf.Approximately(clampedH, proposedH))
			{
				if ((edgeMask & ResizeBottom) != 0) offsetMin.y = offsetMax.y - clampedH;
				else offsetMax.y = offsetMin.y + clampedH;
			}
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			_resizing = false;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			MinimizeEnabled = minimizeEnabled;
			MaximizeEnabled = maximizeEnabled;
		}

		/// <summary>
		/// Called by a child <see cref="WindowContent"/> from its own OnValidate to push
		/// its control changes to this window.
		/// </summary>
		internal void SyncFromContent()
		{
			RefreshMinimizeButton();
			RefreshMaximizeButton();
		}
#endif
		
		private void Awake()
		{
			RectTransform = GetComponent<RectTransform>();
			contentContainerRect = contentContainer.GetComponent<RectTransform>();
			_canvas = GetComponent<Canvas>();

			if (RectTransform == null || contentContainerRect == null)
				Logr.Error("Window and its content must have RectTransforms.");
			if (_canvas == null)
				Logr.Error("Window requires a Canvas component on the same GameObject.");

			WindowManager.Instance.RegisterWindow(new InternalWindowHandle(this));
			
			closeButton.onClick.AddListener(Quit);
			maximizeButton?.onClick.AddListener(ToggleMaximize);
			minimizeButton?.onClick.AddListener(Minimize);
			
			shown = startShown;
			gameObject.SetActive(startShown);
			
			// if content is assigned in the editor, initialize it
			var initialContent = contentContainer.GetComponentInChildren<WindowContent>();
			if (initialContent != null)
			{
				content = initialContent;
				ConfigureContent();
				content.OnInitialize();
				NotifyContentResized();
			}
			else
			{
				RefreshMinimizeButton();
				RefreshMaximizeButton();
			}
		}

		private void OnEnable()
		{
			Logr.Info($"Show window '{Title}'", this);
			if (content != null)
			{
				if (_pendingResizeOnShow)
				{
					_pendingResizeOnShow = false;
					StartCoroutine(DeferredOnResize());
				}
				content.OnShow();
			}
			shown = true;
		}

		private IEnumerator DeferredOnResize()
		{
			// Wait one frame so all child Awakes and Starts in the content hierarchy
			// complete before we notify the content of its initial size.
			for (int i = 0; i < 8; i++) yield return null;
			content?.OnResize();
		}
		
		private void OnDisable()
		{
			if (!shown) return;
			Logr.Info($"Minimize/Close window '{Title}'", this);
			if (content != null)
			{
				content.OnMinimize();
			}
			shown = false;
		}

		private void OnDestroy()
		{
			WindowManager.Instance.UnregisterWindow(WindowId);
		}
	}
}