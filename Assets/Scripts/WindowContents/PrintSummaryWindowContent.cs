using System.Collections.Generic;
using Desktop.WindowSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WindowContents
{
    public class PrintSummaryWindowContent : WindowContent
    {
        public override string WindowTitle => "Print Summary";
        public override bool AllowMaximize => false;
        public override bool AllowMinimize => false;

        [SerializeField] private TMP_Text restartsLabel;
        [SerializeField] private TMP_Text accuracyLabel;
        [SerializeField] private Button nextPrintButton;

        private int _restarts;
        private float _accuracy;
        private PrinterViewWindowContent _printerView;

        private void Awake()
        {
            nextPrintButton?.onClick.AddListener(OnNextPrint);
        }

        public void SetData(int restarts, float accuracy, PrinterViewWindowContent printerView)
        {
            _restarts = restarts;
            _accuracy = accuracy;
            _printerView = printerView;
        }

        public override void OnShow()
        {
            if (restartsLabel != null)
                restartsLabel.text = $"Restarts: {_restarts}";

            if (accuracyLabel != null)
                accuracyLabel.text = _accuracy < 0f ? "Accuracy: N/A" : $"Accuracy: {_accuracy:P0}";
        }

        private void OnNextPrint()
        {
            CloseFinalPrintWindow();
            CloseWindow();
            _printerView?.ReactivateForNextLevel();
        }

        private void CloseFinalPrintWindow()
        {
            var snapshot = new List<Window>(WindowManager.Instance.ActiveWindows);
            foreach (var win in snapshot)
                if (win.Content is PrintFinalImageWindowContent)
                    win.Quit();
        }
    }
}
