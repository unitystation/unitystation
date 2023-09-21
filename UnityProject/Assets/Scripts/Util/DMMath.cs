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

	/// <summary>
	/// this method can be used to determine whether a given event should occur with a certain probability defined by the input "percent".
	/// For example, if percent=50, there is a 50% chance that the method will return true, and a 50% chance that it will return false.
	/// </summary>
	/// <returns>If the random value is less than the threshold, the method returns true. Otherwise, it returns false.</returns>
	public static bool Prob(double percent)
	{
		return RNG.Random.NextDouble() < percent / 100.0;
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