using System;
using UnityEngine;
using US13.Managers;
using US13.UI.Core.Net.Elements;

namespace US13.UI.Objects.Shuttles
{
	/// Slider, used to call methods with float as arg
	[RequireComponent(typeof(GUI_FuelGauge))]
	[Serializable]
	public class NetFuelGauge : NetUIStringElement
	{
		public override ElementMode InteractionMode => ElementMode.ServerWrite;
		public override string Value {
			get => Element.PercentageFuel.ToString();
			protected set {
				externalChange = true;
				Element.PercentageFuel = float.Parse(value);
				Element.UpdateFuelLevel(Element.PercentageFuel);
				externalChange = false;
			}
		}

		public FloatEvent ServerMethod;

		private GUI_FuelGauge element;
		public GUI_FuelGauge Element => element ??= GetComponent<GUI_FuelGauge>();

		public override void ExecuteServer(PlayerInfo subject)
		{
			ServerMethod.Invoke(Element.PercentageFuel);
		}
	}
}
