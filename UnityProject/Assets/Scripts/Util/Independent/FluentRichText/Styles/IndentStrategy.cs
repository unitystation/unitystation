using Logs;

namespace Util.Independent.FluentRichText.Styles
{
	public class IndentStrategy : IStyleStrategy
	{
		private readonly string indentValue;

		public IndentStrategy(string indentValue)
		{
			if (CommonValidations.IsValidPixelValue(indentValue) == false
			    && CommonValidations.IsValidFontUnitValue(indentValue) == false
			    && CommonValidations.IsValidPercentageValue(indentValue) == false)
			{
				Loggy.LogError("RichText received invalid indent. Indent must be a pixel value (e.g., \"1\", \"2.5\"), a font unit value (e.g., \"1em\", \"-0.5em\"), or a percentage value (e.g., \"10%\", \"2.5%\").");
				this.indentValue = null;
				return;
			}

			this.indentValue = indentValue;
		}

		public string ApplyStyle(string text)
		{
			return string.IsNullOrEmpty(indentValue) ? text : $"<indent={indentValue}>{text}</indent>";
		}
	}
}