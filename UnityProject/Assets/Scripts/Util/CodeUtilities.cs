using System;

public static class CodeUtilities
{
	public static string RemoveClone(this string text) => text?.Replace("(Clone)", string.Empty);

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
