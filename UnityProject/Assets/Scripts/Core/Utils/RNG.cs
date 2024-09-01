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

}
