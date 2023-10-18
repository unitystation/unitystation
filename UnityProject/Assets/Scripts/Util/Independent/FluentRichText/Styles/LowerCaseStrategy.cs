namespace Util.Independent.FluentRichText.Styles
{
	public class LowerCaseStrategy : IStyleStrategy
	{
		public string ApplyStyle(string text) => $"<lowercase>{text}</lowercase>";
	}
}