using Logs;

namespace Util.Independent.FluentRichText.Styles
{
	public class ColorStrategy : IStyleStrategy
	{
		private readonly string hexColor;
		private readonly Color color = Color.NoColor;

		public ColorStrategy(string hexColor)
		{
			if (CommonValidations.IsValidHexColor(hexColor) == false)
			{
				Loggy.LogError($"RichText received invalid hexadecimal color: {hexColor}.");
				this.hexColor = null;
				return;
			}

			this.hexColor = hexColor;
		}

		public ColorStrategy(Color color)
		{
			this.color = color;
		}

		public string ApplyStyle(string text)
		{
			string colorString;

			if (!string.IsNullOrEmpty(hexColor))
			{
				// If it's a hexadecimal color, use it as is.
				colorString = hexColor;
			}
			else if (color != Color.NoColor)
			{
				// If it's a named color, convert to lowercase and wrap in quotes.
				colorString = $"\"{color.ToString().ToLower()}\"";
			}
			else
			{
				colorString = null;
			}

			return string.IsNullOrEmpty(colorString) ? text : $"<color={colorString}>{text}</color>";
		}
	}
}