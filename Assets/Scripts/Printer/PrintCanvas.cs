using System.Collections.Generic;
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

        public Texture2D DO_NOT_MODIFY_CanvasInternalTexture => texture;

        private Texture2D texture;
        private RenderTexture renderTexture;
        private bool dirty;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            GameManager.RegisterCanvas(this);
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
            GameManager.UnregisterCanvas(this);
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

        // ── Disruption / Repair ────────────────────────────────────────────────

        /// <summary>
        /// Erases a contiguous block of lines back to the background color.
        /// Call before rewinding the printhead so the player can redo clean lines.
        /// </summary>
        public void ClearLineRange(int firstLine, int lineCount, int linePixelHeight)
        {
            for (int li = 0; li < lineCount; li++)
            {
                int topY = LineIndexToCanvasY(firstLine + li, linePixelHeight);
                for (int dy = 0; dy < linePixelHeight; dy++)
                {
                    int y = topY - dy;
                    if (y < 0 || y >= canvasHeight) continue;
                    for (int x = 0; x < canvasWidth; x++)
                        texture.SetPixel(x, y, backgroundColor);
                }
            }
            dirty = true;
        }

        /// <summary>
        /// Displaces a random subset of ink pixels within a line band to simulate a
        /// paper-jam skew. When <paramref name="respectPrintSize"/> is true, horizontal
        /// displacement is snapped to the print-size grid; otherwise it is free-form.
        /// </summary>
        public void ShuffleLinePixels(int lineIndex, int linePixelHeight,
            int shuffleCount, bool respectPrintSize, int printSize)
        {
            int topY    = LineIndexToCanvasY(lineIndex, linePixelHeight);
            int startY  = Mathf.Max(0, topY - linePixelHeight + 1);
            int bandH   = topY - startY + 1;
            if (bandH <= 0) return;

            // Batch-read the band for performance
            Color[] band = texture.GetPixels(0, startY, canvasWidth, bandH);
            Color32 bg   = backgroundColor;

            // Collect indices of non-background pixels
            var inkIdx = new List<int>(band.Length);
            for (int i = 0; i < band.Length; i++)
            {
                Color32 c = band[i];
                if (c.r != bg.r || c.g != bg.g || c.b != bg.b || c.a != bg.a)
                    inkIdx.Add(i);
            }
            if (inkIdx.Count == 0) return;

            // Partial Fisher-Yates to pick a random subset without repetition
            int toMove = Mathf.Min(shuffleCount, inkIdx.Count);
            for (int i = 0; i < toMove; i++)
            {
                int j = Random.Range(i, inkIdx.Count);
                (inkIdx[i], inkIdx[j]) = (inkIdx[j], inkIdx[i]);
            }

            int spread = Mathf.Max(printSize * 3, canvasWidth / 8);
            for (int i = 0; i < toMove; i++)
            {
                int src  = inkIdx[i];
                int srcX = src % canvasWidth;
                int srcY = src / canvasWidth;

                int dx = Random.Range(-spread, spread + 1);
                if (respectPrintSize && printSize > 1)
                    dx = Mathf.RoundToInt((float)dx / printSize) * printSize;

                int dstX = Mathf.Clamp(srcX + dx, 0, canvasWidth - 1);
                int dy   = Random.Range(-(bandH - 1), bandH);
                int dstY = Mathf.Clamp(srcY + dy, 0, bandH - 1);
                int dst  = dstY * canvasWidth + dstX;

                (band[src], band[dst]) = (band[dst], band[src]);
            }

            texture.SetPixels(0, startY, canvasWidth, bandH, band);
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