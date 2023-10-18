namespace Util.Independent.FluentRichText.Styles
{
	public class BoldStrategy : IStyleStrategy
	{
		public string ApplyStyle(string text) => $"<b>{text}</b>";
	}
}