namespace Util.Independent.FluentRichText.Styles
{
	public class FontStrategy : IStyleStrategy
	{
		private readonly string fontName;

		public FontStrategy(string fontName)
		{
			this.fontName = fontName;
		}

		public string ApplyStyle(string text) => $"<font=\"{fontName}\">{text}</font>";
	}
}