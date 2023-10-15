namespace Util.Independent.FluentRichText.Styles
{
	public class UnderlineStrategy: IStyleStrategy
	{
		public string ApplyStyle(string text) => $"<u>{text}</u>";
	}
}