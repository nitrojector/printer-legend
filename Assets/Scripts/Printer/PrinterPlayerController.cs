using UnityEngine;
using UnityEngine.InputSystem;

namespace Printer
{
    [RequireComponent(typeof(PlayerPrinter))]
    public class PrinterPlayerController : MonoBehaviour
    {
        private PlayerPrinter _printer;
        private InputAction _printAction;
        private InputAction _newLineAction;

        private void Awake()
        {
            _printer = GetComponent<PlayerPrinter>();
        }

        private void OnEnable()
        {
            _printAction = InputSystem.actions["Printer/Print"];
            _newLineAction = InputSystem.actions["Printer/NewLine"];

            _printAction.performed += OnPrint;
            _newLineAction.performed += OnNewLine;
        }

        private void OnDisable()
        {
            _printAction.performed -= OnPrint;
            _newLineAction.performed -= OnNewLine;
        }

        private void OnPrint(InputAction.CallbackContext ctx) => _printer.Print();
        private void OnNewLine(InputAction.CallbackContext ctx) => _printer.AdvanceLine();
    }
}