using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// Slider, used to call methods with float as arg
[RequireComponent(typeof(GUI_FuelGauge))]
[Serializable]
public class NetFuelGauge : NetUIStringElement
{
	public override ElementMode InteractionMode => ElementMode.ServerWrite;
	public override string Value
	{
		get
		{
			return (Element.PercentageFuel).ToString();
		}
		set
		{
			externalChange = true;
			Element.PercentageFuel = float.Parse(value);
			Element.UpdateFuelLevel(Element.PercentageFuel);
			externalChange = false;
		}
	}

	public FloatEvent ServerMethod;

	private GUI_FuelGauge element;
	public GUI_FuelGauge Element
	{
		get
		{
			if ( !element )
			{
				element = GetComponent<GUI_FuelGauge>();
			}
			return element;
		}
	}

	public override void ExecuteServer(ConnectedPlayer subject)
	{
		ServerMethod.Invoke(Element.PercentageFuel);
	}
}