using System;
using System.Collections;
using AudioSystem;
using UnityEngine;
using Utility;

namespace Printer
{
    public class PrintheadController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform printheadRoot;
        [SerializeField] public RectTransform printheadMarker;

        /// <summary>
        /// Reference to the PrintCanvas component where this printhead should draw.
        /// </summary>
        private PrintCanvas canvas;
        
        public PrintCanvas Canvas => canvas;

        [Header("Print Settings")]
        [SerializeField] private int linePixelHeight = 4;
        [SerializeField] private int linePixelWidth = 4;
        [SerializeField] private Color inkColor = Color.black;

        private PrintLineState lineState;
        private int totalLines;
        private int canvasX = 0;

        public float NormalisedX { get; private set; } = 0.5f;

        /// <summary>
        /// When true the printhead advances right-to-left instead of left-to-right.
        /// Set by <see cref="PrinterMagic"/> during a Motor Malfunction obstacle.
        /// </summary>
        public bool IsRightToLeft { get; set; }

        /// <summary>Fires after every successful line advance, including wrap-arounds from AdvancePrinthead.</summary>
        public event Action OnLineAdvanced;

        private RectTransform CanvasRect => canvas.DisplayRect;
        
        // ── Audio ──────────────────────────────────────────────────────────────

        private AudioClip sfxInking;
        private AudioClip sfxLine;
        private AudioClip sfxNewLine;

        private AudioCategory newlineSfxCategory;
        private AudioHandle inkLoopHandle;
        private bool        inkLoopActive;   // true once PlayManaged has succeeded
        private bool        printedThisFrame;

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
            sfxInking = Resources.Load<AudioClip>("Audio/print_ink");
            sfxLine = Resources.Load<AudioClip>("Audio/print_line");
            sfxNewLine = Resources.Load<AudioClip>("Audio/print_cr");
            canvas     = GetComponentInParent<PrintCanvas>();
            totalLines = canvas.CanvasHeight / linePixelHeight;
            lineState  = new PrintLineState(totalLines, linePixelHeight);

            newlineSfxCategory = AudioManager.Instance.CreateCategory(new AudioCategoryConfig()
            {
                MaxVoices = 1,
                MinInterval = 0.35f,
                Overflow = AudioOverflowMode.StopOldest
            });
        }

        // Coroutine so the Canvas layout pass has run before we read rect dimensions.
        private IEnumerator Start()
        {
            GameManager.Instance.Reference?.Init(canvas.CanvasHeight, linePixelHeight);
            yield return null;
            RefreshLayout();
        }

        /// <summary>
        /// Snaps the marker width to one print pixel and repositions the indicator line.
        /// Call after any runtime canvas resize.
        /// </summary>
        public void RefreshLayout()
        {
            SetMarkerSize();
            GameManager.Instance.Reference?.RefreshLayout();
            SetIndicatorLine(lineState.CurrentLine, totalLines, linePixelHeight);
            SetCanvasX(canvasX);
        }

        private void SetMarkerSize()
        {
            if (printheadMarker == null || canvas == null) return;
            float pixelsToUnits = CanvasRect.rect.width / canvas.CanvasWidth * linePixelWidth;
            var sd = printheadMarker.sizeDelta;
            printheadMarker.sizeDelta = new Vector2(pixelsToUnits, pixelsToUnits);
        }

        // ── Print Actions ──────────────────────────────────────────────────────

        public void Print()
        {
            if (lineState.IsComplete) return;

            int x = canvas.NormalisedToCanvasX(NormalisedX);
            CommitInterval(new PrintInterval(x, inkColor, linePixelWidth));

            printedThisFrame = true;
            EnsureInkLoopPlaying();
        }

        public void AdvanceLine()
        {
            
            if (lineState.IsComplete) return;

            bool advanced = lineState.AdvanceLine();
            if (advanced)
            {
                SetIndicatorLine(lineState.CurrentLine, totalLines, linePixelHeight);
                SetCanvasX(0);
                OnLineAdvanced?.Invoke();
                AudioManager.Instance.Play(sfxLine, transform, AudioBus.SFX, newlineSfxCategory);
            }
        }

        public void AdvancePrinthead()
        {
            if (lineState.IsComplete) return;

            bool wrapped = IsRightToLeft
                ? RetreatHorizontal(linePixelWidth)
                : AdvanceHorizontal(linePixelWidth);
            if (wrapped) AdvanceLine();
        }

        /// <summary>Moves the printhead to the start of the current line without advancing it (Carriage Return).</summary>
        public void CarriageReturn()
        {
            SetCanvasX(0);
        }

        /// <summary>
        /// Moves the printhead back by <paramref name="lineCount"/> lines and clears those
        /// lines on the canvas so the player can reprint them cleanly.
        /// Returns false if already at the first line.
        /// </summary>
        public bool RewindLines(int lineCount)
        {
            int targetLine  = Mathf.Max(0, lineState.CurrentLine - lineCount);
            int rewindCount = lineState.CurrentLine - targetLine;
            if (rewindCount == 0) return false;

            canvas.ClearLineRange(targetLine, rewindCount, linePixelHeight);
            lineState.SetCurrentLine(targetLine);
            SetIndicatorLine(targetLine, totalLines, linePixelHeight);
            SetCanvasX(0);
            return true;
        }

        public void ResetCanvasAndPrinthead()
        {
            Canvas.Clear();
            SetPrintheadLine(0);
            SetPrintheadPosition(0);
            AudioManager.Instance.StopAllInCategory(newlineSfxCategory);
        }
        
        // ── Audio helpers ──────────────────────────────────────────────────────

        private void LateUpdate()
        {
            if (!printedThisFrame)
                PauseInkLoop();
            printedThisFrame = false;
        }

        private void OnDestroy()
        {
            if (inkLoopActive)
                AudioManager.Instance.Stop(inkLoopHandle);
        }

        private void EnsureInkLoopPlaying()
        {
            var am = AudioManager.Instance;
            if (!inkLoopActive)
            {
                inkLoopActive = am.PlayManaged(sfxInking, transform, AudioBus.SFX,
                    out inkLoopHandle, loop: true);
                return;
            }
            // Resume covers both the already-playing and the paused cases cleanly
            am.Resume(inkLoopHandle);
        }

        private void PauseInkLoop()
        {
            if (inkLoopActive)
                AudioManager.Instance.Pause(inkLoopHandle);
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

        private bool RetreatHorizontal(int stepPixels)
        {
            if (canvas == null) return false;

            int nextCanvasX = canvasX - Mathf.Max(0, stepPixels);
            if (nextCanvasX < 0)
            {
                SetCanvasX(canvas.CanvasWidth - 1);
                return true;
            }

            SetCanvasX(nextCanvasX);
            return false;
        }

        private void SetIndicatorLine(int lineIndex, int totalLineCount, int lineHeightPixels)
        {
            float localY = ComputeLocalY(lineIndex + 1, totalLineCount, lineHeightPixels);

            if (printheadRoot != null)
                printheadRoot.anchoredPosition = new Vector2(0f, localY);

            GameManager.Instance.Reference?.SetIndicatorLine(lineIndex);
        }

        public void SetPrintheadLine(int lineIndex)
        {
            lineState?.SetCurrentLine(lineIndex);
            SetIndicatorLine(lineIndex, totalLines, linePixelHeight);
            SetCanvasX(0);
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
            return Mathf.LerpUnclamped(-canvasH * 0.5f, canvasH * 0.5f, normalisedY);
        }
    }
}
