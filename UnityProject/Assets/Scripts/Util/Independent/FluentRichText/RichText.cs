using System.Collections.Generic;
using System.Linq;
using System.Text;
using Util.Independent.FluentRichText.Styles;

namespace Util.Independent.FluentRichText
{
	/// <summary>
	/// This utility class helps with creating formatted text in TMP.
	/// </summary>
	public class RichText
	{
		private readonly StringBuilder builder;
		private readonly List<IStyleStrategy> styles = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="RichText"/> class with an empty content.
		/// </summary>
		public RichText()
		{
			builder = new StringBuilder();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RichText"/> class with the specified initial text.
		/// </summary>
		/// <param name="text">The initial text to set in the RichText.</param>
		public RichText(string text)
		{
			builder = new StringBuilder(text);
		}

		public static implicit operator string(RichText richText)
		{
			return richText.ToString();
		}

		/// <summary>
		/// Aligns the text according to the specified alignment.
		/// </summary>
		/// <param name="alignment">The desired text alignment (e.g., left, right, center).</param>
		/// <returns>The current RichText instance to allow method chaining.</returns>
		public RichText Align(Alignment alignment)
		{
			styles.Add(new AlignStrategy(alignment));
			return this;
		}

		/// <summary>
		/// Makes the text bold.
		/// </summary>
		/// <returns>The current RichText instance to allow method chaining.</returns>
		public RichText Bold()
		{
			styles.Add(new BoldStrategy());
			return this;
		}

		/// <summary>
		/// Makes the text italic.
		/// </summary>
		/// <returns>The current RichText instance to allow method chaining.</returns>
		public RichText Italic()
		{
			styles.Add(new ItalicStrategy());
			return this;
		}

		/// <summary>
		/// Adjusts the spacing between characters based on the given amount. This spacing can be specified in pixels, font units, or percentages.
		/// A positive value increases the spacing, separating the characters, whereas a negative value decreases it, bringing them closer together.
		/// </summary>
		/// <param name="amount">The desired spacing value. Accepts pixel values (e.g., "12", "12.4"), font unit values (e.g., "12em", "12.4em"), or percentages (e.g., "12%", "12.4%").</param>
		/// <returns>The current RichText instance to allow for method chaining.</returns>
		public RichText Spacing(string amount)
		{
			styles.Add(new SpacingStrategy(amount));
			return this;
		}

		/// <summary>
		/// Makes the text colored according to the specified named color.
		/// </summary>
		/// <param name="namedColor">What color to use.</param>
		/// <returns>The current RichText instance to allow for method chaining.</returns>
		public RichText Color(Color namedColor)
		{
			styles.Add(new ColorStrategy(namedColor));
			return this;
		}

		/// <summary>
		/// Makes the text colored according to the specified named color.
		/// </summary>
		/// <param name="hexColor">What color to use in hexadecimal format. It should start with a #</param>
		/// <returns>The current RichText instance to allow for method chaining.</returns>
		public RichText Color(string hexColor)
		{
			styles.Add(new ColorStrategy(hexColor));
			return this;
		}

		/// <summary>
		/// Sets the font for the text based on the provided font name.
		/// </summary>
		/// <param name="fontName">The name of the font to apply to the text.</param>
		/// <returns>The current RichText instance to allow for method chaining.</returns>
		public RichText Font(string fontName)
		{
			styles.Add(new FontStrategy(fontName));
			return this;
		}

		/// <summary>
		/// Indents the text by the specified amount, which persists across lines. Useful for creating layouts like bullet points compatible with word-wrapping.
		/// </summary>
		/// <param name="amount">The indentation value. Accepts pixel values (e.g., "12", "12.4"), font unit values (e.g., "12em", "12.4em"), or percentages (e.g., "12%", "12.4%").</param>
		public RichText Indent(string amount)
		{
			styles.Add(new IndentStrategy(amount));
			return this;
		}

		/// <summary>
		/// Adjusts the line height for the text based on the specified amount, affecting the vertical spacing between lines.
		/// </summary>
		/// <param name="amount">The desired line height value. Accepts pixel values (e.g., "12", "12.4"), font unit values (e.g., "12em", "12.4em"), or percentages (e.g., "12%", "12.4%").</param>
		/// <returns>The current RichText instance to allow for method chaining.</returns>
		public RichText LineHeight(string amount)
		{
			styles.Add(new LineHeightStrategy(amount));
			return this;
		}

		/// <summary>
		/// Inserts horizontal space directly after it, and before the start of each new line. It only affects manual line breaks, not word-wrapped lines. You can use pixels, font units, or percentages.
		/// </summary>
		/// <param name="amount">Amount of line indentation. Accepts pixel values (e.g., "12", "12.4"), font unit values (e.g., "12em", "12.4em"), or percentages (e.g., "12%", "12.4%").</param>
		/// <returns></returns>
		public RichText LineIndentation(string amount)
		{
			styles.Add(new LineIndentationStrategy(amount));
			return this;
		}

		/// <summary>
		/// Adds an unstyled hyperlink to the current content using the provided URL.
		/// </summary>
		/// <param name="url">The URL to which the hyperlink will point.</param>
		/// <returns>The current RichText instance to allow for method chaining.</returns>
		public RichText UnStyledLink(string url)
		{
			styles.Add(new LinkStrategy(url));
			return this;
		}

		/// <summary>
		/// Makes the text lowercase
		/// </summary>
		/// <returns></returns>
		public RichText LowerCase()
		{
			styles.Add(new LowerCaseStrategy());
			return this;
		}

		/// <summary>
		/// Makes the text uppercase
		/// </summary>
		/// <returns></returns>
		public RichText UpperCase()
		{
			styles.Add(new UpperCaseStrategy());
			return this;
		}

		/// <summary>
		/// Makes the text small caps. This means that all characters will be uppercase but the ones that were lowercase will be smaller in font size.
		/// </summary>
		/// <returns></returns>
		public RichText SmallCaps()
		{
			styles.Add(new SmallCapsStrategy());
			return this;
		}

		/// <summary>
		///	adds an overlay on top of the text. You can use this to highlight portions of your text.
		/// Because the markings lay on top of the text, you have to give them a semitransparent color to still be able
		/// to see the text.
		///
		/// Marks tags don't stack, they replace each other.
		/// </summary>
		/// <param name="hexColor">Color in hexadecimal format.</param>
		/// <returns></returns>
		public RichText Mark(string hexColor)
		{
			styles.Add(new MarkStrategy(hexColor));
			return this;
		}

		/// <summary>
		///  This will force all characters to claim the same horizontal space. You can use pixels or font units to set the monospace character width.
		/// </summary>
		/// <param name="characterWidth">Horizontal space the character will use. Accepts pixels (eg. 1, 1.2) or font units (eg. 1em, 1.2em)</param>
		/// <returns></returns>
		public RichText Monospace(string characterWidth)
		{
			styles.Add(new MonospaceStrategy(characterWidth));
			return this;
		}

		/// <summary>
		/// Wraps the text in a tag that will make it so any inner tag isn't parsed by TMP interpreter.
		/// Useful if there are things that might be interpreted as tags but you don't want them to be.
		/// </summary>
		/// <returns></returns>
		public RichText NoParse()
		{
			styles.Add(new NoParseStrategy());
			return this;
		}

		/// <summary>
		/// Wraps the text in a tag that will prevent the word wrapper from separating the word into different lines.
		/// </summary>
		/// <returns></returns>
		public RichText NonBreakingSpaces()
		{
			styles.Add(new NonBreakingSpacesStrategy());
			return this;
		}

		/// <summary>
		/// Gives you direct control over the horizontal caret position. You can put it anywhere on the same line,
		/// regardless where it started. You can use either pixels, font units, or percentages.
		/// This tags is best used with left alignment.
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public RichText HorizontalPosition(string position)
		{
			styles.Add(new HorizontalPositionStrategy(position));
			return this;
		}

		/// <summary>
		/// Changes the size of the font. You can use pixels, font units, or percentages.
		/// </summary>
		/// <param name="size"></param>
		/// <returns></returns>
		public RichText FontSize(string size)
		{
			styles.Add(new FontSizeStrategy(size));
			return this;
		}

		/// <summary>
		/// Appends a space tag to the current content. You can use pixels, font units, or percentages.
		/// </summary>
		/// <param name="amount"></param>
		/// <remarks>Note this style doesn't get added to the stack and immediately appends the tag instead.</remarks>
		/// <returns></returns>
		public RichText Space(string amount)
		{
			builder.Append(new SpaceStrategy(amount).ApplyStyle(string.Empty));
			return this;
		}

		/// <summary>
		/// Appends a sprite tag to the current content.
		/// </summary>
		/// <param name="index">Index of the sprite in the default atlas.</param>
		/// <remarks>Note this style doesn't get added to the stack and immediately appends the tag instead.</remarks>
		/// <returns></returns>
		public RichText Sprite(int index)
		{
			builder.Append(new SpriteStrategy(index).ApplyStyle(string.Empty));
			return this;
		}

		/// <summary>
		/// Appends a sprite tag to the current content.
		/// </summary>
		/// <param name="name">name of the sprite in the default atlas.</param>
		/// <remarks>Note this style doesn't get added to the stack and immediately appends the tag instead.</remarks>
		/// <returns></returns>
		public RichText Sprite(string name)
		{
			builder.Append(new SpriteStrategy(name).ApplyStyle(string.Empty));
			return this;
		}

		/// <summary>
		/// Appends a sprite tag to the current content.
		/// </summary>
		/// <param name="atlas">asset name of the atlas from which we will take the sprite</param>
		/// <param name="index">Index of the sprite in the selected atlas.</param>
		/// <remarks>Note this style doesn't get added to the stack and immediately appends the tag instead.</remarks>
		/// <returns></returns>
		public RichText Sprite(string atlas, int index)
		{
			builder.Append(new SpriteStrategy(atlas, index).ApplyStyle(string.Empty));
			return this;
		}

		/// <summary>
		/// Appends a sprite tag to the current content.
		/// </summary>
		/// <param name="atlas">asset name of the atlas from which we will take the sprite</param>
		/// <param name="name">name of the sprite in the selected atlas.</param>
		/// <remarks>Note this style doesn't get added to the stack and immediately appends the tag instead.</remarks>
		/// <returns></returns>
		public RichText Sprite(string atlas, string name)
		{
			builder.Append(new SpriteStrategy(atlas, name).ApplyStyle(string.Empty));
			return this;
		}

		/// <summary>
		/// Adds a horizontal line that crosses the text.
		/// </summary>
		/// <returns></returns>
		public RichText Strikethrough()
		{
			styles.Add(new StrikethroughStrategy());
			return this;
		}

		/// <summary>
		/// Adds an underline to the text.
		/// </summary>
		/// <returns></returns>
		public RichText Underline()
		{
			styles.Add(new UnderlineStrategy());
			return this;
		}

		/// <summary>
		/// Wraps the text in a tag that will apply the specified style to it.
		/// </summary>
		/// <param name="style"></param>
		/// <returns></returns>
		public RichText CustomStyle(string style)
		{
			styles.Add(new CustomStyleStrategy(style));
			return this;
		}

		/// <summary>
		/// Wraps the text in the subscript tag. This will make the text smaller and lower than the rest of the text.
		/// </summary>
		/// <returns></returns>
		public RichText Subscript()
		{
			styles.Add(new SubscriptStrategy());
			return this;
		}

		/// <summary>
		/// Wraps the text in the superscript tag. This will make the text smaller and higher than the rest of the text.
		/// </summary>
		/// <returns></returns>
		public RichText SuperScript()
		{
			styles.Add(new SuperscriptStrategy());
			return this;
		}

		/// <summary>
		/// Adds an offset to the vertical position of the text. You can use pixels, font units, or percentages.
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		public RichText VerticalOffset(string amount)
		{
			styles.Add(new VerticalOffsetStrategy(amount));
			return this;
		}

		/// <summary>
		/// Modifies the width of the text. You can use pixels, font units, or percentages.
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		public RichText Width(string amount)
		{
			styles.Add(new WidthStrategy(amount));
			return this;
		}

		/// <summary>
		/// Applies accumulated styles to the specified text and then appends it to the current content. After application, the style stack is cleared.
		/// </summary>
		/// <param name="text">The text to which the accumulated styles will be applied.</param>
		/// <returns>The current RichText instance to allow for method chaining.</returns>
		public RichText ApplyTo(string text)
		{
			foreach (IStyleStrategy style in styles)
			{
				text = style.ApplyStyle(text);
			}

			builder.Append(text);
			styles.Clear();
			return this;
		}

		/// <summary>
		/// Appends the provided text to the current content without applying any additional styles.
		/// </summary>
		/// <param name="text">The text to be appended.</param>
		/// <returns>The current RichText instance to allow for method chaining.</returns>
		public RichText Add(string text)
		{
			builder.Append(text);
			return this;
		}

		/// <summary>
		/// Appends a formatted string to the current content using the specified format template and arguments.
		/// </summary>
		/// <param name="template">A composite format string.</param>
		/// <param name="text">An array of objects to format and then append.</param>
		/// <returns>The current RichText instance to allow for method chaining.</returns>
		public RichText AddFormat(string template, params object[] text)
		{
			builder.AppendFormat(template, text.ToArray());
			return this;
		}

		/// <summary>
		/// Same as <see cref="Add"/> but adds a newline character before the text.
		/// </summary>
		///
		/// <param name="text"></param>
		/// <returns></returns>
		public RichText AddOnNewLine(string text)
		{
			builder.Append("\n").Append(text);
			return this;
		}

		/// <summary>
		/// Same as <see cref="AddFormat"/> but adds a newline character before the text.
		/// </summary>
		/// <param name="template"></param>
		/// <param name="text"></param>
		/// <returns></returns>
		public RichText AddFormatOnNewLine(string template, params object[] text)
		{
			builder.Append("\n").AppendFormat(template, text.ToArray());
			return this;
		}
	}

	public enum Alignment
	{
		Left,
		Center,
		Right
	}

	public enum Color
	{
		NoColor,
		Black,
		Blue,
		Green,
		Orange,
		Purple,
		Red,
		White,
		Yellow
	}

	public enum MarginDirection
	{
		All,
		Left,
		Right
	}
}