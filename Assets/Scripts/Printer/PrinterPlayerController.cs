using UnityEngine;
using UnityEngine.InputSystem;

namespace Printer
{
    [RequireComponent(typeof(PrintheadController))]
    public class PrinterPlayerController : MonoBehaviour
    {
        [field: Header("Auto Cursor Advance")]
        [field: SerializeField, Min(0.001f)]
        public float PrintStepsPerSecond { get; set; } = 10f;

        private PrintheadController printhead;
        private InputAction printAction;
        private InputAction newLineAction;
        private float cursorStepTimer;

        private void Awake()
        {
            printhead = GetComponent<PrintheadController>();
        }

        private void Update()
        {
            if (printhead.IsComplete) return;

            cursorStepTimer += Time.deltaTime;
            float stepInterval = 1f / Mathf.Max(0.001f, PrintStepsPerSecond);

            if (printAction == null || printAction.IsPressed())
                printhead.Print();

            while (cursorStepTimer >= stepInterval)
            {
                printhead.AdvancePrinthead();
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

        private void OnNewLine(InputAction.CallbackContext ctx) => printhead.AdvanceLine();
    }
}