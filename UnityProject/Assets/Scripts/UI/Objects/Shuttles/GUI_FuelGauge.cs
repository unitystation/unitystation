using System.Collections.Generic;
using Logs;
using UnityEngine;

namespace UI.Objects.Shuttles
{
	/// <summary>
	/// This script is attached to a Fuel Gauge and scales a child image to show a Fuel Level
	/// </summary>
	public class GUI_FuelGauge : MonoBehaviour
	{
		[Header("References")]
		public RectTransform fuelGaugeTransform;
		public RectTransform colourBarTransform;
		public RectTransform pointerTransform;

		private float fuelGaugeMaxWidth = 0f;
		public float PercentageFuel;

		private void Start()
		{
			if (fuelGaugeTransform == null || colourBarTransform == null || pointerTransform == null)
			{
				Loggy.LogError("No Fuel Gauge Set on Shuttle!", Category.Shuttles);
				this.enabled = false;
				return;
			}
			fuelGaugeMaxWidth = fuelGaugeTransform.rect.width;
		}

		/// <summary>
		/// Give this method the percentage of fuel left and it'll set the bar correctly
		/// </summary>
		/// <param name="percentageFuel"></param>
		public void UpdateFuelLevel(float percentageFuel)
		{
			PercentageFuel = percentageFuel;
			if (percentageFuel < 0f || percentageFuel > 100f)
			{
				Loggy.LogWarning("Can't set fuel to a non-percent value", Category.Shuttles);
				return;
			}
			float fuelGaugeWidth = (fuelGaugeMaxWidth) / 100 * percentageFuel;
			colourBarTransform.sizeDelta = new Vector2(fuelGaugeWidth, colourBarTransform.sizeDelta.y);
			pointerTransform.anchoredPosition = new Vector2(fuelGaugeWidth - 2f, pointerTransform.anchoredPosition.y);
		}
	}
}
