using UnityEngine;

namespace Printer
{
    public class GameState
    {
        public static double GetRawSimilarity()
        {
            if (GameManager.Instance.Reference == null ||
                GameManager.Instance.Canvas == null)
                return 0;
            Texture2D reference = GameManager.Instance.Reference.ReferenceImage.sprite.texture;
            Texture2D canvas = GameManager.Instance.Canvas.DO_NOT_MODIFY_CanvasInternalTexture;

            // Blit reference sprite rect to a readable texture
            var sprite = GameManager.Instance.Reference.ReferenceImage.sprite;
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

            var refHash = ImageHashLib.HashFunctions.PHash(readableRef);
            var canvasHash = ImageHashLib.HashFunctions.PHash(canvas);
            
            Object.Destroy(readableRef);

            return refHash.Similarity(canvasHash);
        }

    }
}