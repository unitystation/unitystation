namespace Util.Independent.FluentRichText.Styles
{
	public class ItalicStrategy : IStyleStrategy
	{
		public string ApplyStyle(string text) => $"<i>{text}</i>";
	}
}