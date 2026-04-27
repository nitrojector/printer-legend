using Data;
using Desktop.WindowSystem;
using Gallery;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WindowContents
{
	public class PrintSummaryWindowContent : WindowContent
	{
		public override string WindowTitle => "Print Summary";
		public override bool AllowMaximize => false;
		public override bool AllowMinimize => false;

		[Header("Images")]
		[SerializeField] private RawImage creationDisplay;
		[SerializeField] private RawImage referenceDisplay;

		[Header("Info")]
		[SerializeField] private TMP_Text detailsText;

		[Header("Actions")]
		[SerializeField] private Button nextPrintButton;

		private GalleryEntry _entry;

		private void Awake()
		{
			nextPrintButton?.onClick.AddListener(OnNextPrint);
		}

		public void SetEntry(GalleryEntry entry)
		{
			_entry = entry;
			if (creationDisplay != null)
				creationDisplay.texture = GalleryManager.LoadImage(entry);
			if (referenceDisplay != null)
				referenceDisplay.texture = GalleryManager.LoadReferenceImage(entry);
			if (detailsText != null)
				detailsText.text = BuildDetailsText(entry);
		}

		public override bool OnQuit()
		{
			if (_entry != null)
			{
				GalleryManager.UnloadImage(_entry);
				GalleryManager.UnloadReferenceImage(_entry);
			}
			return true;
		}

		private void OnNextPrint()
		{
			CloseWindow();
			GameActions.Instance.OpenProgressionPrint();
		}

		private static string BuildDetailsText(GalleryEntry e)
		{
			var date = e.Date.ToLocalTime().ToString("yyyy-MM-dd  HH:mm");
			var score = e.SimilarityScore >= 0f
				? $"{e.SimilarityScore * 100f:F1}%"
				: "N/A";
			var duration = e.PrintDuration >= 60f
				? $"{(int)(e.PrintDuration / 60)}m {(int)(e.PrintDuration % 60)}s"
				: $"{e.PrintDuration:F1}s";

			return $"Date:            {date}\n" +
			       $"Similarity:      {score}\n" +
			       $"Resets:          {e.ResetCount}\n" +
			       $"Print Duration:  {duration}";
		}
	}
}
