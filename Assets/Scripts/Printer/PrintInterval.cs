using UnityEngine;

namespace Printer
{
    /// <summary>
    /// A discrete unit of ink: a position and width on the current print line.
    /// Immutable value type — create a new one per print event.
    /// </summary>
    public readonly struct PrintInterval
    {
        /// <summary>X coordinate in canvas pixels.</summary>
        public readonly int CanvasX;

        /// <summary>Total width of the printed mark in pixels.</summary>
        public readonly int PixelWidth;

        public readonly Color Color;

        public PrintInterval(int canvasX, Color color, int pixelWidth = 2)
        {
            CanvasX = canvasX;
            Color = color;
            PixelWidth = pixelWidth;
        }
    }
}