namespace Util.Independent.FluentRichText.Styles
{
	public class StrikethroughStrategy: IStyleStrategy
	{
		public string ApplyStyle(string text) => $"<s>{text}</s>";
	}
}