using Logs;

namespace Util.Independent.FluentRichText.Styles
{
	public class LineHeightStrategy : IStyleStrategy
	{
		private readonly string amount;

		public LineHeightStrategy(string amount)
		{
			if (CommonValidations.IsValidPixelValue(amount) == false
			    && CommonValidations.IsValidFontUnitValue(amount) == false
			    && CommonValidations.IsValidPercentageValue(amount) == false)
			{
				Loggy.LogError("RichText received invalid line height. Line height must be a pixel value (e.g., \"1\", \"2.5\"), a font unit value (e.g., \"1em\", \"-0.5em\"), or a percentage value (e.g., \"10%\", \"2.5%\").");
				return;
			}

			this.amount = amount;
		}

		public string ApplyStyle(string text)
		{
			return string.IsNullOrEmpty(amount) ? text : $"<line-height={amount}>{text}</line-height>";
		}
	}
}