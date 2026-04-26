using Desktop.WindowSystem;
using Printer;
using UnityEngine;

namespace WindowContents
{
	public class PrinterViewWindowContent : WindowContent
	{
		protected override string WindowTitle => "Printer View";

		[SerializeField] private PrintCanvas printCanvas;
		[SerializeField] private PrintheadController controller;
		[SerializeField] private PrinterPlayerController playerController;

		public override void OnResize()
		{
			if (controller != null)
			{
				controller.RefreshLayout();
			}
		}
	}
}