using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CodeUtilities
{
	public static string GetUntilOrEmpty(this string text, string stopAt = "-")
	{
		if (!String.IsNullOrWhiteSpace(text))
		{
			int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);

			if (charLocation > 0)
			{
				return text.Substring(0, charLocation);
			}
		}

		return String.Empty;
	}
}
