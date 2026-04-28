using System;
using Desktop.WindowSystem;
using Printer;
using UnityEngine;
using Utility;

namespace WindowContents
{
	public class PrinterReferenceWindowContent : WindowContent
	{
		public override string WindowTitle => "Reference";
		
		[SerializeField] public PrinterReference pReference;

		public int PrintViewId => _printViewId;
		
		private int _printViewId = -1;

		public void SetPrintViewId(int id)
		{
			_printViewId = id;
			GameMgr.Instance.RegisterReference(id, this);
			GameMgr.Instance.GetPrintView(id)?.pController?.SetPrinterReference(pReference);
		}

		private void OnDestroy()
		{
			if (_printViewId >= 0)
				GameMgr.Instance.UnregisterReference(_printViewId);
		}

		public override string GetContentDescription()
		{
			return $"pReference PrintID({PrintViewId})";
		}

		public void SetWindowVisible(bool visible)
		{
			if (visible) AttachedWindow?.Show();
			else AttachedWindow?.Minimize();
		}

		public void SetReferenceSprite(Sprite reference)
		{
			if (pReference == null) return;
			pReference.LoadReference(reference);
		}

		/// <summary>Destroys this window. Called by PrinterViewWindowContent after print completes.</summary>
		public void Close() => CloseWindow();
	}
}