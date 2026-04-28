using System.Text;
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

		[Header("Containers")]
		[SerializeField] private GameObject creationContainer;
		[SerializeField] private GameObject referenceContainer;

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

		public void SetEntry(GalleryEntry entry, bool isProgression = false)
		{
			_entry = entry;

			creationContainer?.SetActive(true);
			referenceContainer?.SetActive(entry.HasRef);
			nextPrintButton?.gameObject.SetActive(isProgression);

			if (creationDisplay != null)
				creationDisplay.texture = GalleryManager.LoadImage(entry);
			if (referenceDisplay != null)
				referenceDisplay.texture = entry.HasRef ? GalleryManager.LoadReferenceImage(entry) : null;
			if (detailsText != null)
				detailsText.text = BuildDetailsText(entry);
		}

		public override bool OnQuit()
		{
			_entry = null;
			return true;
		}

		private void OnNextPrint()
		{
			CloseWindow();
			GameActions.Instance.OpenProgressionPrint();
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
