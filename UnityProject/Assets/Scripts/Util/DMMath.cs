using System;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public static class DMMath
{
	public static double Round(double n, double x)
	{
		return Math.Round(n / x) * x;
	}

	public static T Max<T>(T[] itemArr)
	{
		return itemArr.Max();
	}

	public static int Clamp(int val, int min, int max)
	{
		return Mathf.Clamp(val, min, max);
	}

	public static bool Prob(double percent)
	{
		Random rand = new Random(Guid.NewGuid().GetHashCode());
		return rand.NextDouble() < percent / 100.0;
	}

	public static float Lerp(float a, float b, float? amount)
	{
		return amount != null ? ((a) + ((b) - (a)) * (amount.Value)) : a ;
	}

	public static float GaussLerp(float x, float x1, float x2)
	{
		var b = (x1 + x2) * 0.5f;
		var c = (x2 - x1) / 6f;

		return Mathf.Exp(-(Mathf.Pow((x - b), 2) / Mathf.Pow((2 * c), 2)));
	}
}