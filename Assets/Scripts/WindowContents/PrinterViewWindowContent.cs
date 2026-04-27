using System;
using Config;
using Data;
using Desktop.WindowSystem;
using Gallery;
using Printer;
using UnityEngine;
using UnityEngine.Serialization;

namespace WindowContents
{
	public class PrinterViewWindowContent : WindowContent
	{
		public override string WindowTitle => "Printer View";

		[SerializeField, FormerlySerializedAs("printCanvas")] public PrintCanvas pCanvas;
		[SerializeField, FormerlySerializedAs("controller")] public PrintheadController pController;
		[SerializeField, FormerlySerializedAs("playerController")] public PrinterPlayerController pPlayerController;

		private bool _isProgressionMode;

		public override void OnInitialize()
		{
			GameMgr.Instance.RegisterPrinterView(this);
			if (pPlayerController != null)
				pPlayerController.OnPrintComplete += HandlePrintComplete;
		}

		private void OnDestroy()
		{
			GameMgr.Instance.UnregisterPrinterView(this);
			if (pPlayerController != null)
				pPlayerController.OnPrintComplete -= HandlePrintComplete;
		}

		public override void OnResize()
		{
			if (pController) pController.RefreshLayout();
		}

		/// <summary>
		/// Enables or disables progression mode. When true, completing a print advances
		/// UserSave.ProgressionNextPrintIdx and opens PrintSummaryWindowContent.
		/// </summary>
		public void SetProgressionMode(bool value) => _isProgressionMode = value;

		/// <summary>
		/// Applies a level's abilities and obstacles to the PrinterMagic on this content's printhead.
		/// Call from the Launch configurator before the window is shown.
		/// </summary>
		public void ApplyLevel(LevelEntry entry)
		{
			if (pController == null) return;
			var magic = pController.GetComponent<PrinterMagic>();
			if (magic == null) return;

			foreach (PrinterAbility ability in Enum.GetValues(typeof(PrinterAbility)))
				magic.DisableAbility(ability);
			foreach (PrinterObstacle obstacle in Enum.GetValues(typeof(PrinterObstacle)))
				magic.DisableObstacle(obstacle);

			foreach (var ability in entry.GetAbilities())
				magic.EnableAbility(ability);
			foreach (var obstacle in entry.GetObstacles())
				magic.EnableObstacle(obstacle);
		}

		/// <summary>
		/// Returns the reference sprite for a given level index. Null if out of range or not found.
		/// </summary>
		public static Sprite GetReferenceSprite(int levelIndex)
		{
			var config = LevelSequenceConfig.Instance;
			if (config == null || levelIndex >= config.Levels.Count) return null;
			string name = System.IO.Path.GetFileName(config.Levels[levelIndex].ImagePath);
			return PrintRefManager.Instance.GetByName(name);
		}

		private void HandlePrintComplete()
		{
			float accuracy = (float)PrintState.GetSimilarityScore();
			string refPath = BuildReferencePath();
			var entry = GalleryManager.SaveEntry(
				pCanvas.GetTexture(), refPath, accuracy,
				pPlayerController.RestartCount, pPlayerController.PrintDuration);

			if (_isProgressionMode && entry != null)
			{
				UserSave.Instance.ProgressionNextPrintIdx++;
				UserSave.Save();

				var captured = entry;
				WindowManager.Instance.Launch<PrintSummaryWindowContent>((w, c) =>
				{
					w.SetPositionNormalized(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
					c.SetEntry(captured);
				});
			}

			GameMgr.Instance.PrinterReferenceWC?.Close();
			AttachedWindow?.Quit();
		}

		private string BuildReferencePath()
		{
			var sprite = GameMgr.Instance.PrinterReferenceWC?.pReference.ReferenceSprite;
			if (sprite == null) return string.Empty;
			return GalleryManager.MakeInternalPath("PrintRefs/" + sprite.name);
		}
	}
}
