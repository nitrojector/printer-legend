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
		
		public void SetReferenceSprite(Sprite reference)
		{
			if (pReference == null)
			{
				return;
			}
			
			pReference.ReferenceImage.sprite = reference;
		}
	}
}