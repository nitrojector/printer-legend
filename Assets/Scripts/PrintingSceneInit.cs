using Desktop.WindowSystem;
using Printer;
using UnityEngine;
using WindowContents;

public class PrintingSceneInit : MonoBehaviour
{
    private void Start()
    {
        WindowManager.Instance.Launch<PrinterViewWindowContent>((w, wc) =>
        {
            w.SetPositionNormalized(new(0.25f, 0.5f), new(0.5f, 0.5f));
        });
        WindowManager.Instance.Launch<PrinterReferenceWindowContent>((w, wc) =>
        {
            w.SetPositionNormalized(new(0.75f, 0.5f), new(0.5f, 0.5f));
            wc.SetReferenceSprite(PrinterViewWindowContent.GetReferenceSprite(LevelManager.CurrentLevelIndex));
        });
    }
}
