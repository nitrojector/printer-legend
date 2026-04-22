using System;
using NUnit.Framework.Constraints;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Printer
{
    [RequireComponent(typeof(PrintheadController))]
    [RequireComponent(typeof(PrinterMagic))]
    public class PrinterPlayerController : MonoBehaviour
    {
        [field: Header("Auto Cursor Advance")]
        [field: SerializeField, Min(0.001f)]
        public float PrintStepsPerSecond { get; set; } = 10f;
        
        [Header("References")]
        [SerializeField] private GameObject startGamePrompt;
        [SerializeField] private TMP_Text coundownText;

        /// <summary>
        /// Whether player is paused so no input propagate
        /// </summary>
        private bool _isPlayerPaused = false;
        
        /// <summary>
        /// Whether the player is currently printing
        /// </summary>
        private bool printingStarted = false;

        private bool printingCountdownActive = false;

        /// <summary>
        /// Count down before print starts
        /// </summary>
        private float startTimer = 0f;

        private const float StartTimeDelay = 3.0f;

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

        private InputAction resetAction;

        // Stored delegates so subscribe/unsubscribe are symmetrical
        private readonly Action<InputAction.CallbackContext>[] colorHoldDelegates    = new Action<InputAction.CallbackContext>[4];
        private readonly Action<InputAction.CallbackContext>[] colorReleaseDelegates = new Action<InputAction.CallbackContext>[4];

        private float cursorStepTimer;

        private void Awake()
        {
            printhead = GetComponent<PrintheadController>();
            magic     = GetComponent<PrinterMagic>();
            
            printAction  = InputSystem.actions["Printer/Print"];
            lfAction     = InputSystem.actions["Printer/LF"];
            crAction     = InputSystem.actions["Printer/CR"];
            speedAction  = InputSystem.actions["Printer/Speed"];
            color1Action = InputSystem.actions["Printer/Color1"];
            color2Action = InputSystem.actions["Printer/Color2"];
            color3Action = InputSystem.actions["Printer/Color3"];
            color4Action = InputSystem.actions["Printer/Color4"];
            resetAction = InputSystem.actions["UI/Reset"];

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
            
            startGamePrompt.SetActive(true);
            coundownText.gameObject.SetActive(false);
            
            GameManager.RegisterPlayer(this);
        }

        private void OnEnable()
        {
            // RegisterInput();
        }

        private void OnDisable()
        {
            UnregisterInput();
        }
        
        private void RegisterInput()
        {
            lfAction.performed += OnLF;
            crAction.performed += OnCR;
            resetAction.performed += OnReset;

            SubscribeColorActions(true);
        }


        private void UnregisterInput()
        {
            lfAction.performed -= OnLF;
            crAction.performed -= OnCR;
            resetAction.performed -= OnReset;

            SubscribeColorActions(false);

            // Release any held channels so magic state stays clean
            for (int i = 0; i < 4; i++)
                magic.ReleaseColor(i);
        }

        private void OnDestroy()
        {
            GameManager.UnregisterPlayer(this);
        }

        private void Update()
        {
            if (_isPlayerPaused) return;
                
            if (!printingStarted && printAction.IsPressed())
            {
                printingStarted = true;
                printingCountdownActive = true;
                startGamePrompt.SetActive(false);
                
                startTimer = StartTimeDelay;
                coundownText.SetText($"{Mathf.CeilToInt(startTimer)}");
                coundownText.gameObject.SetActive(true);
                return;
            }

            if (printingCountdownActive)
            {
                coundownText.SetText($"{Mathf.CeilToInt(startTimer)}");
                startTimer -= Time.deltaTime;
                if (startTimer <= 0f)
                {
                    printingCountdownActive = false;
                    RegisterInput();
                    coundownText.gameObject.SetActive(false);
                }

                return;
            }
            
            if (!printingStarted || printhead.IsComplete) return;

            // ── Auto cursor advance (always on) ───────────────────────────────
            cursorStepTimer += Time.deltaTime;
            float stepInterval = 1f / Mathf.Max(0.001f, PrintStepsPerSecond);
            while (cursorStepTimer >= stepInterval)
            {
                printhead.AdvancePrinthead();
                cursorStepTimer -= stepInterval;
            }

            // ── Print: action binding + LMB ───────────────────────────────────
            bool isPrinting = printAction != null && printAction.IsPressed();
            if (isPrinting)
                printhead.Print();

            // Speed adjustment: scroll wheel
            if (magic.IsAbilityEnabled(PrinterAbility.SpeedAdjustment))
            {
                float scroll = speedAction.ReadValue<Vector2>().y;
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

        private void OnReset(InputAction.CallbackContext ctx)
        {
            printingStarted = false;
            printhead.Canvas.Clear();
            printhead.SetPrintheadLine(0);
            printhead.SetPrintheadPosition(0);
            startGamePrompt.SetActive(true);
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

        public bool SetPaused(bool paused)
        {
            if (paused == _isPlayerPaused) return false;
            if (!enabled) return false;
            
            _isPlayerPaused = paused;
            if (_isPlayerPaused)
            {
                UnregisterInput();
            }
            else
            {
                RegisterInput();
            }

            return true;
        }
    }
}