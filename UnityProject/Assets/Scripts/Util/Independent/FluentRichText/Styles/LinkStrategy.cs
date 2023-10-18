namespace Util.Independent.FluentRichText.Styles
{
	public class LinkStrategy : IStyleStrategy
	{
		private readonly string url;

		public LinkStrategy(string url)
		{
			this.url = url;
		}

		public string ApplyStyle(string text) => $"<link=\"{url}\">{text}</link>";
	}
}