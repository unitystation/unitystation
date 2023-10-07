namespace Util.Independent.FluentRichText.Styles
{
	public class AlignStrategy : IStyleStrategy
	{
		private readonly Alignment alignment;

		public AlignStrategy(Alignment alignment)
		{
			this.alignment = alignment;
		}

		public string ApplyStyle(string text) => $"<align=\"{alignment}\">{text}</align>";
	}
}