namespace Util.Independent.FluentRichText.Styles
{
	public class SmallCapsStrategy : IStyleStrategy
	{
		public string ApplyStyle(string text) => $"<smallcaps>{text}</smallcaps>";
	}
}