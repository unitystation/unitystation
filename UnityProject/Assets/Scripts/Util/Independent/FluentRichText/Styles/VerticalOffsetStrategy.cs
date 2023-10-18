using Logs;

namespace Util.Independent.FluentRichText.Styles
{
	public class VerticalOffsetStrategy: IStyleStrategy
	{
		private readonly string amount;

		public VerticalOffsetStrategy(string amount)
		{
			if (CommonValidations.IsValidPixelValue(amount) == false &&
			    CommonValidations.IsValidFontUnitValue(amount) == false &&
			    CommonValidations.IsValidPercentageValue(amount) == false)
			{
				Loggy.LogError($"RichText received invalid vertical offset amount: {amount}");
				return;
			}

			this.amount = amount;
		}

		public string ApplyStyle(string text)
		{
			return string.IsNullOrEmpty(amount) ? text : $"<voffset={amount}>{text}</voffset>";
		}
	}
}