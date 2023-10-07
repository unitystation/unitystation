using System;
using Logs;

namespace Util.Independent.FluentRichText.Styles
{
	public class MarginStrategy : IStyleStrategy
	{
		private readonly MarginDirection direction;
		private readonly string amount;

		public MarginStrategy(string amount)
		{
			if (ValidateAmount(amount) == false) return;

			this.amount = amount;
			direction = MarginDirection.All;
		}

		public MarginStrategy(string amount, MarginDirection direction)
		{
			if (ValidateAmount(amount) == false) return;

			this.amount = amount;
			this.direction = direction;
		}

		private static bool ValidateAmount(string amount)
		{
			if (CommonValidations.IsValidPixelValue(amount) ||
			    CommonValidations.IsValidFontUnitValue(amount) ||
			    CommonValidations.IsValidPercentageValue(amount))
			{
				return true;
			}

			Loggy.LogError(
				"RichText received invalid margin. Margin must be a pixel value (e.g., \"1\", \"2.5\"), a font unit value (e.g., \"1em\", \"-0.5em\"), or a percentage value (e.g., \"10%\", \"2.5%\").");
			return false;
		}

		public string ApplyStyle(string text)
		{
			if (string.IsNullOrEmpty(amount)) return text;

			const string allDirectionTemplate = "<margin={0}>{1}</margin>";
			const string directionTemplate = "<margin-{0}={1}>{2}</margin-{0}>";

			return direction switch
			{
				MarginDirection.All => string.Format(allDirectionTemplate, amount, text),
				MarginDirection.Right => string.Format(directionTemplate, "right", amount, text),
				MarginDirection.Left => string.Format(directionTemplate, "left", amount, text),
				_ => throw new ArgumentOutOfRangeException()
			};
		}
	}
}