using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Printer
{
    // ── Enums ─────────────────────────────────────────────────────────────────

    public enum PrinterAbility
    {
        /// <summary>Player can manually trigger CR+LF. Default-enabled.</summary>
        Newline,

        /// <summary>Scroll wheel adjusts printhead speed.</summary>
        SpeedAdjustment,

        /// <summary>Player can select from the primary-color ink palette.</summary>
        ColorAdjustment,

        /// <summary>Player can trigger a carriage return (move to line start) without advancing the line.</summary>
        CarriageReturn,
    }

    public enum PrinterObstacle
    {
        /// <summary>Shuffles ink pixels on the affected lines and rewinds the printhead to redo them.</summary>
        PaperJam,

        /// <summary>Hides the reference image for a configurable number of lines.</summary>
        InternetDisconnect,

        /// <summary>Reverses printhead direction (RTL) for a configurable number of lines.</summary>
        MotorMalfunction,
    }

    // ── PrinterMagic ──────────────────────────────────────────────────────────

    /// <summary>
    /// Central hub for all printer abilities and obstacles.
    /// Abilities are opt-in features the player can use; obstacles are environmental
    /// disruptions applied for a fixed number of line advances (min 1).
    ///
    /// Query <see cref="IsAbilityEnabled"/> / <see cref="IsObstacleActive"/> from UI
    /// or game systems without reaching into PrintheadController directly.
    /// </summary>
    [RequireComponent(typeof(PrintheadController))]
    public class PrinterMagic : MonoBehaviour
    {
        // ── Ink palette ───────────────────────────────────────────────────────

        /// <summary>Available ink colors for the Color Adjustment ability (Black + RGB primaries).</summary>
        public static readonly Color[] InkPalette =
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.black,
        };

        // ── Speed Adjustment parameters ───────────────────────────────────────

        [Header("Speed Adjustment")]
        [SerializeField, Min(0.001f)] private float minPrintSpeed   = 1f;
        [SerializeField]              private float maxPrintSpeed   = 50f;
        [SerializeField, Min(0.001f)] private float speedScrollStep = 1f;

        public float MinPrintSpeed   => minPrintSpeed;
        public float MaxPrintSpeed   => maxPrintSpeed;
        public float SpeedScrollStep => speedScrollStep;

        // ── Paper Jam parameters ──────────────────────────────────────────────

        [Header("Paper Jam")]
        [Tooltip("Number of lines the player must reprint after a jam (min 1).")]
        [SerializeField, Min(1)] private int paperJamLineDuration = 2;

        [Tooltip("Number of ink pixels displaced during the jam.")]
        [SerializeField, Min(1)] private int paperJamShuffleCount = 12;

        [Tooltip("When true, horizontal displacement snaps to the print-size grid.")]
        [SerializeField] private bool paperJamRespectPrintSize = true;

        // ── Internet Disconnect parameters ────────────────────────────────────

        [Header("Internet Disconnect")]
        [Tooltip("Number of lines the reference image stays disrupted.")]
        [SerializeField, Min(1)] private int internetDisconnectLineDuration = 4;

        [Tooltip("Seconds the reference image stays fully blacked out.")]
        [SerializeField, Min(0f)] private float internetDisconnectBlackoutSeconds = 1.5f;

        [Tooltip("Seconds to fade the reference image back in.")]
        [SerializeField, Min(0f)] private float internetDisconnectFadeInSeconds = 0.5f;

        [Tooltip("CanvasGroup on the reference image GameObject (assign in Inspector).")]
        [SerializeField] private CanvasGroup referenceImageGroup;

        // ── Motor Malfunction parameters ──────────────────────────────────────

        [Header("Motor Malfunction")]
        [Tooltip("Number of lines the printhead runs right-to-left (min 1).")]
        [SerializeField, Min(1)] private int motorMalfunctionLineDuration = 2;

        // ── Internal state ────────────────────────────────────────────────────

        private readonly HashSet<PrinterAbility>                enabledAbilities = new();
        private readonly HashSet<PrinterObstacle>               enabledObstacles = new();
        private readonly Dictionary<PrinterObstacle, int>       activeObstacles  = new();

        private PrintheadController printhead;
        private PrintCanvas         canvas;
        private Coroutine           disconnectCoroutine;

        // Per-color state (index into InkPalette)
        private readonly HashSet<int> toggledColors = new();
        private readonly HashSet<int> heldColors    = new();
        private Color currentInkColor = Color.black;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Fired when an ability is enabled or disabled. Bool = new enabled state.</summary>
        public event Action<PrinterAbility, bool> onAbilityChanged;

        /// <summary>Fired when an obstacle becomes active or ends. Bool = is now active.</summary>
        public event Action<PrinterObstacle, bool> onObstacleChanged;

        /// <summary>Fired whenever the mixed ink color changes.</summary>
        public event Action<Color> onColorChanged;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            printhead = GetComponent<PrintheadController>();
            canvas    = GetComponent<PrintCanvas>();

            // Newline is always available out of the box
            enabledAbilities.Add(PrinterAbility.Newline);
        }

        private void OnEnable()  => printhead.OnLineAdvanced += HandleLineAdvanced;
        private void OnDisable() => printhead.OnLineAdvanced -= HandleLineAdvanced;

        // ── Ability API ───────────────────────────────────────────────────────

        public bool IsAbilityEnabled(PrinterAbility ability) => enabledAbilities.Contains(ability);

        public void EnableAbility(PrinterAbility ability)
        {
            if (enabledAbilities.Add(ability))
                onAbilityChanged?.Invoke(ability, true);
        }

        public void DisableAbility(PrinterAbility ability)
        {
            if (!enabledAbilities.Remove(ability)) return;
            if (ability == PrinterAbility.ColorAdjustment && heldColors.Count > 0)
            {
                heldColors.Clear();
                RefreshInkColor();
            }
            onAbilityChanged?.Invoke(ability, false);
        }

        public void SetAbilityEnabled(PrinterAbility ability, bool enabled)
        {
            if (enabled) EnableAbility(ability);
            else         DisableAbility(ability);
        }

        // ── Obstacle API ──────────────────────────────────────────────────────

        /// <summary>Whether the obstacle has been unlocked (can potentially be triggered).</summary>
        public bool IsObstacleEnabled(PrinterObstacle obstacle) => enabledObstacles.Contains(obstacle);

        /// <summary>Whether the obstacle is currently running.</summary>
        public bool IsObstacleActive(PrinterObstacle obstacle) => activeObstacles.ContainsKey(obstacle);

        /// <summary>Returns the remaining line count for an active obstacle, or 0 if inactive.</summary>
        public int ObstacleRemainingLines(PrinterObstacle obstacle) =>
            activeObstacles.TryGetValue(obstacle, out int v) ? v : 0;

        public void EnableObstacle(PrinterObstacle obstacle)  => enabledObstacles.Add(obstacle);
        public void DisableObstacle(PrinterObstacle obstacle) => enabledObstacles.Remove(obstacle);

        /// <summary>
        /// Starts the obstacle if it is enabled and not already active.
        /// Returns false if the obstacle is disabled, already running, or could not start.
        /// </summary>
        public bool StartObstacle(PrinterObstacle obstacle)
        {
            if (!enabledObstacles.Contains(obstacle))  return false;
            if (activeObstacles.ContainsKey(obstacle)) return false;

            activeObstacles[obstacle] = LineDurationFor(obstacle);
            ApplyObstacleStart(obstacle);
            onObstacleChanged?.Invoke(obstacle, true);
            return true;
        }

        /// <summary>Force-stops a running obstacle before its line count expires.</summary>
        public void StopObstacle(PrinterObstacle obstacle)
        {
            if (!activeObstacles.ContainsKey(obstacle)) return;
            activeObstacles.Remove(obstacle);
            ApplyObstacleEnd(obstacle);
            onObstacleChanged?.Invoke(obstacle, false);
        }

        // ── Color API (Color Adjustment ability) ──────────────────────────────
        //
        // Individual palette channels can be toggled (persistent on/off) or held
        // (active only while the caller keeps the hold open).  Both modes compose:
        // a channel contributes to the mix if it is toggled ON *or* held.
        //
        // Toggle pattern  →  call ToggleColor / SetColorToggled on press
        // Hold pattern    →  call HoldColor on press, ReleaseColor on release
        //
        // Colors blend additively: R+G = Yellow, R+B = Magenta, G+B = Cyan, etc.
        // No active channel → black.

        /// <summary>The current blended ink color (recomputed on every channel change).</summary>
        public Color CurrentInkColor => currentInkColor;

        /// <summary>True if the channel contributes to the mix (toggled on or currently held).</summary>
        public bool IsColorContributing(int index) =>
            toggledColors.Contains(index) || heldColors.Contains(index);

        public bool IsColorToggled(int index) => toggledColors.Contains(index);
        public bool IsColorHeld(int index)    => heldColors.Contains(index);

        // ── Toggle ────────────────────────────────────────────────────────────

        /// <summary>Flips the persistent toggle state of the palette channel at <paramref name="index"/>.</summary>
        public void ToggleColor(int index) =>
            SetColorToggled(index, !toggledColors.Contains(index));

        /// <summary>Explicitly sets the persistent toggle state of a palette channel.</summary>
        public void SetColorToggled(int index, bool on)
        {
            bool changed = on ? toggledColors.Add(index) : toggledColors.Remove(index);
            if (changed) RefreshInkColor();
        }

        // ── Hold ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Marks a channel as held — it contributes to the mix until <see cref="ReleaseColor"/>
        /// is called.  Intended for press→release input bindings.
        /// </summary>
        public void HoldColor(int index)
        {
            if (heldColors.Add(index)) RefreshInkColor();
        }

        /// <summary>Removes the hold on a channel.  Toggle state is unaffected.</summary>
        public void ReleaseColor(int index)
        {
            if (heldColors.Remove(index)) RefreshInkColor();
        }

        // ── Internals ─────────────────────────────────────────────────────────

        private void RefreshInkColor()
        {
            currentInkColor = ComputeMixedColor();
            onColorChanged?.Invoke(currentInkColor);
        }

        private Color ComputeMixedColor()
        {
            float r = 0f, g = 0f, b = 0f;
            bool any = false;
            for (int i = 0; i < InkPalette.Length; i++)
            {
                if (!IsColorContributing(i)) continue;
                Color c = InkPalette[i];
                r += c.r; g += c.g; b += c.b;
                any = true;
            }
            return any
                ? new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b))
                : Color.black;
        }

        // ── Line-advance tracking ─────────────────────────────────────────────

        private void HandleLineAdvanced()
        {
            // Tick down all active obstacles; collect those that have expired
            var toEnd = new List<PrinterObstacle>(activeObstacles.Count);
            foreach (var pair in activeObstacles)
            {
                int remaining = pair.Value - 1;
                if (remaining <= 0)
                    toEnd.Add(pair.Key);
                else
                    activeObstacles[pair.Key] = remaining;
            }

            foreach (var obs in toEnd)
            {
                activeObstacles.Remove(obs);
                ApplyObstacleEnd(obs);
                onObstacleChanged?.Invoke(obs, false);
            }
        }

        // ── Obstacle effects ──────────────────────────────────────────────────

        private void ApplyObstacleStart(PrinterObstacle obstacle)
        {
            switch (obstacle)
            {
                case PrinterObstacle.PaperJam:
                    ApplyPaperJam();
                    break;

                case PrinterObstacle.InternetDisconnect:
                    if (disconnectCoroutine != null) StopCoroutine(disconnectCoroutine);
                    disconnectCoroutine = StartCoroutine(InternetDisconnectRoutine());
                    break;

                case PrinterObstacle.MotorMalfunction:
                    printhead.IsRightToLeft = true;
                    break;
            }
        }

        private void ApplyObstacleEnd(PrinterObstacle obstacle)
        {
            switch (obstacle)
            {
                case PrinterObstacle.PaperJam:
                    // Nothing to undo — canvas was already cleared during start
                    break;

                case PrinterObstacle.InternetDisconnect:
                    // Ensure image is fully visible when the line count expires
                    if (referenceImageGroup != null)
                    {
                        if (disconnectCoroutine != null)
                        {
                            StopCoroutine(disconnectCoroutine);
                            disconnectCoroutine = null;
                        }
                        referenceImageGroup.alpha = 1f;
                    }
                    break;

                case PrinterObstacle.MotorMalfunction:
                    printhead.IsRightToLeft = false;
                    break;
            }
        }

        // ── Paper Jam ─────────────────────────────────────────────────────────

        private void ApplyPaperJam()
        {
            if (canvas == null) return;

            int startLine = Mathf.Max(0, printhead.CurrentLine - paperJamLineDuration + 1);
            int count     = printhead.CurrentLine - startLine + 1;

            // Shuffle pixels on the affected lines
            for (int li = 0; li < count; li++)
                canvas.ShuffleLinePixels(
                    startLine + li,
                    printhead.LinePixelHeight,
                    paperJamShuffleCount,
                    paperJamRespectPrintSize,
                    printhead.LinePixelWidth);

            // Rewind the printhead so the player reprints those lines
            printhead.RewindLines(count);
        }

        // ── Internet Disconnect ───────────────────────────────────────────────

        private IEnumerator InternetDisconnectRoutine()
        {
            if (referenceImageGroup == null) yield break;

            referenceImageGroup.alpha = 0f;

            if (internetDisconnectBlackoutSeconds > 0f)
                yield return new WaitForSeconds(internetDisconnectBlackoutSeconds);

            // Fade back in
            float elapsed = 0f;
            while (elapsed < internetDisconnectFadeInSeconds)
            {
                elapsed += Time.deltaTime;
                referenceImageGroup.alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, internetDisconnectFadeInSeconds));
                yield return null;
            }

            referenceImageGroup.alpha = 1f;
            disconnectCoroutine = null;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private int LineDurationFor(PrinterObstacle obstacle) => obstacle switch
        {
            PrinterObstacle.PaperJam            => paperJamLineDuration,
            PrinterObstacle.InternetDisconnect  => internetDisconnectLineDuration,
            PrinterObstacle.MotorMalfunction    => motorMalfunctionLineDuration,
            _                                   => 1,
        };
    }
}
