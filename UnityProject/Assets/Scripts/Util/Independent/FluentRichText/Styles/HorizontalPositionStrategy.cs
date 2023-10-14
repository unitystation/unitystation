using C5;
using Logs;

namespace Util.Independent.FluentRichText.Styles
{
	public class HorizontalPositionStrategy: IStyleStrategy
	{
		private readonly string position;

		public HorizontalPositionStrategy(string position)
		{
			if (CommonValidations.IsValidPixelValue(position) == false &&
			    CommonValidations.IsValidFontUnitValue(position) == false &&
			    CommonValidations.IsValidPercentageValue(position) == false)
			{
				Loggy.LogError($"RichText received invalid horizontal position value: {position}" );
				return;
			}

			this.position = position;
		}

		public string ApplyStyle(string text)
		{
			return string.IsNullOrEmpty(position) ? text : $"<pos={position}>{text}</pos>";
		}
	}
}