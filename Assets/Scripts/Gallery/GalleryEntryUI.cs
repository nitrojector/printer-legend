using System;
using Data;
using UnityEngine;
using UnityEngine.UI;

namespace Gallery
{
	/// <summary>
	/// Prefab component for a single entry card in the gallery grid.
	/// Call <see cref="Setup"/> after instantiation.
	/// Single click fires <see cref="OnSelected"/>; double click also fires <see cref="OnDoubleClicked"/>.
	/// </summary>
	public class GalleryEntryUI : MonoBehaviour
	{
		[SerializeField] private RawImage creationThumbnail;
		[SerializeField] private Button selectButton;
		[SerializeField] private GameObject selectedIndicator;

		private const float DoubleClickThreshold = 0.3f;
		private float _lastClickTime = -1f;

		public GalleryEntry Entry { get; private set; }

		public event Action<GalleryEntryUI> OnSelected;
		public event Action<GalleryEntryUI> OnDoubleClicked;

		private void Awake()
		{
			selectButton?.onClick.AddListener(HandleClick);
		}

		private void HandleClick()
		{
			float now = Time.unscaledTime;
			bool isDouble = now - _lastClickTime < DoubleClickThreshold;
			_lastClickTime = now;

			OnSelected?.Invoke(this);
			if (isDouble)
				OnDoubleClicked?.Invoke(this);
		}

		public void Setup(GalleryEntry entry)
		{
			Entry = entry;
			if (creationThumbnail != null)
				creationThumbnail.texture = GalleryManager.LoadImage(entry);
			SetSelected(false);
		}

		public void SetSelected(bool selected)
		{
			if (selectedIndicator != null)
				selectedIndicator.SetActive(selected);
		}

		private void OnDestroy()
		{
			if (Entry == null) return;
			GalleryManager.UnloadImage(Entry);
		}
	}
}
