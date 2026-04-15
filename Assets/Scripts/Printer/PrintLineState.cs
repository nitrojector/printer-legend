using System.Collections.Generic;
using UnityEngine;

namespace Printer
{
    /// <summary>
    /// Tracks which print line is active and stores any queued intervals for that line.
    /// Stateless between lines — caller is responsible for advancing via AdvanceLine().
    /// </summary>
    public class PrintLineState
    {
        private readonly int totalLines;
        private readonly int linePixelHeight;

        private int currentLine;
        private readonly List<PrintInterval> pendingIntervals = new();

        public int CurrentLine => currentLine;
        public int TotalLines => totalLines;
        public int LinePixelHeight => linePixelHeight;
        public bool IsComplete => currentLine >= totalLines;

        public IReadOnlyList<PrintInterval> PendingIntervals => pendingIntervals;

        public PrintLineState(int totalLines, int linePixelHeight = 4)
        {
            this.totalLines = totalLines;
            this.linePixelHeight = linePixelHeight;
            currentLine = 0;
        }

        /// <summary>Queues an interval to be drawn on the current line.</summary>
        public void QueueInterval(PrintInterval interval) => pendingIntervals.Add(interval);

        /// <summary>
        /// Commits a single interval to the current line immediately.
        /// This is used when ink should appear as soon as the player presses print.
        /// </summary>
        public void CommitInterval(PrintCanvas canvas, PrintInterval interval)
        {
            if (IsComplete) return;

            int y = canvas.LineIndexToCanvasY(currentLine, linePixelHeight);
            for (int dy = 0; dy < linePixelHeight; dy++)
                canvas.DrawInterval(interval, y - dy);
        }

        /// <summary>Advances to the next logical line and clears any queued intervals.</summary>
        public bool AdvanceLine()
        {
            if (IsComplete) return false;

            pendingIntervals.Clear();
            currentLine++;
            return true;
        }

        /// <summary>Sets the active logical line directly and clears queued intervals.</summary>
        public void SetCurrentLine(int lineIndex)
        {
            pendingIntervals.Clear();
            currentLine = Mathf.Clamp(lineIndex, 0, totalLines);
        }

        /// <summary>
        /// Commits all pending intervals to the canvas then clears the queue
        /// and advances to the next line. Returns false if already on the last line.
        /// </summary>
        public bool CommitAndAdvance(PrintCanvas canvas)
        {
            if (IsComplete) return false;

            foreach (var interval in pendingIntervals)
                CommitInterval(canvas, interval);

            return AdvanceLine();
        }

        /// <summary>Returns [0,1] normalised progress through all lines.</summary>
        public float Progress => totalLines > 0 ? (float)currentLine / totalLines : 0f;
    }
}