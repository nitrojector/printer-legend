using System;
using System.Collections.Generic;
using EngineSystem;
using UnityEngine;
using Utility;

namespace Desktop.WindowSystem
{
	[DefaultExecutionOrder(-100)]
	public class WindowManager : MonoBehaviour
	{
		private static WindowManager _instance;

		public static WindowManager Instance
		{
			get
			{
				if (_instance != null) return _instance;
				
				var go = new GameObject("WindowManager");
				_instance = go.AddComponent<WindowManager>();
				return _instance;
			}
		}
		
		private DesktopWrapper _desktop;

		/// <summary>Base Canvas sortingOrder for the bottom-most window. Each window above it adds 1.</summary>
		[SerializeField] private int baseSortOrder = 0;

		/// <summary>Windows ordered back-to-front; the last entry is the focused window.</summary>
		private readonly List<Window> _focusStack = new();

		private readonly Dictionary<int, Window.InternalWindowHandle> _handles = new();
		private readonly SortedSet<int> _freedIds = new();
		private int _nextId = 1;

		/// <summary>Read-only view of all active windows, ordered back-to-front.</summary>
		public IReadOnlyList<Window> ActiveWindows => _focusStack;

		/// <summary>The currently focused (front-most) window, or null if none.</summary>
		public Window FocusedWindow => _focusStack.Count > 0 ? _focusStack[^1] : null;

		private void Awake()
		{
			if (_instance != null && _instance != this)
			{
				Destroy(gameObject);
				return;
			}
			
			_instance = this;
			DontDestroyOnLoad(gameObject);
		}

		private void OnDestroy()
		{
			if (_instance == this) _instance = null;
		}

		private DesktopWrapper GetDesktop()
		{
			if (_desktop != null) return _desktop;
			_desktop = FindAnyObjectByType<DesktopWrapper>();
			if (_desktop == null)
			{
				Logr.Error("WindowManager requires a DesktopWrapper in the scene.", this);
			}
			return _desktop;
		}

		// ── Registration ──────────────────────────────────────────────────────

		/// <summary>
		/// Registers a new window. The window is placed at the front of the focus stack.
		/// Should only be called by <see cref="Window"/>.
		/// </summary>
		public void RegisterWindow(Window.InternalWindowHandle handle)
		{
			int id = AllocId();
			handle.SetWindowId(id);
			_handles[id] = handle;
			_focusStack.Add(handle.target);
			AssignSortOrders();
		}

		/// <summary>
		/// Removes a window from the manager.
		/// Should only be called by <see cref="Window"/>.
		/// </summary>
		public void UnregisterWindow(int id)
		{
			if (!_handles.TryGetValue(id, out var handle)) return;
			_focusStack.Remove(handle.target);
			_handles.Remove(id);
			_freedIds.Add(id);
			AssignSortOrders();
		}
		
		// ── Launch ────────────────────────────────────────────────────────────

		/// <summary>
		/// Launches a new window with content of type <typeparamref name="T"/>. <paramref name="configurator"/>
		/// is called with <see cref="WindowContent"/> before the window is shown.
		/// </summary>
		/// <param name="configurator">delegate for configurator function for window and content</param>
		/// <typeparam name="T">window content to launch</typeparam>
		/// <returns>newly launched content</returns>
		public T Launch<T>(Action<Window, T> configurator = null) where T : WindowContent
		{
			var desktop = GetDesktop();
			var win = Instantiate(ReferenceManager.Instance.windowPrefab, desktop.transform);
			win.Initialize(GetPrefab<T>());
			var content = win.Content as T;
			if (content != null)
			{
				configurator?.Invoke(win, content);
			}
			else
			{
				Logr.Error($"Launched window content of type {win.Content.GetType().Name}, " +
				           $"but expected {typeof(T).Name}. Check the prefab setup.", this);
				return null;
			}
			win.Show();
			return content;
		}

		private T GetPrefab<T>() where T : WindowContent
		{
			string typeNamePrefix = typeof(T).Name.Replace("WindowContent", "");
			string path = $"WindowContent/{typeNamePrefix}";
			T prefab = Resources.Load<T>(path);
			if (prefab == null)
			{
				Logr.Error($"Failed to load window content prefab at path \"{path}\". " +
				           $"Make sure the prefab exists and is named correctly.", this);
			}
			return prefab;
		}

		// ── Focus ─────────────────────────────────────────────────────────────

		/// <summary>
		/// Moves <paramref name="window"/> to the front of the stack and reassigns sort orders.
		/// No-op if the window is already focused or not registered.
		/// </summary>
		public void BringToFront(Window window)
		{
			int idx = _focusStack.IndexOf(window);
			if (idx < 0 || idx == _focusStack.Count - 1) return;
			_focusStack.RemoveAt(idx);
			_focusStack.Add(window);
			AssignSortOrders();
		}

		// ── Bulk operations ───────────────────────────────────────────────────

		/// <summary>Destroys the window with the given ID, bypassing normal close procedures.</summary>
		public void Kill(int id)
		{
			if (_handles.TryGetValue(id, out var handle))
				Destroy(handle.target.gameObject);
		}

		/// <summary>Destroys all windows, bypassing normal close procedures.</summary>
		public void KillAll()
		{
			var snapshot = new List<Window>(_focusStack);
			foreach (var w in snapshot)
				Destroy(w.gameObject);
		}

		// ── Internals ─────────────────────────────────────────────────────────

		private void AssignSortOrders()
		{
			for (int i = 0; i < _focusStack.Count; i++)
				_focusStack[i].ApplySortOrder(baseSortOrder + i);
		}

		private int AllocId()
		{
			if (_freedIds.Count == 0) return _nextId++;
			int id = _freedIds.Min;
			_freedIds.Remove(id);
			return id;
		}
	}
}
