namespace Util.Independent.FluentRichText.Styles
{
	public class NonBreakingSpacesStrategy: IStyleStrategy
	{
		public string ApplyStyle(string text) => $"<nobr>{text}</nobr>";
	}
}