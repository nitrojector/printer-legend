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

		[Header("Images")]
		[SerializeField] private RawImage creationDisplay;
		[SerializeField] private RawImage referenceDisplay;

		[Header("Info")]
		[SerializeField] private TMP_Text detailsText;

		private GalleryEntry _entry;

		/// <summary>
		/// Populates the detail view with <paramref name="entry"/>.
		/// Safe to call before or after <see cref="WindowContent.OnInitialize"/>.
		/// </summary>
		public void SetEntry(GalleryEntry entry)
		{
			_entry = entry;

			if (entry == null)
			{
				if (creationDisplay != null) creationDisplay.texture = null;
				if (referenceDisplay != null) referenceDisplay.texture = null;
				if (detailsText != null) detailsText.text = string.Empty;
				return;
			}

			if (creationDisplay != null)
				creationDisplay.texture = GalleryManager.LoadImage(entry);
			if (referenceDisplay != null)
				referenceDisplay.texture = GalleryManager.LoadReferenceImage(entry);
			if (detailsText != null)
				detailsText.text = BuildDetailsText(entry);
		}

		public override bool OnQuit()
		{
			_entry = null;
			return true;
		}

		private static string BuildDetailsText(GalleryEntry e)
		{
			var date = e.Date.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
			var score = $"{e.SimilarityScore * 100f:F1}%";
			var duration = e.PrintDuration >= 60f
				? $"{(int)(e.PrintDuration / 60)}m {(int)(e.PrintDuration % 60)}s"
				: $"{e.PrintDuration:F1}s";

			return $"<u>Date</u>\n{date}\n\n" +
			       $"<u>Similarity</u>\n{score}\n\n" +
			       $"<u>Resets</u>\n{e.ResetCount}\n\n" +
			       $"<u>Print Duration</u>\n{duration}";
		}
	}
}
