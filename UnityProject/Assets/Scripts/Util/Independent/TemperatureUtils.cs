using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TemeratureUnits
{
	C,
	K,
	F
}

/// <summary>
/// Transform temperature to different units
/// </summary>
public static class TemperatureUtils
{
	public const float ZERO_CELSIUS_IN_KELVIN = 273.15f;

	public static float Transform(float t, TemeratureUnits from, TemeratureUnits to)
	{
		if (from == to)
		{
			return t;
		}

		float ret = t;
		// transform to kelvins
		if (from != TemeratureUnits.K)
		{
			ret = ToKelvin(t, from);
		}

		// now transform to desired
		if (to != TemeratureUnits.K)
		{
			ret = FromKelvin(t, to);
		}

		return ret;
	}


	public static float ToKelvin(float t, TemeratureUnits from)
	{
		switch (from)
		{
			case TemeratureUnits.C:
				return t + ZERO_CELSIUS_IN_KELVIN;
			case TemeratureUnits.K:
				return t;
			case TemeratureUnits.F:
				return (t + 459.67f) * (5f / 9f);
		}

		return t;
	}

	public static float FromKelvin(float t, TemeratureUnits to)
	{
		switch (to)
		{
			case TemeratureUnits.C:
				return t - ZERO_CELSIUS_IN_KELVIN;
			case TemeratureUnits.K:
				return t;
			case TemeratureUnits.F:
				return t * (9f / 5f) - 459.67f;
		}

		return t;
	}
}
