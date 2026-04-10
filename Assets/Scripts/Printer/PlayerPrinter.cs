using UnityEngine;

namespace Printer
{
    /// <summary>
    /// Entry point for player-driven print actions.
    /// Composes PrintheadController, PrintCanvas, and PrintLineState.
    /// Game systems (input, scoring, abilities) should call into this — not reach
    /// directly into the canvas or line state.
    /// </summary>
    public class PlayerPrinter : MonoBehaviour
    {
        [Header("Components — assign in Inspector")] [SerializeField]
        private PrintCanvas canvas;

        [SerializeField] private PrintheadController printhead;

        [Header("Print Settings")] [SerializeField]
        private int linePixelHeight = 4;

        [SerializeField] private Color inkColor = Color.black;
        [SerializeField] private int inkPixelWidth = 4;

        private int totalLines;

        private PrintLineState _lineState;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            int totalLines = canvas.CanvasHeight / linePixelHeight;
            _lineState = new PrintLineState(totalLines, linePixelHeight);
        }

        private void Start()
        {
            // Sync visual to initial line
            printhead.SetIndicatorLine(_lineState.CurrentLine, totalLines, linePixelHeight);
        }

        // ── Public API — called by input handlers, game systems ────────────────

        /// <summary>
        /// Registers a print action at the printhead's current horizontal position.
        /// Call this when the player triggers a print (e.g. button press, timing window).
        /// </summary>
        public void Print()
        {
            if (_lineState.IsComplete) return;

            int canvasX = canvas.NormalisedToCanvasX(printhead.NormalisedX);
            var interval = new PrintInterval(canvasX, inkColor, inkPixelWidth);
            _lineState.QueueInterval(interval);
        }

        /// <summary>
        /// Commits all queued intervals and advances to the next line.
        /// Typically called by the game's line-advance logic (timer, beat, etc.).
        /// </summary>
        public void AdvanceLine()
        {
            if (_lineState.IsComplete) return;

            _lineState.CommitAndAdvance(canvas);
            printhead.SetIndicatorLine(_lineState.CurrentLine, totalLines, linePixelHeight);
        }

        /// <summary>Moves the printhead. Input systems drive this each frame.</summary>
        public void SetPrintheadPosition(float normalisedX)
        {
            printhead.SetNormalisedX(normalisedX);
        }

        // ── Accessors ──────────────────────────────────────────────────────────

        public float Progress => _lineState.Progress;
        public bool IsComplete => _lineState.IsComplete;
        public int CurrentLine => _lineState.CurrentLine;

        public Color InkColor
        {
            get => inkColor;
            set => inkColor = value; // abilities can swap ink color
        }
    }
}