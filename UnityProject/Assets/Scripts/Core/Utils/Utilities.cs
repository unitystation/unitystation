using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace Core.Utils
{
	public static class Utils
	{
		private static Random random = new Random();
		public static void SetValueByName(this Dropdown dropdown, string valueName)
		{
			List<Dropdown.OptionData> options = dropdown.options;
			for (int i = 0; i < options.Count; i++)
			{
				if (options[i].text == valueName)
				{
					dropdown.value = i;
					break;
				}
			}
		}

		public static string GetValueName(this Dropdown dropdown)
		{
			List<Dropdown.OptionData> options = dropdown.options;
			int selectedIndex = dropdown.value;
			if (selectedIndex >= 0 && selectedIndex < options.Count)
			{
				return options[selectedIndex].text;
			}
			return null;
		}

		public static T[] FindAll<T>(this T[] items, Predicate<T> predicate) => Array.FindAll<T>(items, predicate);
		public static T PickRandom<T>(this IEnumerable<T> source)
		{
			return source.PickRandom(1).SingleOrDefault();
		}

		public static T PickRandomNonNull<T>(this IList<T> source)
		{
			if (source == null || source.Count == 0)
			{
				throw new InvalidOperationException("The list is empty or null.");
			}

			var nonNullItems = source.Where(item => item != null).ToList();

			if (nonNullItems.Count == 0)
			{
				throw new InvalidOperationException("There are no non-null elements in the list.");
			}

			int randomIndex = random.Next(nonNullItems.Count);
			return nonNullItems[randomIndex];
		}

		public static string ToHexString(this UnityEngine.Color color)
		{
			return  $"#{ColorUtility.ToHtmlStringRGBA(color)}";
		}

		public static float RoundToArbitrary(this float ValuedRound, float RoundBy)
		{
			if (RoundBy == 0)
			{
				throw new ArgumentException("Rounding value cannot be zero.", nameof(RoundBy));
			}

			return (float)(Math.Round(ValuedRound / RoundBy) * RoundBy);
		}

		/// <summary>
		/// Used to find if two strings are closer to each other. Useful when trying to search for a string in a list.
		/// </summary>
		/// <returns>Level of clossness</returns>
		public static int LevenshitenDistance(string a, string b)
		{
			if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b) ? 0 : b.Length;
			if (string.IsNullOrEmpty(b)) return a.Length;

			int[,] costs = new int[a.Length + 1, b.Length + 1];

			for (int i = 0; i <= a.Length; i++) costs[i, 0] = i;
			for (int j = 0; j <= b.Length; j++) costs[0, j] = j;

			for (int i = 1; i <= a.Length; i++)
			{
				for (int j = 1; j <= b.Length; j++)
				{
					int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
					costs[i, j] = Math.Min(Math.Min(costs[i - 1, j] + 1, costs[i, j - 1] + 1), costs[i - 1, j - 1] + cost);
				}
			}

			return costs[a.Length, b.Length];
		}

	}

	#if UNITY_EDITOR
	public static class DEBUG
	{
		public static bool RUN(Action Action)
		{
			Action.Invoke();
			return true;
		}
	}
	#endif
}


