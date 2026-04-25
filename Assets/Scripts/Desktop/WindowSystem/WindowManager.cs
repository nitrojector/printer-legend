using System.Collections.Generic;
using UnityEngine;

namespace Desktop.WindowSystem
{
	public class WindowManager
	{
		public static WindowManager Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new WindowManager();
				}
				return _instance;
			}
		}
		
		public readonly List<Window> ActiveWindows = new();
		
		private static WindowManager _instance;
		
		private int nextWindowId = 1;
		private readonly SortedSet<int> freedWindowIds = new();
		private readonly Dictionary<int, Window.InternalWindowHandle> windows = new();

		/// <summary>
		/// Destroys the window with the given ID.
		/// This bypasses normal window closing procedures.
		/// </summary>
		/// <param name="id"></param>
		public void Kill(int id)
		{
			Object.Destroy(windows[id].target.gameObject);
		}
		
		/// <summary>
		/// Destroys all windows.
		/// This bypasses normal window closing procedures.
		/// </summary>
		public void KillAll()
		{
			foreach (var win in ActiveWindows)
			{
				Object.Destroy(win.gameObject);
			}
			ActiveWindows.Clear();
			windows.Clear();
			freedWindowIds.Clear();
			nextWindowId = 1;
		}
		
		/// <summary>
		/// Registers a window with the WindowManager.
		/// Should only be called by <see cref="Window"/>
		/// </summary>
		/// <param name="win"></param>
		public void RegisterWindow(Window.InternalWindowHandle win)
		{
			var id = GetNextWindowId();
			win.SetWindowId(id);
			ActiveWindows.Add(win.target);
			windows[id] = win;
		}
		
		public void UnregisterWindow(int id)
		{
			if (!windows.ContainsKey(id)) return;
			
			ActiveWindows.Remove(windows[id].target);
			windows.Remove(id);
			freedWindowIds.Add(id);
		}

		private int GetNextWindowId()
		{
			if (freedWindowIds.Count == 0) return nextWindowId++;
			int id = freedWindowIds.Min;
			freedWindowIds.Remove(id);
			return id;
		}
	}
}