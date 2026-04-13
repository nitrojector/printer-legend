using UnityEngine;
using UnityEngine.UI;

namespace Printer
{
    /// <summary>
    /// Owns the Texture2D pixel buffer and the RenderTexture displayed via RawImage.
    /// All draw calls go through here. Extensible: add disruption/overlay methods here.
    /// </summary>
    public class PrintCanvas : MonoBehaviour
    {
        [Header("Canvas Settings")] [SerializeField]
        private int canvasWidth = 512;
        [SerializeField] private int canvasHeight = 512;

        [Header("Display")] [SerializeField]
        private Color backgroundColor = Color.white;
        
        private RawImage displayTarget;

        public RectTransform DisplayRect => displayTarget.rectTransform;

        private Texture2D texture;
        private RenderTexture renderTexture;
        private bool dirty;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (displayTarget == null)
                displayTarget = GetComponent<RawImage>();

            if (displayTarget == null)
            {
                Debug.LogError("PrintCanvas requires a RawImage display target.", this);
                enabled = false;
                return;
            }

            texture = new Texture2D(canvasWidth, canvasHeight, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            renderTexture = new RenderTexture(canvasWidth, canvasHeight, depth: 0)
            {
                filterMode = FilterMode.Point
            };

            displayTarget.texture = renderTexture;
            Clear();
        }

        private void LateUpdate()
        {
            // Flush dirty CPU texture → GPU RenderTexture once per frame
            if (!dirty) return;
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            Graphics.Blit(texture, renderTexture);
            dirty = false;
        }

        private void OnDestroy()
        {
            Destroy(texture);
            renderTexture.Release();
            Destroy(renderTexture);
        }

        // ── Public Draw API ────────────────────────────────────────────────────

        /// <summary>Paints a single pixel. Coordinates are canvas-space (0,0 = bottom-left).</summary>
        public void DrawPixel(int x, int y, Color color)
        {
            if (x < 0 || x >= canvasWidth || y < 0 || y >= canvasHeight) return;
            texture.SetPixel(x, y, color);
            dirty = true;
        }

        /// <summary>Paints a horizontal run of pixels for one print interval.</summary>
        public void DrawInterval(PrintInterval interval, int lineY)
        {
            int width = Mathf.Max(1, interval.PixelWidth);
            int startX = interval.CanvasX;
            for (int dx = 0; dx < width; dx++)
                DrawPixel(startX + dx, lineY, interval.Color);
        }

        /// <summary>Clears the entire canvas to the background color.</summary>
        public void Clear()
        {
            Color32[] fill = new Color32[canvasWidth * canvasHeight];
            Color32 bg = backgroundColor;
            for (int i = 0; i < fill.Length; i++) fill[i] = bg;
            texture.SetPixels32(fill);
            dirty = true;
        }

        // ── Accessors ──────────────────────────────────────────────────────────

        public int CanvasWidth => canvasWidth;
        public int CanvasHeight => canvasHeight;

        /// <summary>
        /// Converts a normalised [0,1] horizontal position to a canvas pixel X.
        /// Useful for mapping printhead UI position → canvas coordinates.
        /// </summary>
        public int NormalisedToCanvasX(float t) => Mathf.RoundToInt(t * (canvasWidth - 1));

        /// <summary>Converts a line index to a canvas Y pixel (lines grow downward in UI, upward in texture).</summary>
        public int LineIndexToCanvasY(int lineIndex, int linePixelHeight = 1)
            => canvasHeight - 1 - lineIndex * linePixelHeight;
    }
}