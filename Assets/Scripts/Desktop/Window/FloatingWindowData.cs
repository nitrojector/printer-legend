using UnityEngine;

namespace Desktop.Window
{
	public struct FloatingWindowData
	{
		public Vector2 anchoredPosition;
		public Vector2 sizeDelta;
		public Vector2 anchorMin;
		public Vector2 anchorMax;
		public Vector2 offsetMin;
		public Vector2 offsetMax;
			 
		public FloatingWindowData(RectTransform rt)
		{
			anchoredPosition = rt.anchoredPosition;
			sizeDelta = rt.sizeDelta;
			anchorMin = rt.anchorMin;
			anchorMax = rt.anchorMax;
			offsetMin = rt.offsetMin;
			offsetMax = rt.offsetMax;
		}
			 
		public void ReadFrom(RectTransform rt)
		{
			anchoredPosition = rt.anchoredPosition;
			sizeDelta = rt.sizeDelta;
			anchorMin = rt.anchorMin;
			anchorMax = rt.anchorMax;
			offsetMin = rt.offsetMin;
			offsetMax = rt.offsetMax;
		}
			 
		public void ApplyTo(RectTransform rt)
		{
			rt.anchoredPosition = anchoredPosition;
			rt.sizeDelta = sizeDelta;
			rt.anchorMin = anchorMin;
			rt.anchorMax = anchorMax;
			rt.offsetMin = offsetMin;
			rt.offsetMax = offsetMax;
		}
	}
		
}