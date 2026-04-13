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

        [SerializeField] private int linePixelWidth = 4;

        [SerializeField] private Color inkColor = Color.black;

        private int totalLines;

        private PrintLineState lineState;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            totalLines = canvas.CanvasHeight / linePixelHeight;
            lineState = new PrintLineState(totalLines, linePixelHeight);
        }

        private void Start()
        {
            // Sync visual to initial line
            printhead.BindLineState(lineState, totalLines, linePixelHeight);
            printhead.SetIndicatorLine(lineState.CurrentLine, totalLines, linePixelHeight);
            printhead.SetCanvasX(0);
        }

        // ── Public API — called by input handlers, game systems ────────────────

        /// <summary>
        /// Registers a print action at the printhead's current horizontal position.
        /// Call this when the player triggers a print (e.g. button press, timing window).
        /// </summary>
        public void Print()
        {
            if (lineState.IsComplete) return;

            int canvasX = canvas.NormalisedToCanvasX(printhead.NormalisedX);
            var interval = new PrintInterval(canvasX, inkColor, linePixelWidth);
            printhead.CommitInterval(interval);
        }

        /// <summary>
        /// Advances to the next line after any immediate prints have already been committed.
        /// Typically called by the game's line-advance logic (timer, beat, etc.).
        /// </summary>
        public void AdvanceLine()
        {
            if (lineState.IsComplete) return;

            printhead.AdvanceLine();
        }

        /// <summary>
        /// Advances the printhead horizontally by one print step and wraps to the next
        /// line when it reaches the end of the canvas.
        /// </summary>
        public void AdvancePrinthead()
        {
            if (lineState.IsComplete) return;

            bool wrapped = printhead.AdvanceHorizontal(linePixelWidth);
            if (wrapped)
                AdvanceLine();
        }

        /// <summary>Moves the printhead. Input systems drive this each frame.</summary>
        public void SetPrintheadPosition(float normalisedX)
        {
            printhead.SetPrintheadPosition(normalisedX);
        }

        public void SetPrintheadLine(int lineIndex) => printhead.SetPrintheadLine(lineIndex);

        // ── Accessors ──────────────────────────────────────────────────────────

        public float Progress => lineState.Progress;
        public bool IsComplete => lineState.IsComplete;
        public int CurrentLine => lineState.CurrentLine;
        public int LinePixelWidth => linePixelWidth;
        public int LinePixelHeight => linePixelHeight;

        public Color InkColor
        {
            get => inkColor;
            set => inkColor = value; // abilities can swap ink color
        }
    }
}