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
		
		[SerializeField] public PrinterReference printerReference;
		
		private void Start()
		{
			AttachedWindow.SetPositionNormalized(new Vector2(0.75f, 0.5f), new Vector2(0.5f, 0.5f));
			Logr.Info("Printer Reference Window Initialized");
		}

		public void SetReferenceSprite(Sprite reference)
		{
			if (printerReference == null)
			{
				return;
			}
			
			printerReference.ReferenceImage.sprite = reference;
		}
	}
}