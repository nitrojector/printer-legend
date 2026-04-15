using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Printer
{
    [RequireComponent(typeof(PrintheadController))]
    [RequireComponent(typeof(PrinterMagic))]
    public class PrinterPlayerController : MonoBehaviour
    {
        [field: Header("Auto Cursor Advance")]
        [field: SerializeField, Min(0.001f)]
        public float PrintStepsPerSecond { get; set; } = 10f;

        private PrintheadController printhead;
        private PrinterMagic        magic;

        /// <summary>Print Activation Action</summary>
        private InputAction printAction;

        /// <summary>Line Feed Action</summary>
        private InputAction lfAction;

        /// <summary>Carriage Return Action</summary>
        private InputAction crAction;

        /// <summary>Speed Adjust Action</summary>
        private InputAction speedAction;

        // Color channel actions (one per palette entry)
        private InputAction color1Action;
        private InputAction color2Action;
        private InputAction color3Action;
        private InputAction color4Action;

        // Stored delegates so subscribe/unsubscribe are symmetrical
        private readonly Action<InputAction.CallbackContext>[] colorHoldDelegates    = new Action<InputAction.CallbackContext>[4];
        private readonly Action<InputAction.CallbackContext>[] colorReleaseDelegates = new Action<InputAction.CallbackContext>[4];

        private float cursorStepTimer;

        private void Awake()
        {
            printhead = GetComponent<PrintheadController>();
            magic     = GetComponent<PrinterMagic>();

            // Build per-index delegates once; lambdas capture the loop variable correctly
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                colorHoldDelegates[idx]    = _ => OnColorHold(idx);
                colorReleaseDelegates[idx] = _ => OnColorRelease(idx);
            }

            // TODO: remove, for testing
            magic.EnableAbility(PrinterAbility.SpeedAdjustment);
            magic.EnableAbility(PrinterAbility.CarriageReturn);
            magic.EnableAbility(PrinterAbility.ColorAdjustment);
        }

        private void OnEnable()
        {
            printAction  = InputSystem.actions["Printer/Print"];
            lfAction     = InputSystem.actions["Printer/LF"];
            crAction     = InputSystem.actions["Printer/CR"];
            speedAction  = InputSystem.actions["Printer/Speed"];
            color1Action = InputSystem.actions["Printer/Color1"];
            color2Action = InputSystem.actions["Printer/Color2"];
            color3Action = InputSystem.actions["Printer/Color3"];
            color4Action = InputSystem.actions["Printer/Color4"];

            lfAction.performed += OnLF;
            crAction.performed += OnCR;

            SubscribeColorActions(true);
        }

        private void OnDisable()
        {
            lfAction.performed -= OnLF;
            crAction.performed -= OnCR;

            SubscribeColorActions(false);

            // Release any held channels so magic state stays clean
            for (int i = 0; i < 4; i++)
                magic.ReleaseColor(i);
        }

        private void Update()
        {
            if (printhead.IsComplete) return;

            // ── Auto cursor advance (always on) ───────────────────────────────
            cursorStepTimer += Time.deltaTime;
            float stepInterval = 1f / Mathf.Max(0.001f, PrintStepsPerSecond);
            while (cursorStepTimer >= stepInterval)
            {
                printhead.AdvancePrinthead();
                cursorStepTimer -= stepInterval;
            }

            // ── Print: action binding + LMB ───────────────────────────────────
            bool isPrinting = (printAction != null && printAction.IsPressed())
                              || Mouse.current.leftButton.isPressed;
            if (isPrinting)
                printhead.Print();

            // ── Ability-gated input ───────────────────────────────────────────

            // Newline: RMB (action-based path handled in OnLF callback)
            if (magic.IsAbilityEnabled(PrinterAbility.Newline)
                && Mouse.current.rightButton.wasPressedThisFrame)
            {
                printhead.AdvanceLine();
            }

            // Carriage Return: middle mouse button (action-based path in OnCR)
            if (magic.IsAbilityEnabled(PrinterAbility.CarriageReturn)
                && Mouse.current.middleButton.wasPressedThisFrame)
            {
                printhead.CarriageReturn();
            }

            // Speed adjustment: scroll wheel
            if (magic.IsAbilityEnabled(PrinterAbility.SpeedAdjustment))
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    PrintStepsPerSecond = Mathf.Clamp(
                        PrintStepsPerSecond + scroll * magic.SpeedScrollStep,
                        magic.MinPrintSpeed,
                        magic.MaxPrintSpeed);
                }
            }

            // Color: sync mixed color to printhead each frame
            if (magic.IsAbilityEnabled(PrinterAbility.ColorAdjustment))
                printhead.InkColor = magic.CurrentInkColor;
        }

        // ── Action callbacks ──────────────────────────────────────────────────

        private void OnLF(InputAction.CallbackContext ctx)
        {
            if (!magic.IsAbilityEnabled(PrinterAbility.Newline)) return;
            printhead.AdvanceLine();
        }

        private void OnCR(InputAction.CallbackContext ctx)
        {
            if (!magic.IsAbilityEnabled(PrinterAbility.CarriageReturn)) return;
            printhead.CarriageReturn();
        }

        // Color hold/release — wired to color action performed/canceled
        private void OnColorHold(int index)
        {
            if (magic.IsAbilityEnabled(PrinterAbility.ColorAdjustment))
                magic.HoldColor(index);
        }

        private void OnColorRelease(int index)
        {
            // Always release regardless of ability state to prevent stuck channels
            magic.ReleaseColor(index);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SubscribeColorActions(bool subscribe)
        {
            InputAction[] actions = { color1Action, color2Action, color3Action, color4Action };
            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i] == null) continue;
                if (subscribe)
                {
                    actions[i].performed += colorHoldDelegates[i];
                    actions[i].canceled  += colorReleaseDelegates[i];
                }
                else
                {
                    actions[i].performed -= colorHoldDelegates[i];
                    actions[i].canceled  -= colorReleaseDelegates[i];
                }
            }
        }
    }
}
