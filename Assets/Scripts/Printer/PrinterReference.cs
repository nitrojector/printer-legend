using System;
using UnityEngine;
using UnityEngine.InputSystem.XInput;
using UnityEngine.UI;

namespace Printer
{
    /// <summary>
    /// Manages the scan-line indicator overlaid on the reference image.
    /// The indicator is a full-width horizontal band that tracks whichever
    /// print line the player is currently drawing.
    ///
    /// Call <see cref="Init"/> from PrintheadController.Awake (before layout),
    /// <see cref="RefreshLayout"/> after the Canvas layout pass, and
    /// <see cref="SetIndicatorLine"/> whenever the active line changes.
    /// </summary>
    public class PrinterReference : MonoBehaviour
    {
        [SerializeField] private RectTransform indicator;
        
        /// <summary>
        /// Reference to the image that is used as reference
        /// </summary>
        public Image ReferenceImage { get; private set; }
        
        /// <summary>
        /// Convenience accessor for the sprite assigned to the reference image.
        /// </summary>
        public Sprite ReferenceSprite => ReferenceImage.sprite;

        private RectTransform containerRect;
        private int imageSizePixels;
        private int printPixelSize;
        private float indicatorHeight;

        private void Awake()
        {
            containerRect = GetComponent<RectTransform>();
            ReferenceImage = GetComponent<Image>();
            
            GameManager.Instance.RegisterReference(this);
        }

        private void Start()
        {
            LoadRandomReference();
        }

        private void OnDestroy()
        {
            GameManager.Instance.UnregisterReference(this);
        }

        // ── Setup ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Stores the pixel ratio used to derive the indicator height.
        /// Safe to call before layout resolves — no rect dimensions are read here.
        /// </summary>
        /// <param name="size">Total canvas height in pixels (e.g. canvasHeight).</param>
        /// <param name="printPixels">Height of one print line in pixels (e.g. linePixelHeight).</param>
        public void Init(int size, int printPixels)
        {
            imageSizePixels = size;
            printPixelSize = printPixels;
        }

        /// <summary>
        /// Recomputes indicator height from the stored pixel ratio and the current
        /// container dimensions.  Call once after the Canvas layout pass has run.
        /// </summary>
        public void RefreshLayout()
        {
            ApplyIndicatorHeight();
        }

        // ── Runtime ───────────────────────────────────────────────────────────
        
        public void LoadReference(Sprite sprite)
        {
            ReferenceImage.sprite = sprite;
        }

        public void LoadRandomReference()
        {
            ReferenceImage.sprite = PrintRefManager.Instance.GetRandom();
        }

        /// <summary>
        /// Moves the indicator to cover the print line at <paramref name="lineIndex"/>.
        /// </summary>
        public void SetIndicatorLine(int lineIndex)
        {
            if (indicator == null) return;
            indicator.anchoredPosition = new Vector2(0f, -indicatorHeight * lineIndex);
        }

        // ── Internals ─────────────────────────────────────────────────────────

        private void ApplyIndicatorHeight()
        {
            indicatorHeight = printPixelSize / (float)imageSizePixels * containerRect.rect.height;
            Vector2 size = indicator.sizeDelta;
            size.y = indicatorHeight;
            indicator.sizeDelta = size;
        }
    }
}
