using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Container : MonoBehaviour
{
	public const float ZERO_CELSIUS_IN_KELVIN = 273.15f;

	[SerializeField]
	protected float temperatureKelvin = ZERO_CELSIUS_IN_KELVIN + 20;

	public float TemperatureKelvin
	{
		get => temperatureKelvin;
		protected set => temperatureKelvin = value;
	}

	public float TemperatureCelsius
	{
		get => TemperatureKelvin - ZERO_CELSIUS_IN_KELVIN;
		set => TemperatureKelvin = ZERO_CELSIUS_IN_KELVIN + value;
	}

	public int MaxCapacity = 100;

	//TODO: replace dictionaries with ReagentMix struct with convenience methods
	public Dictionary<string, float> Contents
	{
		get => contents;
		protected set
		{
			contents = value;
		}
	}

	protected virtual void ResetContents()
	{
		Contents = new Dictionary<string, float>();
	}

	[SerializeField] [FormerlySerializedAs(nameof(Contents))]
	private Dictionary<string, float> contents = new Dictionary<string, float>();

}
