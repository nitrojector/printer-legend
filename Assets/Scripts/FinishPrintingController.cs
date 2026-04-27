using Desktop.WindowSystem;
using UnityEngine;
using WindowContents;

public class FinishPrintingController : MonoBehaviour
{
    private void Start()
    {
        WindowManager.Instance.Launch<PrintFinalImageWindowContent>();
        WindowManager.Instance.Launch<PrintSummaryWindowContent>();

    
    }
}
