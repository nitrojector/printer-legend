using UnityEngine;
using UnityEngine.UI;

namespace Printer
{
    /// <summary>
    /// Owns the Texture2D pixel buffer and the RenderTexture displayed via RawImage.
    /// All draw calls go through here. Extensible: add disruption/overlay methods here.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class PrintCanvas : MonoBehaviour
    {
        [Header("Canvas Settings")] [SerializeField]
        private int canvasWidth = 512;
        
        private RawImage displayTarget;

        [SerializeField] private int canvasHeight = 512;
        [SerializeField] private Color backgroundColor = Color.white;

        public RectTransform DisplayRect => displayTarget.rectTransform;

        private Texture2D _texture;
        private RenderTexture _renderTexture;
        private bool _dirty;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _texture = new Texture2D(canvasWidth, canvasHeight, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            _renderTexture = new RenderTexture(canvasWidth, canvasHeight, depth: 0)
            {
                filterMode = FilterMode.Point
            };

            displayTarget.texture = _renderTexture;
            Clear();
        }

        private void LateUpdate()
        {
            // Flush dirty CPU texture → GPU RenderTexture once per frame
            if (!_dirty) return;
            Graphics.Blit(_texture, _renderTexture);
            _dirty = false;
        }

        private void OnDestroy()
        {
            Destroy(_texture);
            _renderTexture.Release();
            Destroy(_renderTexture);
        }

        // ── Public Draw API ────────────────────────────────────────────────────

        /// <summary>Paints a single pixel. Coordinates are canvas-space (0,0 = bottom-left).</summary>
        public void DrawPixel(int x, int y, Color color)
        {
            if (x < 0 || x >= canvasWidth || y < 0 || y >= canvasHeight) return;
            _texture.SetPixel(x, y, color);
            _dirty = true;
        }

        /// <summary>Paints a horizontal run of pixels for one print interval.</summary>
        public void DrawInterval(PrintInterval interval, int lineY)
        {
            int halfW = interval.PixelWidth / 2;
            for (int dx = -halfW; dx <= halfW; dx++)
                DrawPixel(interval.CanvasX + dx, lineY, interval.Color);
        }

        /// <summary>Clears the entire canvas to the background color.</summary>
        public void Clear()
        {
            Color32[] fill = new Color32[canvasWidth * canvasHeight];
            Color32 bg = backgroundColor;
            for (int i = 0; i < fill.Length; i++) fill[i] = bg;
            _texture.SetPixels32(fill);
            _dirty = true;
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