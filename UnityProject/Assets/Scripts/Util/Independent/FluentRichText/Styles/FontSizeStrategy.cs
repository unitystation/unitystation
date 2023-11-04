using Logs;

namespace Util.Independent.FluentRichText.Styles
{
	public class FontSizeStrategy: IStyleStrategy
	{
		private readonly string size;

		public FontSizeStrategy(string size)
		{
			if (CommonValidations.IsValidPixelValue(size) == false &&
			    CommonValidations.IsValidFontUnitValue(size) == false &&
			    CommonValidations.IsValidPercentageValue(size) == false)
			{
				Loggy.LogError($"RichText received invalid font size value: {size}");
				return;
			}

			this.size = size;
		}

		public string ApplyStyle(string text)
		{
			return string.IsNullOrEmpty(size) ? text : $"<size={size}>{text}</size>";
		}
	}
}