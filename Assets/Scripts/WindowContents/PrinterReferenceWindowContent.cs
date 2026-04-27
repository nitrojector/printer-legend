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
		
		public override void OnInitialize()
		{
			GameMgr.Instance.RegisterPrinterReference(this);
		}

		private void OnDestroy()
		{
			GameMgr.Instance.UnregisterPrinterReference(this);
		}

		public void SetWindowVisible(bool visible)
		{
			if (visible) AttachedWindow?.Show();
			else AttachedWindow?.Minimize();
		}

		public void SetReferenceSprite(Sprite reference)
		{
			if (pReference == null) return;
			pReference.ReferenceImage.sprite = reference;
		}
	}
}