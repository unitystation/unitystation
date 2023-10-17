namespace Util.Independent.FluentRichText.Styles
{
	public class CustomStyleStrategy: IStyleStrategy
	{
		private readonly string style;

		public CustomStyleStrategy(string style)
		{
			this.style = style;
		}

		public string ApplyStyle(string text) => $"<style=\"{style}\">{text}</style>";
	}
}