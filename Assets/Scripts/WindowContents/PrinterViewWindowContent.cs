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
		[SerializeField] public PrinterMagic pMagic;
		[SerializeField, FormerlySerializedAs("playerController")] public PrinterPlayerController pPlayerController;

		/// <summary>ID assigned by GameMgr on registration. Valid after OnInitialize.</summary>
		public int PrintViewId { get; private set; } = -1;

		private bool _isProgressionMode;

		public override void OnInitialize()
		{
			PrintViewId = GameMgr.Instance.RegisterPrintView(this);
			if (pPlayerController != null)
				pPlayerController.OnPrintComplete += HandlePrintComplete;
		}

		private void OnDestroy()
		{
			if (pPlayerController != null)
				pPlayerController.OnPrintComplete -= HandlePrintComplete;
			if (PrintViewId >= 0)
				GameMgr.Instance.UnregisterPrintView(PrintViewId);
		}

		public override void OnResize()
		{
			if (pController) pController.RefreshLayout();
		}

		public override string GetContentDescription()
		{
			return $"pView PrintID({PrintViewId}) Progression({_isProgressionMode})";
		}
		
		/// <summary>
		/// Enables or disables all magic abilities and obstacles on this content's printhead.
		/// </summary>
		/// <param name="enableAllMagic">if all magic abilities should be enabled</param>
		public void SetAllMagicAbilitiesEnabled(bool enableAllMagic)
		{
			if (pMagic == null) return;

			foreach (PrinterAbility ability in Enum.GetValues(typeof(PrinterAbility)))
				pMagic.SetAbilityEnabled(ability, enableAllMagic);
		}
		
		/// <summary>
		/// Enables or disables all magic obstacles on this content's printhead.
		/// </summary>
		/// <param name="enableAllMagic">if all obstacles should be enabled</param>
		public void SetAllMagicObstaclesEnabled(bool enableAllMagic)
		{
			if (pController == null) return;
			var magic = pController.GetComponent<PrinterMagic>();
			if (magic == null) return;

			foreach (PrinterObstacle obstacle in Enum.GetValues(typeof(PrinterObstacle)))
				magic.SetObstacleEnabled(obstacle, enableAllMagic);
		}

		/// <summary>
		/// Marks this view as part of the progression flow. Also notifies GameMgr.
		/// Call from the Launch configurator.
		/// </summary>
		public void SetProgressionMode(bool value)
		{
			_isProgressionMode = value;
			if (PrintViewId >= 0)
				GameMgr.Instance.SetPrintViewProgressionMode(PrintViewId, value);
		}

		/// <summary>
		/// Applies a level's abilities and obstacles to the PrinterMagic on this content's printhead.
		/// Call from the Launch configurator.
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

		/// <summary>Returns the reference sprite for a given level index. Null if out of range.</summary>
		public static Sprite GetReferenceSprite(int levelIndex)
		{
			var config = LevelSequenceConfig.Instance;
			if (config == null || levelIndex >= config.Levels.Count) return null;
			string name = System.IO.Path.GetFileName(config.Levels[levelIndex].ImagePath);
			return PrintRefManager.Instance.GetByName(name);
		}

		private void HandlePrintComplete()
		{
			var refWc     = GameMgr.Instance.GetReference(PrintViewId);
			var refSprite = refWc?.pReference?.ReferenceSprite;

			float  accuracy = refSprite != null
				? (float)PrintState.GetSimilarityScore(pCanvas.GetTexture(), refSprite)
				: 0f;
			string refPath = refSprite != null
				? GalleryManager.MakeInternalPath("PrintRefs/" + refSprite.name)
				: string.Empty;

			var entry = GalleryManager.SaveEntry(
				pCanvas.GetTexture(), refPath, accuracy,
				pPlayerController.RestartCount, pPlayerController.PrintDuration);

			if (_isProgressionMode && entry != null)
			{
				UserSave.Instance.ProgressionNextPrintIdx++;
				UserSave.Save();
			}

			if (entry != null)
			{
				var captured = entry;
				WindowManager.Instance.Launch<PrintSummaryWindowContent>((w, c) =>
				{
					w.SetPositionNormalized(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
					c.SetEntry(captured, _isProgressionMode);
				});
			}

			refWc?.Close();
			AttachedWindow?.Quit();
		}
	}
}
