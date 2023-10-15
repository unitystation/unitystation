using Logs;

namespace Util.Independent.FluentRichText.Styles
{
	public class MonospaceStrategy: IStyleStrategy
	{
		private readonly string characterWidth;

		public MonospaceStrategy(string characterWidth)
		{
			if (characterWidth.StartsWith("-"))
			{
				Loggy.LogError("RichText received a negative character width for monospace. This is not allowed.");
				return;
			}

			if (CommonValidations.IsValidPixelValue(characterWidth) == false &&
			    CommonValidations.IsValidFontUnitValue(characterWidth) == false)
			{
				Loggy.LogError($"RichText received an invalid character width for monospace: {characterWidth}");
				return;
			}

			this.characterWidth = characterWidth;
		}

		public string ApplyStyle(string text)
		{
			return string.IsNullOrWhiteSpace(characterWidth) ? text : $"<mspace={characterWidth}>{text}</mspace>";
		}
	}
}