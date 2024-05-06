using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Kelvin
{
	public static float FromC(float temp) => TemperatureUtils.ToKelvin(temp, TemeratureUnits.C);
}
