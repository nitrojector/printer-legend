using System;
using UnityEngine;

namespace Desktop
{
	[RequireComponent(typeof(Canvas))]
	public class DesktopWrapper : MonoBehaviour
	{
		public Canvas DesktopCanvas { get; private set; }

		private void Awake()
		{
			DesktopCanvas = GetComponent<Canvas>();
		}
	}
}