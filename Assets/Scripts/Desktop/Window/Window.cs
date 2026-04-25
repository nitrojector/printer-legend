using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utility;

namespace Desktop.Window
{
	public class Window : MonoBehaviour
	{
		/// <summary>
		/// Gets or sets the title of the window. This is displayed in the title bar of the window.
		/// </summary>
		public string Title { 
			get => titleText.text; 
			set => titleText.text = value; 
		}
		
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
			get => maximizeEnabled; 
			set
			{
				maximizeEnabled = value;
				if (maximizeButton != null)
				{
					maximizeButton.gameObject.SetActive(value);
				}
			}
		}

		/// <summary>
		/// Gets or sets whether minimize is enabled.
		/// If disabled, the minimize button is hidden and the window cannot be minimized.
		/// </summary>
		public bool MinimizeEnabled
		{
			get => minimizeEnabled;
			set
			{
				minimizeEnabled = value;
				if (minimizeButton != null)
				{
					minimizeButton.gameObject.SetActive(value);
				}
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
		
		// State
		private bool shown = false;
		private bool maximized = false;
		private FloatingWindowData floatingData = default;
		
		// RTs
		private RectTransform contentContainerRect;
		
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
				content.OnMaximize();
				Logr.Info("Maximizing window.");
			}
			else
			{
				floatingData.ApplyTo(RectTransform);
			}

			maximized = !maximized;
		}

		/// <summary>
		/// Closes the window, destroying it and any attached content.
		/// </summary>
		public void Close()
		{
			RemoveContent();
			Destroy(gameObject);
		}

		/// <summary>
		/// Removes the currently attached content GameObject from the window. The content GameObject is destroyed.
		/// </summary>
		public void RemoveContent()
		{
			Destroy(content);
		}

		/// <summary>
		/// Initializes the window with the specified prefab as its content. Any existing content will be destroyed.
		/// If the prefab implements <see cref="IWindowContent"/>,
		/// it will also receive lifecycle callbacks.
		/// </summary>
		/// <param name="prefab"></param>
		public void Initialize(WindowContent prefab)
		{
			if (prefab == null) return;
			if (content != null)
				Destroy(content);
			
			content = Instantiate(prefab, contentContainer.transform);
			AlignContent(content);
			content.OnInitialize();
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
				Destroy(content);
			}
			
			newContent.transform.SetParent(contentContainer.transform);
			AlignContent(newContent);
			content = newContent;
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
				prev.transform.SetParent(reparent);
			}
			
			newContent.transform.SetParent(contentContainer.transform);
			AlignContent(newContent);
			content = newContent;
			return prev;
		}

		private static void AlignContent(WindowContent newContent)
		{
			RectTransform contentRt = newContent.GetComponent<RectTransform>();
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
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			MinimizeEnabled = minimizeEnabled;
			MaximizeEnabled = maximizeEnabled;
		}
#endif
		
		private void Awake()
		{
			RectTransform = GetComponent<RectTransform>();
			contentContainerRect = contentContainer.GetComponent<RectTransform>();

			if (RectTransform == null || contentContainerRect == null)
			{
				Logr.Error("Window and its content must have RectTransforms.");
			}
			
			closeButton.onClick.AddListener(Close);
			maximizeButton?.onClick.AddListener(ToggleMaximize);
			minimizeButton?.onClick.AddListener(Minimize);
			
			shown = false;
			gameObject.SetActive(false);
			
			// if content is assigned in the editor, initialize it
			{
				var initialContent = contentContainer.GetComponentInChildren<WindowContent>();
				if (initialContent != null)
				{
					content = initialContent;
					AlignContent(content);
					content.OnInitialize();
				}
			}
			
			if (!MinimizeEnabled)
				minimizeButton?.gameObject.SetActive(false);
			if (!MaximizeEnabled)
				maximizeButton?.gameObject.SetActive(false);
			
			if (startShown) Show();
		}

		private void OnEnable()
		{
			Logr.Info($"Showing window '{Title}'");
			if (content != null)
			{
				content.OnShow();
			}
			shown = true;
		}
		
		private void OnDisable()
		{
			if (!shown) return;
			Logr.Info($"Minimizing window '{Title}'");
			if (content != null)
			{
				content.OnMinimize();
			}
			shown = false;
		}

	}
}