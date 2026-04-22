using System;
using System.IO;
using UnityEngine;

namespace Utility
{
    /// <summary>
    /// Provides functions for creating a behavior that writes to an external log file.
    /// Log files are written to the file at <see cref="LogPath"/>
    /// </summary>
    /// <typeparam name="T">
    /// the type of the LoggingBehaviour, used for CRTP to allow static log writer per subclass
    /// </typeparam>
    public abstract class LoggingBehaviour<T> : MonoBehaviour
        where T : LoggingBehaviour<T>
    {
        /// <summary>
        /// StreamWriter for the log file. Initialized on first Awake of any
        /// LoggingBehaviour, and shared across all LoggingBehaviours of the same
        /// type <see cref="T"/>.
        /// </summary>
        private static StreamWriter logWriter;

        /// <summary>
        /// Path to the log file path.
        /// </summary>
        protected abstract string LogPath { get; }

        /// <summary>
        /// Whether to append to log file per application session or overwrite.
        /// Override to change default behavior (true).
        /// </summary>
        protected virtual bool Append => true;

        /// <summary>
        /// Initializes the log writer if it is not already initialized. Registers
        /// the <see cref="Dispose"/> method to the <see cref="Application.quitting"/>.
        /// </summary>
        private void Awake()
        {
            if (logWriter != null) return;
            var directory = Path.GetDirectoryName(LogPath);
            if (directory != null && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            logWriter = new StreamWriter(LogPath, Append);
            logWriter.AutoFlush = true;
            Application.quitting += Dispose;
        }

        /// <summary>
        /// Disposes the log writer when the application quits. Registered to
        /// <see cref="Application.quitting"/> event in Awake.
        /// </summary>
        private static void Dispose()
        {
            if (logWriter == null) return;
            logWriter.Dispose();
            logWriter = null;
        }

        /// <summary>
        /// Writes a log message with info level with timestamp to log file.
        /// </summary>
        /// <param name="message">message to log</param>
        protected void LogInfo(string message)
        {
            logWriter?.WriteLine($"[INFO]  {DateTime.Now:O}\t{message}");
        }

        /// <summary>
        /// Writes a log message with warning level with timestamp to log file.
        /// </summary>
        /// <param name="message">message to log</param>
        protected void LogWarning(string message)
        {
            logWriter?.WriteLine($"[WARN]  {DateTime.Now:O}\t{message}");
        }

        /// <summary>
        /// Writes a log message with error level with timestamp to log file.
        /// </summary>
        /// <param name="message">message to log</param>
        protected void LogError(string message)
        {
            logWriter?.WriteLine($"[ERROR] {DateTime.Now:O}\t{message}");
        }
    }
}
