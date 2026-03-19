using System;
using System.Collections.Generic;
using UnityEngine;

namespace US13.Core.Utils
{
	public static class RNG
	{
		public static System.Random Random = new System.Random();

		private static readonly int[] RNG01 = new int[] { 0, 1, 2, 3 };
		private static readonly int[] RNG02 = new int[] { 0, 1, 3, 2 };
		private static readonly int[] RNG03 = new int[] { 0, 2, 1, 3 };
		private static readonly int[] RNG04 = new int[] { 0, 2, 3, 1 };
		private static readonly int[] RNG05 = new int[] { 0, 3, 1, 2 };
		private static readonly int[] RNG06 = new int[] { 0, 3, 2, 1 };
		private static readonly int[] RNG07 = new int[] { 1, 0, 2, 3 };
		private static readonly int[] RNG08 = new int[] { 1, 0, 3, 2 };
		private static readonly int[] RNG09 = new int[] { 1, 2, 0, 3 };
		private static readonly int[] RNG10 = new int[] { 1, 2, 3, 0 };
		private static readonly int[] RNG11 = new int[] { 1, 3, 0, 2 };
		private static readonly int[] RNG12 = new int[] { 1, 3, 2, 0 };
		private static readonly int[] RNG13 = new int[] { 2, 0, 1, 3 };
		private static readonly int[] RNG14 = new int[] { 2, 0, 3, 1 };
		private static readonly int[] RNG15 = new int[] { 2, 1, 0, 3 };
		private static readonly int[] RNG16 = new int[] { 2, 1, 3, 0 };
		private static readonly int[] RNG17 = new int[] { 2, 3, 0, 1 };
		private static readonly int[] RNG18 = new int[] { 2, 3, 1, 0 };
		private static readonly int[] RNG19 = new int[] { 3, 0, 1, 2 };
		private static readonly int[] RNG20 = new int[] { 3, 0, 2, 1 };
		private static readonly int[] RNG21 = new int[] { 3, 1, 0, 2 };
		private static readonly int[] RNG22 = new int[] { 3, 1, 2, 0 };
		private static readonly int[] RNG23 = new int[] { 3, 2, 0, 1 };
		private static readonly int[] RNG24 = new int[] { 3, 2, 1, 0 };


		public static int[] GetRandomDirectionLoop()
		{
			int choice = GetRandomNumber(0, 23);

			switch (choice)
			{
				case 0:  return RNG01;
				case 1:  return RNG02;
				case 2:  return RNG03;
				case 3:  return RNG04;
				case 4:  return RNG05;
				case 5:  return RNG06;
				case 6:  return RNG07;
				case 7:  return RNG08;
				case 8:  return RNG09;
				case 9:  return RNG10;
				case 10: return RNG11;
				case 11: return RNG12;
				case 12: return RNG13;
				case 13: return RNG14;
				case 14: return RNG15;
				case 15: return RNG16;
				case 16: return RNG17;
				case 17: return RNG18;
				case 18: return RNG19;
				case 19: return RNG20;
				case 20: return RNG21;
				case 21: return RNG22;
				case 22: return RNG23;
				case 23: return RNG24;
				default: return RNG01; // safety
			}
		}

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

		private static class Cache<T> where T : struct, Enum
		{
			public static readonly T[] Values = (T[])Enum.GetValues(typeof(T));
		}

		public static T BetterRandomValue<T>(this T _) where T : struct, Enum
		{
			var values = Cache<T>.Values;
			return values[Random.Next(values.Length)];
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

		public static T RemoveRandom<T>(this IList<T> list)
		{
			int index = Random.Next(list.Count);
			T value = list[index];

			int last = list.Count - 1;
			list[index] = list[last];
			list.RemoveAt(last);

			return value;
		}

	}
}
