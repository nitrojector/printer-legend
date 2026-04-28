using System.Text;
using Data;
using Desktop.WindowSystem;
using Gallery;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WindowContents
{
	public class GalleryEntryDetailWindowContent : WindowContent
	{
		public override string WindowTitle => "Print Details";

		[Header("Containers")]
		[SerializeField] private GameObject creationContainer;
		[SerializeField] private GameObject referenceContainer;

		[Header("Images")]
		[SerializeField] private RawImage creationDisplay;
		[SerializeField] private RawImage referenceDisplay;

		[Header("Info")]
		[SerializeField] private TMP_Text detailsText;

		private GalleryEntry _entry;
		private Texture2D _ownedCreationTex;
		private Texture2D _ownedRefTex;
		private bool _ownsRefTex;

		public void SetEntry(GalleryEntry entry)
		{
			ReleaseOwnedTextures();
			_entry = entry;

			if (entry == null)
			{
				creationContainer?.SetActive(false);
				referenceContainer?.SetActive(false);
				if (detailsText != null) detailsText.text = string.Empty;
				return;
			}

			creationContainer?.SetActive(true);
			referenceContainer?.SetActive(entry.HasRef);

			_ownedCreationTex = GalleryManager.LoadImageOwned(entry);
			if (creationDisplay != null)
				creationDisplay.texture = _ownedCreationTex;

			_ownsRefTex = entry.HasRef && !GalleryManager.IsInternalReference(entry.ReferenceImagePath);
			_ownedRefTex = entry.HasRef ? GalleryManager.LoadReferenceImageOwned(entry) : null;
			if (referenceDisplay != null)
				referenceDisplay.texture = _ownedRefTex;

			if (detailsText != null)
				detailsText.text = BuildDetailsText(entry);
		}

		public override bool OnQuit()
		{
			_entry = null;
			return true;
		}

		private void OnDestroy() => ReleaseOwnedTextures();

		private void ReleaseOwnedTextures()
		{
			if (_ownedCreationTex != null) { Destroy(_ownedCreationTex); _ownedCreationTex = null; }
			if (_ownsRefTex && _ownedRefTex != null) { Destroy(_ownedRefTex); _ownedRefTex = null; }
			_ownedRefTex = null;
			_ownsRefTex = false;
		}

		private static string BuildDetailsText(GalleryEntry e)
		{
			var date = e.Date.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
			var duration = e.PrintDuration >= 60f
				? $"{(int)(e.PrintDuration / 60)}m {(int)(e.PrintDuration % 60)}s"
				: $"{e.PrintDuration:F1}s";

			var sb = new StringBuilder();
			sb.Append($"<u>Date</u>\n{date}\n\n");
			if (e.HasRef)
				sb.Append($"<u>Similarity</u>\n{e.SimilarityScore * 100f:F1}%\n\n");
			sb.Append($"<u>Resets</u>\n{e.ResetCount}\n\n");
			sb.Append($"<u>Print Duration</u>\n{duration}");
			return sb.ToString();
		}
	}
}
