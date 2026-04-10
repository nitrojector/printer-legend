using System.Collections.Generic;
using UnityEngine;

namespace Printer
{
    /// <summary>
    /// Tracks which print line is active and accumulates queued intervals for that line.
    /// Stateless between lines — caller is responsible for advancing via NextLine().
    /// </summary>
    public class PrintLineState
    {
        private readonly int _totalLines;
        private readonly int _linePixelHeight;

        private int _currentLine;
        private readonly List<PrintInterval> _pendingIntervals = new();

        public int CurrentLine => _currentLine;
        public int TotalLines => _totalLines;
        public int LinePixelHeight => _linePixelHeight;
        public bool IsComplete => _currentLine >= _totalLines;

        public IReadOnlyList<PrintInterval> PendingIntervals => _pendingIntervals;

        public PrintLineState(int totalLines, int linePixelHeight = 4)
        {
            _totalLines = totalLines;
            _linePixelHeight = linePixelHeight;
            _currentLine = 0;
        }

        /// <summary>Queues an interval to be drawn on the current line.</summary>
        public void QueueInterval(PrintInterval interval) => _pendingIntervals.Add(interval);

        /// <summary>
        /// Commits all pending intervals to the canvas then clears the queue
        /// and advances to the next line. Returns false if already on the last line.
        /// </summary>
        public bool CommitAndAdvance(PrintCanvas canvas)
        {
            if (IsComplete) return false;

            int y = canvas.LineIndexToCanvasY(_currentLine, _linePixelHeight);
            foreach (var interval in _pendingIntervals)
            {
                // Fill linePixelHeight rows so each logical line has thickness
                for (int dy = 0; dy < _linePixelHeight; dy++)
                    canvas.DrawInterval(interval, y - dy);
            }

            _pendingIntervals.Clear();
            _currentLine++;
            return true;
        }

        /// <summary>Returns [0,1] normalised progress through all lines.</summary>
        public float Progress => _totalLines > 0 ? (float)_currentLine / _totalLines : 0f;
    }
}