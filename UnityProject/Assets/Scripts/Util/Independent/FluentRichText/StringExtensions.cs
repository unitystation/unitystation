using Util.Independent.FluentRichText.Styles;

namespace Util.Independent.FluentRichText
{
	public static class StringExtensions
	{
		public static string Align(this string text, Alignment alignment) => new AlignStrategy(alignment).ApplyStyle(text);
		public static string Bold(this string text) => new BoldStrategy().ApplyStyle(text);
		public static string Italic(this string text) => new ItalicStrategy().ApplyStyle(text);
		public static string Spacing(this string text, string amount) => new SpacingStrategy(amount).ApplyStyle(text);
		public static string Color(this string text, Color namedColor) => new ColorStrategy(namedColor).ApplyStyle(text);
		public static string Color(this string text, string hexColor) => new ColorStrategy(hexColor).ApplyStyle(text);
		public static string Font(this string text, string fontName) => new FontStrategy(fontName).ApplyStyle(text);
		public static string Indent(this string text, string amount) => new IndentStrategy(amount).ApplyStyle(text);
		public static string LineHeight(this string text, string amount) => new LineHeightStrategy(amount).ApplyStyle(text);
		public static string UnStyledLink(this string text, string url) => new LinkStrategy(url).ApplyStyle(text);
		public static string TmpLowercase(this string text) => new LowerCaseStrategy().ApplyStyle(text);
		public static string TmpUppercase(this string text) => new UpperCaseStrategy().ApplyStyle(text);
		public static string SmallCaps(this string text) => new SmallCapsStrategy().ApplyStyle(text);
		public static string Mark(this string text, string hexColor) => new MarkStrategy(hexColor).ApplyStyle(text);
		public static string Monospace(this string text, string characterWidth) => new MonospaceStrategy(characterWidth).ApplyStyle(text);
		public static string NoParse(this string text) => new NoParseStrategy().ApplyStyle(text);
		public static string NonBreakingSpaces(this string text) => new NonBreakingSpacesStrategy().ApplyStyle(text);
		public static string HorizontalPosition(this string text, string position) => new HorizontalPositionStrategy(position).ApplyStyle(text);
		public static string FontSize(this string text, string size) => new FontStrategy(size).ApplyStyle(text);
		public static string Space(this string text, string amount) => new SpaceStrategy(amount).ApplyStyle(text);
		public static string Sprite(this string text, int index) => new SpriteStrategy(index).ApplyStyle(text);
		public static string Sprite(this string text, string name) => new SpriteStrategy(name).ApplyStyle(text);
		public static string Sprite(this string text, string atlas, int index) => new SpriteStrategy(atlas, index).ApplyStyle(text);
		public static string Sprite(this string text, string atlas, string name) => new SpriteStrategy(atlas, name).ApplyStyle(text);
		public static string Strikethrough(this string text) => new StrikethroughStrategy().ApplyStyle(text);
		public static string Underline(this string text) => new UnderlineStrategy().ApplyStyle(text);
		public static string CustomStyle(this string text, string style) => new CustomStyleStrategy(style).ApplyStyle(text);
		public static string Subscript(this string text) => new SubscriptStrategy().ApplyStyle(text);
		public static string Superscript(this string text) => new SuperscriptStrategy().ApplyStyle(text);
		public static string VerticalOffset(this string text, string amount) => new VerticalOffsetStrategy(amount).ApplyStyle(text);
		public static string Width(this string text, string amount) => new WidthStrategy(amount).ApplyStyle(text);
	}
}