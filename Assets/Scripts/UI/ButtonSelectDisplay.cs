using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
	/// <summary>
	/// Shows a target GameObject while the button this is attached to is selected,
	/// and hides it when the button is deselected.
	/// </summary>
	public class ButtonSelectDisplay : MonoBehaviour, ISelectHandler, IDeselectHandler
	{
		[SerializeField] private GameObject target;

		private void Awake()
		{
			if (target != null) target.SetActive(false);
		}

		public void OnSelect(BaseEventData eventData)
		{
			if (target != null) target.SetActive(true);
		}

		public void OnDeselect(BaseEventData eventData)
		{
			if (target != null) target.SetActive(false);
		}
	}
}
