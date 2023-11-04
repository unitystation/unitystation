using System.Text.RegularExpressions;

namespace Util.Independent.FluentRichText.Styles
{
	public static class CommonValidations
	{
		public static bool IsValidPixelValue(string value)
		{
			return Regex.IsMatch(value, @"^-?\+?\d+(\.\d+)?$");
		}

		public static  bool IsValidFontUnitValue(string value)
		{
			return Regex.IsMatch(value, @"^-?\+?\d+(\.\d+)?em$");
		}

		public static bool IsValidPercentageValue(string value)
		{
			return Regex.IsMatch(value, @"^-?\+?\d+(\.\d+)?%$");
		}

		public static bool IsValidHexColor(string hexColor)
		{
			return Regex.IsMatch(hexColor, @"^#([a-fA-F0-9]{6}|[a-fA-F0-9]{8})$");
		}
	}
}