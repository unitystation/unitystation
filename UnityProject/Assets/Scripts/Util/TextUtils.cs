using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TextUtils
{
	/// <summary>
	/// Some most common collors Hue component
	/// </summary>
	private static List<(float, string)> CommonColors = new List<(float, string)>()
		{
			( 0f, "red"),
			( 0.125f, "orange"),
			( 0.2f, "yellow"),
			( 0.3f, "green"),
			( 0.66f, "blue"),
			( 0.625f, "blue"),
			( 0.75f, "purple"),
			( 0.875f, "pink"),
			( 1f, "red"),
		};

	private static Dictionary<string, string> ColorsExceptions = new Dictionary<string, string>()
	{
		{"dark orange", "brown" },
		{"bright orange", "bright brown" }
	};

	/// <summary>
	/// Gets closest most common color as a string
	/// </summary>
	/// <param name="color"></param>
	/// <returns></returns>
	public static string ColorToString(Color color)
	{
		var a = color.a;
		if (a < 0.5f)
			return "transparent";

		float H, S, V;
		Color.RGBToHSV(color, out H, out S, out V);

		// It's kinda magic. Check HSV pie representation to see reasoning behind this

		// Check if color is really dark
		if (V < 0.1f)
			return "black";

		// Check if we are inside "gray tube"
		if (S < 0.1f)
		{
			if (V > 0.9f)
				return "white";
			else
				return "gray";
		}

		var nearest = CommonColors.OrderBy(x => Mathf.Abs(x.Item1 - H)).First();
		var nearestColor = nearest.Item2;

		if (S < 0.4f)
			nearestColor = "bright " + nearestColor;
		else if (V < 0.4f)
			nearestColor = "dark " + nearestColor;

		if (ColorsExceptions.ContainsKey(nearestColor))
			nearestColor = ColorsExceptions[nearestColor];

		return nearestColor;
	}
}
