using ImageHashLib;
using UnityEngine;

namespace Printer
{
	public class PrintState
	{
		private static readonly Color32 White = new Color32(255, 255, 255, 255);
		private const float WhiteThreshold = 0.1f;

		public static double GetSimilarityScore()
		{
			if (GameMgr.Instance.PrinterReferenceWC == null ||
			    GameMgr.Instance.PrinterViewWC == null ||
			    GameMgr.Instance.PrinterReferenceWC.pReference.ReferenceImage == null ||
			    GameMgr.Instance.PrinterReferenceWC.pReference.ReferenceImage.sprite == null)
				return 0;
			Texture2D reference = GameMgr.Instance.PrinterReferenceWC.pReference.ReferenceImage.sprite.texture;
			Texture2D canvas = GameMgr.Instance.PrinterViewWC.pCanvas.DO_NOT_MODIFY_CanvasInternalTexture;

			// Blit reference sprite rect to a readable texture
			var sprite = GameMgr.Instance.PrinterReferenceWC.pReference.ReferenceImage.sprite;
			var rect = sprite.textureRect;
			var rt = RenderTexture.GetTemporary((int)rect.width, (int)rect.height);
			Graphics.Blit(reference, rt);

			var prev = RenderTexture.active;
			RenderTexture.active = rt;

			var readableRef = new Texture2D((int)rect.width, (int)rect.height);
			readableRef.ReadPixels(new Rect(rect.x, rect.y, rect.width, rect.height), 0, 0);
			readableRef.Apply();

			RenderTexture.active = prev;
			RenderTexture.ReleaseTemporary(rt);

			double sim = GetSimilarity(canvas, readableRef);
            
			Object.Destroy(readableRef);

			return sim;
		}
		
		public static double GetSimilarity(Texture2D user, Texture2D reference)
		{
			var croppedUser = CropToContent(user);
			var croppedRef = CropToContent(reference);

			// DHash captures stroke gradients well, PHash captures structure
			var dHashSim = HashFunctions.DHash(croppedUser).Similarity(HashFunctions.DHash(croppedRef));
			var pHashSim = HashFunctions.PHash(croppedUser).Similarity(HashFunctions.PHash(croppedRef));

			// blend — weight pHash higher for structural similarity
			return dHashSim * 0.4 + pHashSim * 0.6;
		}

		private static Texture2D CropToContent(Texture2D tex)
		{
			var pixels = tex.GetPixels32();
			int w = tex.width, h = tex.height;

			int minX = w, maxX = 0, minY = h, maxY = 0;

			for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				var p = pixels[y * w + x];
				if (IsBackground(p)) continue;
				if (x < minX) minX = x;
				if (x > maxX) maxX = x;
				if (y < minY) minY = y;
				if (y > maxY) maxY = y;
			}

			// no content found — return original
			if (minX > maxX || minY > maxY) return tex;

			int cw = maxX - minX + 1;
			int ch = maxY - minY + 1;

			var croppedPixels = tex.GetPixels(minX, minY, cw, ch);
			var cropped = new Texture2D(cw, ch, tex.format, false);
			cropped.SetPixels(croppedPixels);
			cropped.Apply();
			return cropped;
		}

		private static bool IsBackground(Color32 p)
		{
			float r = p.r / 255f, g = p.g / 255f, b = p.b / 255f;
			float dist = Mathf.Sqrt(
				(r - 1f) * (r - 1f) +
				(g - 1f) * (g - 1f) +
				(b - 1f) * (b - 1f));
			return dist < WhiteThreshold;
		}
	}
}