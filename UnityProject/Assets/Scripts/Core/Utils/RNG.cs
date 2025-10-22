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

	//e.g 1 ,9 Will return 1 to 9 randomly
	//as Float
	public static float GetRandomNumber(float min, float max)
	{
		return (float)(Random.NextDouble() * (max - min) + min);
	}


	public static bool FlipACoin()
	{
		return Random.NextDouble() >= 0.5f;
	}

	/// <summary>
	/// 0 to 1
	/// </summary>
	/// <param name="Chance"> 0f to 1f </param>
	/// <returns></returns>
	public static bool RoleChance(float Chance)
	{
		var value = Random.NextDouble();
		return Chance >= value;
	}
	public static Vector2 RandomDirection()
	{
		// Pick a random angle in radians between 0 and 2π
		double angle = Random.NextDouble() * Mathf.PI * 2;

		// Convert the angle to a unit vector
		float x = (float)Mathf.Cos((float)angle);
		float y = (float)Mathf.Sin((float)angle);

		return new Vector2(x, y);
	}

}
