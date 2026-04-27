using System;
using Desktop.WindowSystem;
using UnityEngine;
using Utility;

namespace WindowContents
{
	public class PrinterReferenceWindowContent : WindowContent
	{
		private void Start()
		{
			AttachedWindow.SetPositionNormalized(new Vector2(0.75f, 0.5f), new Vector2(0.5f, 0.5f));
			Logr.Info("Printer Reference Window Initialized");
		}
	}
}