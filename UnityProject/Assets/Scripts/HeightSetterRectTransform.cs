using System;
using UnityEngine;

namespace US13.UI.Systems
{
	//Sticky plaster fix for beard randomly resizing itself inthe UI
	public class HeightSetterRectTransform : MonoBehaviour
	{
		public float ToSetHeight;

		public void Awake()
		{
			var rec = this.GetComponent<RectTransform>();
			rec.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ToSetHeight);
		}

		void Start()
		{
			var rec = this.GetComponent<RectTransform>();
			rec.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ToSetHeight);
		}

	}
}