namespace Util.Independent.FluentRichText.Styles
{
	public class SuperscriptStrategy: IStyleStrategy
	{
		public string ApplyStyle(string text) => $"<sup>{text}</sup>";
	}
}