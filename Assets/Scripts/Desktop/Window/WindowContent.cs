using UnityEngine;

namespace Desktop.Window
{
	public abstract class WindowContent : MonoBehaviour
	{
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