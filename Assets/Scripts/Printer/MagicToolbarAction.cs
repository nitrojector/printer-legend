using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Printer
{
	public class MagicToolbarAction : MonoBehaviour
	{
		/// <summary>
		/// Color of the <see cref="iconContainerImage"/> when the magic is inactive
		/// </summary>
		[SerializeField] public Color32 defaultColor = new Color32(255, 255, 255, 132);
		
		/// <summary>
		/// Color of the <see cref="iconContainerImage"/> when the magic is active
		/// </summary>
		[SerializeField] public Color32 activeColor = new Color32(255, 160, 160, 162);
		
		/// <summary>
		/// The image for the icon background/container
		/// </summary>
		[SerializeField] public Image iconContainerImage;
		
		/// <summary>
		/// Label for the keybind associated with this magic
		/// </summary>
		[SerializeField] public TMP_Text keybindLabelText;
		
		/// <summary>
		/// Sets the visual style of this action to active or inactive.
		/// This should be called by the owning <see cref="MagicToolbar"/> when the active magic changes.
		/// </summary>
		public void SetStyleActive(bool active)
		{
			if (iconContainerImage != null)
				iconContainerImage.color = active ? activeColor : defaultColor;
		}
		
#if UNITY_EDITOR
		private void OnValidate()
		{
			if (iconContainerImage != null)
				iconContainerImage.color = defaultColor;
		}
#endif
	}
}