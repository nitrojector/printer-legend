using System;
using Desktop.WindowSystem;
using UnityEngine;

namespace WindowContents
{
	public class PrinterReferenceWindowContent : WindowContent
	{
		private void Start()
		{
			AttachedWindow.SetPosition(Vector2.one, Vector2.one);
		}
	}
}