namespace Util.Independent.FluentRichText.Styles
{
	public class UpperCaseStrategy : IStyleStrategy
	{
		public string ApplyStyle(string text) => $"<uppercase>{text}</uppercase>";
	}
}