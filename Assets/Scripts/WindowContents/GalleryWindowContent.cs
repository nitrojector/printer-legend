using System.Collections.Generic;
using Data;
using Desktop.WindowSystem;
using EngineSystem;
using Gallery;
using UnityEngine;
using UnityEngine.UI;

namespace WindowContents
{
	public class GalleryWindowContent : WindowContent
	{
		public override string WindowTitle => "Gallery";

		[Header("Gallery")]
		[SerializeField] private Transform galleryContainer;

		[Header("Controls")]
		[SerializeField] private Button deleteButton;

		private readonly List<GalleryEntryUI> _entryUIs = new();
		private GalleryEntryUI _selected;

		private void Awake()
		{
			deleteButton?.onClick.AddListener(OnDeleteClicked);
		}

		public override void OnShow()
		{
			Refresh();
		}

		private void Refresh()
		{
			foreach (var ui in _entryUIs)
				if (ui != null) Destroy(ui.gameObject);
			_entryUIs.Clear();
			_selected = null;
			UpdateButtonStates();

			var prefab = ReferenceManager.Instance.galleryEntryPrefab;
			foreach (var entry in GalleryManager.GetEntries())
			{
				var ui = Instantiate(prefab, galleryContainer);
				ui.Setup(entry);
				ui.OnSelected += SelectEntry;
				ui.OnDoubleClicked += e => OpenDetailWindow(e.Entry);
				_entryUIs.Add(ui);
			}
		}

		private void SelectEntry(GalleryEntryUI ui)
		{
			if (_selected != null) _selected.SetSelected(false);
			_selected = ui;
			if (_selected != null) _selected.SetSelected(true);
			UpdateButtonStates();
		}

		private void UpdateButtonStates()
		{
			if (deleteButton != null) deleteButton.interactable = _selected != null;
		}

		private void OnDeleteClicked()
		{
			if (_selected == null) return;
			var entryToDelete = _selected.Entry;
			WindowManager.Instance.Launch<ConfirmationPopupWindowContent>((w, c) =>
			{
				w.SetPositionNormalized(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
				c.Title = "Delete Entry";
				c.Message = $"Delete \"{entryToDelete.Date.ToLocalTime():yyyy/MM/dd HH:mm}\"? This cannot be undone.";
				c.ConfirmButtonText = "Delete";
				c.CancelButtonText = "Cancel";
				c.OnConfirm += () =>
				{
					GalleryManager.RemoveEntry(entryToDelete);
					Refresh();
				};
			});
		}

		private void OpenDetailWindow(GalleryEntry entry)
		{
			WindowManager.Instance.Launch<GalleryEntryDetailWindowContent>((_, detail) => detail.SetEntry(entry));
		}
	}
}
