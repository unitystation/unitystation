using Logs;

namespace Util.Independent.FluentRichText.Styles
{
	public class MarkStrategy : IStyleStrategy
	{
		private readonly string hexColor;

		public MarkStrategy(string hexColor)
		{
			if (CommonValidations.IsValidHexColor(hexColor) == false)
			{
				Loggy.LogError($"RichText received invalid hexadecimal color: {hexColor}.");
				this.hexColor = null;
				return;
			}

			this.hexColor = hexColor;
		}

		public string ApplyStyle(string text)
		{
			return string.IsNullOrEmpty(hexColor) ? text : $"<mark={hexColor}>{text}</mark>";
		}
	}
}