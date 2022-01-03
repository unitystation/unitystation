using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// Slider, used to call methods with float as arg
[RequireComponent(typeof(Slider))]
[Serializable]
public class NetSlider : NetUIStringElement
{
	public override string Value
	{
		get
		{
			return (( int ) ( Element.value * 100 )).ToString();
		}
		set
		{
			externalChange = true;
			Element.value = int.Parse(value) / 100f;
			externalChange = false;
		}
	}

	public FloatEvent ServerMethod;

	private Slider element;
	public Slider Element
	{
		get
		{
			if ( !element )
			{
				element = GetComponent<Slider>();
			}
			return element;
		}
	}

	public override void ExecuteServer(ConnectedPlayer subject)
	{
		ServerMethod.Invoke(Element.value);
	}
}
/// <inheritdoc />
/// "If you wish to use a generic UnityEvent type you must override the class type."
[Serializable]
public class FloatEvent : UnityEvent<float>{}