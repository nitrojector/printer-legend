using Desktop.WindowSystem;
using Printer;
using UnityEngine;
using UnityEngine.Serialization;

namespace WindowContents
{
	public class PrinterViewWindowContent : WindowContent
	{
		public override string WindowTitle => "Printer View";

		[SerializeField, FormerlySerializedAs("printCanvas")] public PrintCanvas pCanvas;
		[SerializeField, FormerlySerializedAs("controller")] public PrintheadController pController;
		[SerializeField, FormerlySerializedAs("playerController")] public PrinterPlayerController pPlayerController;

		public override void OnResize()
		{
			if (pController != null)
			{
				pController.RefreshLayout();
			}
		}
	}
}