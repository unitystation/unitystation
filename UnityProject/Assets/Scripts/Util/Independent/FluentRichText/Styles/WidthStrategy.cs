using Logs;

namespace Util.Independent.FluentRichText.Styles
{
	public class WidthStrategy: IStyleStrategy
	{
		private readonly string amount;

		public WidthStrategy(string amount)
		{
			if (CommonValidations.IsValidPixelValue(amount) == false &&
			    CommonValidations.IsValidFontUnitValue(amount) == false &&
			    CommonValidations.IsValidPercentageValue(amount) == false)
			{
				Loggy.LogError($"RichText received invalid width amount: {amount}");
				return;
			}

			this.amount = amount;
		}

		public string ApplyStyle(string text)
		{
			return string.IsNullOrEmpty(amount) ? text : $"<width={amount}>{text}</width>";
		}
	}
}