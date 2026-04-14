using UnityEngine;

namespace Printer
{
    public class PrintheadController : MonoBehaviour
    {
        [Header("Prefab References — assign in Inspector")]
        [SerializeField] private RectTransform printheadRoot;
        [SerializeField] private RectTransform printheadMarker;
        [SerializeField] private PrintCanvas canvas;

        [Header("Print Settings")]
        [SerializeField] private int linePixelHeight = 4;
        [SerializeField] private int linePixelWidth = 4;
        [SerializeField] private Color inkColor = Color.black;

        private PrintLineState lineState;
        private int totalLines;
        private int canvasX;

        public float NormalisedX { get; private set; } = 0.5f;

        private RectTransform CanvasRect => canvas.DisplayRect;

        // ── Accessors ──────────────────────────────────────────────────────────

        public float Progress => lineState.Progress;
        public bool IsComplete => lineState.IsComplete;
        public int CurrentLine => lineState.CurrentLine;
        public int LinePixelWidth => linePixelWidth;
        public int LinePixelHeight => linePixelHeight;

        public Color InkColor
        {
            get => inkColor;
            set => inkColor = value;
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            totalLines = canvas.CanvasHeight / linePixelHeight;
            lineState = new PrintLineState(totalLines, linePixelHeight);
        }

        private void Start()
        {
            SetIndicatorLine(lineState.CurrentLine, totalLines, linePixelHeight);
            SetCanvasX(0);
        }

        // ── Print Actions ──────────────────────────────────────────────────────

        public void Print()
        {
            if (lineState.IsComplete) return;

            int x = canvas.NormalisedToCanvasX(NormalisedX);
            CommitInterval(new PrintInterval(x, inkColor, linePixelWidth));
        }

        public void AdvanceLine()
        {
            if (lineState.IsComplete) return;

            bool advanced = lineState.AdvanceLine();
            if (advanced)
                SetIndicatorLine(lineState.CurrentLine, totalLines, linePixelHeight);
        }

        public void AdvancePrinthead()
        {
            if (lineState.IsComplete) return;

            bool wrapped = AdvanceHorizontal(linePixelWidth);
            if (wrapped) AdvanceLine();
        }

        // ── Printhead Position ─────────────────────────────────────────────────

        public void SetNormalisedX(float t)
        {
            NormalisedX = Mathf.Clamp01(t);
            if (canvas == null) return;

            int targetCanvasX = canvas.CanvasWidth > 1
                ? Mathf.RoundToInt(NormalisedX * (canvas.CanvasWidth - 1))
                : 0;
            SetCanvasX(targetCanvasX);
        }

        public void SetPrintheadPosition(float t) => SetNormalisedX(t);

        public void SetCanvasX(int targetCanvasX)
        {
            if (canvas == null) return;

            canvasX = Mathf.Clamp(targetCanvasX, 0, Mathf.Max(0, canvas.CanvasWidth - 1));
            NormalisedX = canvas.CanvasWidth > 1 ? (float)canvasX / (canvas.CanvasWidth - 1) : 0f;

            if (printheadMarker == null) return;

            float localX = Mathf.Lerp(-CanvasRect.rect.width * 0.5f, CanvasRect.rect.width * 0.5f, NormalisedX);
            printheadMarker.anchoredPosition = new Vector2(localX, 0f);
        }

        public bool AdvanceHorizontal(int stepPixels)
        {
            if (canvas == null) return false;

            int nextCanvasX = canvasX + Mathf.Max(0, stepPixels);
            if (nextCanvasX >= canvas.CanvasWidth)
            {
                SetCanvasX(0);
                return true;
            }

            SetCanvasX(nextCanvasX);
            return false;
        }

        public void SetIndicatorLine(int lineIndex, int totalLineCount, int lineHeightPixels)
        {
            float localY = ComputeLocalY(lineIndex, totalLineCount, lineHeightPixels);

            if (printheadRoot != null)
                printheadRoot.anchoredPosition = new Vector2(0f, localY);

            SetCanvasX(0);
        }

        public void SetPrintheadLine(int lineIndex)
        {
            lineState?.SetCurrentLine(lineIndex);
            SetIndicatorLine(lineIndex, totalLines, linePixelHeight);
        }

        public void CommitInterval(PrintInterval interval)
        {
            if (lineState == null || canvas == null) return;
            lineState.CommitInterval(canvas, interval);
        }

        public void SetVisible(bool visible)
        {
            if (printheadRoot != null) printheadRoot.gameObject.SetActive(visible);
            if (printheadMarker != null) printheadMarker.gameObject.SetActive(visible);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private float ComputeLocalY(int lineIndex, int totalLineCount, int lineHeightPixels)
        {
            float canvasH = CanvasRect.rect.height;
            float normalisedY = totalLineCount > 0
                ? 1f - (float)(lineIndex * lineHeightPixels) / (totalLineCount * lineHeightPixels)
                : 1f;
            return Mathf.Lerp(-canvasH * 0.5f, canvasH * 0.5f, normalisedY);
        }
    }
}
