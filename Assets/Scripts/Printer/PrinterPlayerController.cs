using UnityEngine;
using UnityEngine.InputSystem;

namespace Printer
{
    [RequireComponent(typeof(PlayerPrinter))]
    public class PrinterPlayerController : MonoBehaviour
    {
        [field: Header("Auto Cursor Advance")]
        [field: SerializeField, Min(0.001f)]
        public float PrintStepsPerSecond { get; set; } = 10f;

        private PlayerPrinter printer;
        private InputAction printAction;
        private InputAction newLineAction;
        private float cursorStepTimer;

        private void Awake()
        {
            printer = GetComponent<PlayerPrinter>();
        }

        private void Update()
        {
            if (printer == null || printer.IsComplete) return;

            cursorStepTimer += Time.deltaTime;
            float stepInterval = 1f / Mathf.Max(0.001f, PrintStepsPerSecond);

            if (printAction == null || printAction.IsPressed())
                printer.Print();

            while (cursorStepTimer >= stepInterval)
            {
                printer.AdvancePrinthead();
                cursorStepTimer -= stepInterval;
            }
        }

        private void OnEnable()
        {
            printAction = InputSystem.actions["Printer/Print"];
            newLineAction = InputSystem.actions["Printer/NewLine"];

            newLineAction.performed += OnNewLine;
        }

        private void OnDisable()
        {
            newLineAction.performed -= OnNewLine;
        }

        private void OnPrint(InputAction.CallbackContext ctx)
        {
            printer.Print();
        }

        private void OnNewLine(InputAction.CallbackContext ctx) => printer.AdvanceLine();
    }
}