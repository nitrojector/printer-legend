using Config;
using Desktop.WindowSystem;
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

		public void ReactivateForNextLevel()
		{
			pPlayerController?.ResetForNextLevel();
			AttachedWindow?.Show();
			GameMgr.Instance.PrinterReferenceWC?.SetWindowVisible(true);
		}

		private void HandlePrintComplete()
		{
			int idx = LevelManager.CurrentLevelIndex;
			Sprite refSprite = GetReferenceSprite(idx);
			float accuracy = CalculateAccuracy(pCanvas.DO_NOT_MODIFY_CanvasInternalTexture, refSprite);
			int restarts = pPlayerController.RestartCount;

			AttachedWindow?.Minimize();
			GameMgr.Instance.PrinterReferenceWC?.SetWindowVisible(false);

			WindowManager.Instance.Launch<PrintFinalImageWindowContent>((w, c) =>
			{
				w.SetPositionNormalized(new(0.25f, 0.5f), new(0.5f, 0.5f));
				c.SetPrintTexture(pCanvas.DO_NOT_MODIFY_CanvasInternalTexture);
			});

			WindowManager.Instance.Launch<PrintSummaryWindowContent>((w, c) =>
			{
				w.SetPositionNormalized(new(0.75f, 0.5f), new(0.5f, 0.5f));
				c.SetData(restarts, accuracy, this);
			});
		}

		public static Sprite GetReferenceSprite(int levelIndex)
		{
			var config = LevelSequenceConfig.Instance;
			if (config == null || levelIndex >= config.Levels.Count) return null;
			string name = System.IO.Path.GetFileName(config.Levels[levelIndex].ImagePath);
			return PrintRefManager.Instance.GetByName(name);
		}

		private static float CalculateAccuracy(Texture2D canvas, Sprite reference)
		{
			if (canvas == null || reference == null) return -1f;
			try
			{
				var rect = reference.textureRect;
				Color[] refPixels = reference.texture.GetPixels(
					(int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
				Color[] canvasPixels = canvas.GetPixels();

				int cw = canvas.width, ch = canvas.height;
				int rw = (int)rect.width, rh = (int)rect.height;
				int hit = 0, total = 0;

				for (int cy = 0; cy < ch; cy++)
				{
					for (int cx = 0; cx < cw; cx++)
					{
						int rx = Mathf.Clamp(Mathf.RoundToInt((float)cx / (cw - 1) * (rw - 1)), 0, rw - 1);
						int ry = Mathf.Clamp(Mathf.RoundToInt((float)cy / (ch - 1) * (rh - 1)), 0, rh - 1);

						Color rp = refPixels[ry * rw + rx];
						Color cp = canvasPixels[cy * cw + cx];

						bool refIsInk = rp.a > 0.5f && rp.grayscale < 0.9f;
						bool canvasIsInk = cp.grayscale < 0.9f;

						if (refIsInk) total++;
						if (refIsInk && canvasIsInk) hit++;
					}
				}
				return total > 0 ? (float)hit / total : 1f;
			}
			catch { return -1f; }
		}
	}
}