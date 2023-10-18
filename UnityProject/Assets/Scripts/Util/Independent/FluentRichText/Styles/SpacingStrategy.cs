using Logs;

namespace Util.Independent.FluentRichText.Styles
{
	public class SpacingStrategy : IStyleStrategy
	{
		private readonly string spacing;

		public SpacingStrategy(string spacing)
		{
			if (CommonValidations.IsValidPixelValue(spacing) == false &&
			    CommonValidations.IsValidFontUnitValue(spacing) == false)
			{
				Loggy.LogError(
					$"RichText received invalid spacing: {spacing}. Spacing must be a pixel value (e.g., \"1\", \"2.5\") or a font unit value (e.g., \"1em\", \"-0.5em\").");
				this.spacing = null;
				return;
			}

			this.spacing = spacing;
		}

		public string ApplyStyle(string text)
		{
			return string.IsNullOrEmpty(spacing) ? text : $"<cspace={spacing}>{text}</cspace>";
		}
	}
}