using Logs;

namespace Util.Independent.FluentRichText.Styles
{
	public class ColorStrategy : IStyleStrategy
	{
		private readonly string hexColor;
		private readonly RichTextColor _richTextColor = RichTextColor.NoColor;

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

		public ColorStrategy(RichTextColor richTextColor)
		{
			this._richTextColor = richTextColor;
		}

		public string ApplyStyle(string text)
		{
			string colorString;

			if (!string.IsNullOrEmpty(hexColor))
			{
				// If it's a hexadecimal color, use it as is.
				colorString = hexColor;
			}
			else if (_richTextColor != RichTextColor.NoColor)
			{
				// If it's a named color, convert to lowercase and wrap in quotes.
				colorString = $"\"{_richTextColor.ToString().ToLower()}\"";
			}
			else
			{
				colorString = null;
			}

			return string.IsNullOrEmpty(colorString) ? text : $"<color={colorString}>{text}</color>";
		}
	}
}