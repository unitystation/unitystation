namespace Util.Independent.FluentRichText.Styles
{
	public class SubscriptStrategy: IStyleStrategy
	{
		public string ApplyStyle(string text) => $"<sub>{text}</sub>";
	}
}