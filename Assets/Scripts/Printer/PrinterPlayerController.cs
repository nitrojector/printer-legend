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
        private PrinterMagic magic;

        /// <summary>
        /// Print Activation Action
        /// </summary>
        private InputAction printAction;

        /// <summary>
        /// Line Feed Action
        /// </summary>
        private InputAction lfAction;

        /// <summary>
        /// Carriage Return Action
        /// </summary>
        private InputAction crAction;

        /// <summary>
        /// Speed Adjust Action
        /// </summary>
        private InputAction speedAction;

        // Color Buttons
        private InputAction color1Action;
        private InputAction color2Action;
        private InputAction color3Action;
        private InputAction color4Action;

        private float cursorStepTimer;

        private void Awake()
        {
            printhead = GetComponent<PrintheadController>();
            magic     = GetComponent<PrinterMagic>();

            // TODO: remove, for testing
            magic.EnableAbility(PrinterAbility.SpeedAdjustment);
            magic.EnableAbility(PrinterAbility.CarriageReturn);
            magic.EnableAbility(PrinterAbility.ColorAdjustment);
        }

        private void OnEnable()
        {
            printAction  = InputSystem.actions["Printer/Print"];
            lfAction = InputSystem.actions["Printer/LF"];
            crAction = InputSystem.actions["Printer/CR"];
            speedAction = InputSystem.actions["Printer/Speed"];
            color1Action = InputSystem.actions["Printer/Color1"];
            color2Action = InputSystem.actions["Printer/Color2"];
            color3Action = InputSystem.actions["Printer/Color3"];
            color4Action = InputSystem.actions["Printer/Color4"];

            lfAction.performed += OnLF;
            crAction.performed += OnCR;
        }

        private void OnDisable()
        {
            lfAction.performed -= OnLF;
            crAction.performed -= OnCR;
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

            // ── Print: existing action binding + LMB ──────────────────────────
            bool isPrinting = (printAction != null && printAction.IsPressed())
                              || Mouse.current.leftButton.isPressed;
            if (isPrinting)
                printhead.Print();

            // ── Ability-gated input ───────────────────────────────────────────

            // Newline: RMB (the action-based path is handled in OnNewLine below)
            if (magic.IsAbilityEnabled(PrinterAbility.Newline)
                && Mouse.current.rightButton.wasPressedThisFrame)
            {
                printhead.AdvanceLine();
            }

            // Carriage Return: middle mouse button
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

            // Color selection: number keys 1–4
            if (magic.IsAbilityEnabled(PrinterAbility.ColorAdjustment))
            {
                var kb = Keyboard.current;
                if      (kb.digit1Key.wasPressedThisFrame) magic.SetInkColorIndex(0);
                else if (kb.digit2Key.wasPressedThisFrame) magic.SetInkColorIndex(1);
                else if (kb.digit3Key.wasPressedThisFrame) magic.SetInkColorIndex(2);
                else if (kb.digit4Key.wasPressedThisFrame) magic.SetInkColorIndex(3);

                // Sync the chosen color to the printhead every frame
                printhead.InkColor = magic.CurrentInkColor;
            }
        }

        // ── Action callbacks ──────────────────────────────────────────────────

        private void OnLF(InputAction.CallbackContext ctx)
        {
            if (!magic.IsAbilityEnabled(PrinterAbility.Newline))
            {
                return;
            }

            printhead.AdvanceLine();
        }

        private void OnCR(InputAction.CallbackContext ctx)
        {
            if (!magic.IsAbilityEnabled(PrinterAbility.CarriageReturn))
            {
                return;
            }

            printhead.CarriageReturn();
        }
    }
}
