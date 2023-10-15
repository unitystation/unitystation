namespace Util.Independent.FluentRichText.Styles
{
	public class NoParseStrategy: IStyleStrategy
	{
		public string ApplyStyle(string text) => $"<noparse>{text}</noparse>";
	}
}