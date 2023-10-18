using Logs;

namespace Util.Independent.FluentRichText.Styles
{
	public class SpaceStrategy: IStyleStrategy
	{
		private readonly string amount;

		public SpaceStrategy(string amount)
		{
			if (CommonValidations.IsValidPixelValue(amount) == false &&
			    CommonValidations.IsValidFontUnitValue(amount) == false &&
			    CommonValidations.IsValidPercentageValue(amount) == false)
			{
				Loggy.LogError($"RichText received invalid space amount value: {amount}");
				return;
			}

			this.amount = amount;
		}

		public string ApplyStyle(string text)
		{
			return string.IsNullOrEmpty(amount) ? text : $"{text}<space={amount}>";
		}
	}
}