using UnityEngine;
using UnityEngine.UI;

namespace Printer
{
    [RequireComponent(typeof(PlayerPrinter))]
    public class PrintheadController : MonoBehaviour
    {
        [Header("Prefab References — assign in Inspector")] [SerializeField]
        private RectTransform indicatorLine;

        [SerializeField] private RectTransform printheadMarker;
        [SerializeField] private PrintCanvas canvas;

        public float NormalisedX { get; private set; } = 0.5f;

        private RectTransform CanvasRect => canvas.DisplayRect;

        // ── Public API ─────────────────────────────────────────────────────────

        public void SetNormalisedX(float t)
        {
            NormalisedX = Mathf.Clamp01(t);
            if (printheadMarker == null) return;

            float localX = Mathf.Lerp(-CanvasRect.rect.width * 0.5f, CanvasRect.rect.width * 0.5f, NormalisedX);
            printheadMarker.anchoredPosition = new Vector2(localX, printheadMarker.anchoredPosition.y);
        }

        public void SetIndicatorLine(int lineIndex, int totalLines, int linePixelHeight)
        {
            float localY = ComputeLocalY(lineIndex, totalLines, linePixelHeight);

            if (indicatorLine != null)
            {
                // Stretch to canvas width
                indicatorLine.sizeDelta = new Vector2(CanvasRect.rect.width, indicatorLine.sizeDelta.y);
                indicatorLine.anchoredPosition = new Vector2(0f, localY);
            }

            if (printheadMarker != null)
                printheadMarker.anchoredPosition = new Vector2(printheadMarker.anchoredPosition.x, localY);
        }

        public void SetVisible(bool visible)
        {
            if (indicatorLine != null) indicatorLine.gameObject.SetActive(visible);
            if (printheadMarker != null) printheadMarker.gameObject.SetActive(visible);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private float ComputeLocalY(int lineIndex, int totalLines, int linePixelHeight)
        {
            float canvasH = CanvasRect.rect.height;
            float normalisedY = totalLines > 0
                ? 1f - (float)(lineIndex * linePixelHeight) / (totalLines * linePixelHeight)
                : 1f;
            return Mathf.Lerp(-canvasH * 0.5f, canvasH * 0.5f, normalisedY);
        }
    }
}