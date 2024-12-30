using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RNG
{
	public static System.Random Random = new System.Random();

	//e.g 1 ,9 Will return 1 to 9 randomly
	//as int
	public static int GetRandomNumber(int min, int max)
	{
		return Random.Next(min, max + 1); // Generates a number between min (inclusive) and max (inclusive)
	}

	public static bool FlipACoin()
	{
		return Random.Next() >= 0.5f;
	}

	/// <summary>
	/// 0 to 1
	/// </summary>
	/// <param name="Chance"> 0f to 1f </param>
	/// <returns></returns>
	public static bool RoleChance(float Chance)
	{
		return Chance >= Random.Next();
	}

}
