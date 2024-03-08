using System;
using NUnit.Framework;
using UnityEngine.TestTools;
using Util.Independent.FluentRichText;
using Util.Independent.FluentRichText.Styles;

namespace Tests.RichTextTests
{
	public class StylesTests
	{

		[TestCase(Alignment.Left)]
		[TestCase(Alignment.Center)]
		[TestCase(Alignment.Right)]
		public void AlignStrategyReturnsAlignedText(Alignment alignment)
		{
			AlignStrategy strategy = new(alignment);
			const string text = "text";
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<align=\"{alignment}\">{text}</align>", actual);
		}


		public void BoldStrategyReturnsBoldText()
		{
			BoldStrategy strategy = new();
			const string text = "text";
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<b>{text}</b>", actual);
		}


		public void ItalicStrategyReturnsItalicText()
		{
			ItalicStrategy strategy = new();
			const string text = "text";
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<i>{text}</i>", actual);
		}


		public void FontStrategyReturnsTextWithFontName()
		{
			const string text = "text";
			const string font = "Arial";
			FontStrategy strategy = new(font);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<font=\"{font}\">{text}</font>", actual);
		}


		[TestCase("1")]
		[TestCase("2.5")]
		[TestCase("1em")]
		[TestCase("-0.5em")]
		public void ValidSpacingShouldReturnSpacedText(string validSpacing)
		{
			SpacingStrategy strategy = new(validSpacing);
			const string text = "text";
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<cspace={validSpacing}>{text}</cspace>", actual);
		}


		[TestCase("1.0.0")]
		[TestCase("not a number")]
		[TestCase("1.0.0em")]
		public void InvalidSpacingShouldReturnUnmodifiedText(string invalidSpacing)
		{
			LogAssert.ignoreFailingMessages = true;
			SpacingStrategy strategy = new(invalidSpacing);
			const string text = "text";
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual(text, actual);
		}


		[TestCase(0)]
		[TestCase(1)]
		[TestCase(99)]
		[TestCase(100)]
		public void ValidAlphaShouldReturnTextWithAlpha(int validAlpha)
		{
			const string text = "text";
			AlphaStrategy strategy = new(validAlpha);
			string actual = strategy.ApplyStyle(text);
			int alphaValue = (int)Math.Round(255 * (validAlpha / 100.0), MidpointRounding.AwayFromZero);
			Assert.AreEqual($"<alpha=#{alphaValue:X2}>{text}</alpha>", actual);
		}


		[TestCase(-1)]
		[TestCase(101)]
		public void InvalidAlphaShouldReturnUnmodifiedText(int invalidAlpha)
		{
			LogAssert.ignoreFailingMessages = true;
			const string text = "text";
			AlphaStrategy strategy = new(invalidAlpha);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual(text, actual);
		}


		[TestCase(RichTextColor.Black)]
		[TestCase(RichTextColor.Blue)]
		[TestCase(RichTextColor.Green)]
		[TestCase(RichTextColor.Red)]
		[TestCase(RichTextColor.Orange)]
		[TestCase(RichTextColor.Purple)]
		[TestCase(RichTextColor.White)]
		[TestCase(RichTextColor.Yellow)]
		public void NamedColorShouldReturnColoredText(RichTextColor namedRichTextColor)
		{
			const string text = "text";
			ColorStrategy strategy = new(namedRichTextColor);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<color=\"{namedRichTextColor.ToString().ToLower()}\">{text}</color>", actual);
		}


		[TestCase("#FF000088")]
		[TestCase("#005500")]
		[TestCase("#000000")]
		public void ValidHexColorShouldReturnColoredText(string validHexColor)
		{
			const string text = "text";
			ColorStrategy strategy = new(validHexColor);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<color={validHexColor}>{text}</color>", actual);
		}


		[TestCase("not a color")]
		[TestCase("FF0000")]
		[TestCase("#FF0000F")]
		public void InvalidHexColorShouldReturnUnmodifiedText(string invalidHexColor)
		{
			LogAssert.ignoreFailingMessages = true;
			const string text = "text";
			ColorStrategy strategy = new(invalidHexColor);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual(text, actual);
		}


		[TestCase("1")]
		[TestCase("2.5")]
		[TestCase("1em")]
		[TestCase("-0.5em")]
		[TestCase("15%")]
		public void ValidIndentShouldReturnIndentedText(string validIndent)
		{
			const string text = "text";
			IndentStrategy strategy = new(validIndent);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<indent={validIndent}>{text}</indent>", actual);
		}


		[TestCase("not a number")]
		[TestCase("1.0.0")]
		[TestCase("1.0.0em")]
		[TestCase("%15")]
		public void InvalidIndentShouldReturnUnmodifiedText(string invalidIndent)
		{
			LogAssert.ignoreFailingMessages = true;
			const string text = "text";
			IndentStrategy strategy = new(invalidIndent);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual(text, actual);
		}


		[TestCase("1")]
		[TestCase("2.5")]
		[TestCase("1em")]
		[TestCase("-0.5em")]
		[TestCase("15%")]
		public void ValidLineHeightShouldReturnTextWithLineHeight(string validLineHeight)
		{
			const string text = "text";
			LineHeightStrategy strategy = new(validLineHeight);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<line-height={validLineHeight}>{text}</line-height>", actual);
		}


		[TestCase("not a number")]
		[TestCase("1.0.0")]
		[TestCase("1.0.0em")]
		[TestCase("%15")]
		public void InvalidLineHeightShouldReturnUnmodifiedText(string invalidLineHeight)
		{
			LogAssert.ignoreFailingMessages = true;
			const string text = "text";
			LineHeightStrategy strategy = new(invalidLineHeight);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual(text, actual);
		}


		[TestCase("1")]
		[TestCase("2.5")]
		[TestCase("1em")]
		[TestCase("-0.5em")]
		[TestCase("15%")]
		public void ValidLineIndentationReturnsIndentedText(string validLineIndentation)
		{
			const string text = "text";
			LineIndentationStrategy strategy = new(validLineIndentation);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<line-indentation={validLineIndentation}>{text}</line-indentation>", actual);
		}


		[TestCase("not a number")]
		[TestCase("1.0.0")]
		[TestCase("1.0.0em")]
		[TestCase("%15")]
		public void InvalidLineIndentationShouldReturnUnmodifiedText(string invalidLineIndentation)
		{
			LogAssert.ignoreFailingMessages = true;
			const string text = "text";
			LineIndentationStrategy strategy = new(invalidLineIndentation);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual(text, actual);
		}


		public void LinkShouldReturnTextWithLink()
		{
			const string text = "text";
			const string url = "https://www.google.com";
			LinkStrategy strategy = new(url);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<link=\"{url}\">{text}</link>", actual);
		}


		public void LowerCaseShouldReturnTextInLowerCase()
		{
			const string text = "TEXT";
			LowerCaseStrategy strategy = new();
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<lowercase>{text}</lowercase>", actual);
		}


		public void UpperCaseShouldReturnTextInUpperCase()
		{
			const string text = "text";
			UpperCaseStrategy strategy = new();
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<uppercase>{text}</uppercase>", actual);
		}


		public void SmallCapsShouldReturnTextInSmallCaps()
		{
			const string text = "text";
			SmallCapsStrategy strategy = new();
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<smallcaps>{text}</smallcaps>", actual);
		}


		[TestCase("1")]
		[TestCase("2.5")]
		[TestCase("1em")]
		[TestCase("-0.5em")]
		[TestCase("15%")]
		public void ValidMarginShouldReturnTextWithMargin(string validMargin)
		{
			const string text = "text";
			MarginStrategy strategy = new(validMargin);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<margin={validMargin}>{text}</margin>", actual);
		}


		[TestCase("not a number")]
		[TestCase("1.0.0")]
		[TestCase("1.0.0em")]
		[TestCase("%15")]
		public void InvalidMarginShouldReturnUnmodifiedText(string invalidMargin)
		{
			LogAssert.ignoreFailingMessages = true;
			const string text = "text";
			MarginStrategy strategy = new(invalidMargin);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual(text, actual);
		}


		[TestCase(MarginDirection.Right)]
		[TestCase(MarginDirection.Left)]
		public void MarginWithDirectionShouldReturnTextWithMargin(MarginDirection direction)
		{
			const string text = "text";
			const string validMargin = "15%";
			MarginStrategy strategy = new(validMargin, direction);
			string actual = strategy.ApplyStyle(text);
			string stringDirection = direction.ToString().ToLower();
			Assert.AreEqual($"<margin-{stringDirection}={validMargin}>{text}</margin-{stringDirection}>", actual);
		}


		public void MarginWithAllDirectionReturnsTextWithMargin()
		{
			const string text = "text";
			const string validMargin = "15%";
			MarginStrategy strategy = new(validMargin, MarginDirection.All);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<margin={validMargin}>{text}</margin>", actual);
		}


		[TestCase("#FF000088")]
		[TestCase("#005500")]
		[TestCase("#000000")]
		public void ValidMarkColorReturnsMarkedText(string hexColor)
		{
			const string text = "text";
			MarkStrategy strategy = new(hexColor);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<mark={hexColor}>{text}</mark>", actual);
		}


		[TestCase("1")]
		[TestCase("2.5")]
		[TestCase("1em")]
		public void ValidCharacterWidthReturnsMonospacedText(string validCharacterWidth)
		{
			const string text = "text";
			MonospaceStrategy strategy = new(validCharacterWidth);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<mspace={validCharacterWidth}>{text}</mspace>", actual);
		}


		[TestCase("not a number")]
		[TestCase("1.0.0")]
		[TestCase("1.0.0em")]
		[TestCase("15%")]
		[TestCase("-1")]
		public void InvalidCharacterWidthReturnsUnmodifiedText(string invalidCharacterWidth)
		{
			LogAssert.ignoreFailingMessages = true;
			const string text = "text";
			MonospaceStrategy strategy = new(invalidCharacterWidth);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual(text, actual);
		}


		public void NoParseShouldReturnNotParsedText()
		{
			const string text = "text";
			NoParseStrategy strategy = new();
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<noparse>{text}</noparse>", actual);
		}


		public void NonBreakingSpacesShouldReturnWrappedText()
		{
			const string text = "text";
			NonBreakingSpacesStrategy strategy = new();
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<nobr>{text}</nobr>", actual);
		}


		[TestCase("1")]
		[TestCase("2.5")]
		[TestCase("1em")]
		[TestCase("10%")]
		public void ValidHorizontalPositionReturnsPositionedText(string validPosition)
		{
			const string text = "text";
			HorizontalPositionStrategy strategy = new(validPosition);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<pos={validPosition}>{text}</pos>", actual);

		}


		[TestCase("not a number")]
		[TestCase("1.0.0")]
		[TestCase("1.0.0em")]
		[TestCase("%15")]
		public void InvalidHorizontalPositionReturnsUnmodifiedText(string invalidPosition)
		{
			LogAssert.ignoreFailingMessages = true;
			const string text = "text";
			HorizontalPositionStrategy strategy = new(invalidPosition);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual(text, actual);
		}


		[TestCase("1")]
		[TestCase("2.5")]
		[TestCase("1em")]
		[TestCase("10%")]
		public void ValidFontSizeReturnsTextWithSize(string validFontSize)
		{
			LogAssert.ignoreFailingMessages = true;
			const string text = "text";
			FontSizeStrategy strategy = new(validFontSize);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<size={validFontSize}>{text}</size>", actual);
		}


		[TestCase("not a number")]
		[TestCase("1.0.0")]
		[TestCase("1.0.0em")]
		[TestCase("%15")]
		public void InvalidFontSizeReturnsUnmodifiedText(string invalidFontSize)
		{
			LogAssert.ignoreFailingMessages = true;
			const string text = "text";
			FontSizeStrategy strategy = new(invalidFontSize);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual(text, actual);
		}


		[TestCase("1")]
		[TestCase("2.5")]
		[TestCase("1em")]
		[TestCase("10%")]
		public void ValidSpaceAddsSpaceTag(string validSpace)
		{
			const string text = "text";
			SpaceStrategy strategy = new(validSpace);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"{text}<space={validSpace}>", actual);
		}


		[TestCase("not a number")]
		[TestCase("1.0.0")]
		[TestCase("1.0.0em")]
		[TestCase("%15")]
		public void InvalidSpaceReturnsUnmodifiedText(string invalidSpace)
		{
			LogAssert.ignoreFailingMessages = true;
			const string text = "text";
			SpaceStrategy strategy = new(invalidSpace);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual(text, actual);
		}


		public void InsertingSpriteByIndexReturnsTextWithSpriteIndex()
		{
			SpriteStrategy spriteStrategy = new(1);
			const string expected = "<sprite=1>";
			string actual = spriteStrategy.ApplyStyle("");
			Assert.AreEqual(expected, actual);
		}


		public void InsertingSpriteByNameFromDefaultAssetReturnsTextWithNamedSprite()
		{
			SpriteStrategy spriteStrategy = new("clown");
			const string expected = "<sprite name=\"clown\">";
			string actual = spriteStrategy.ApplyStyle("");
			Assert.AreEqual(expected, actual);
		}


		public void InsertingSpriteFromAtlasWithIndexReturnsTextWithAtlasAndIndex()
		{
			SpriteStrategy spriteStrategy = new("atlas", 1);
			const string expected = "<sprite=\"atlas\" index=1>";
			string actual = spriteStrategy.ApplyStyle("");
			Assert.AreEqual(expected, actual);
		}


		public void InsertingSpriteFromAtlasWithNameReturnsTextWIthAtlasAndNamedSprite()
		{
			SpriteStrategy spriteStrategy = new("atlas", "clown");
			const string expected = "<sprite=\"atlas\" name=\"clown\">";
			string actual = spriteStrategy.ApplyStyle("");
			Assert.AreEqual(expected, actual);
		}


		public void StrikethroughReturnsTextWithStrikethrough()
		{
			StrikethroughStrategy strategy = new();
			const string text = "text";
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<s>{text}</s>", actual);
		}


		public void UnderlineReturnsTextWithUnderline()
		{
			UnderlineStrategy strategy = new();
			const string text = "text";
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<u>{text}</u>", actual);
		}


		public void CustomStyleReturnsStyledText()
		{
			const string text = "text";
			const string style = "my-style";
			CustomStyleStrategy strategy = new(style);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<style=\"my-style\">{text}</style>", actual);
		}


		public void SubscriptShouldReturnTextWithSubscript()
		{
			SubscriptStrategy strategy = new();
			const string text = "text";
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<sub>{text}</sub>", actual);
		}


		public void SuperscriptShouldReturnTextWithSuperscript()
		{
			SuperscriptStrategy strategy = new();
			const string text = "text";
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<sup>{text}</sup>", actual);
		}


		[TestCase("1")]
		[TestCase("2.5")]
		[TestCase("1em")]
		[TestCase("10%")]
		public void ValidVerticalOffsetShouldReturn(string amount)
		{
			const string text = "text";
			VerticalOffsetStrategy strategy = new(amount);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<voffset={amount}>{text}</voffset>", actual);
		}


		[TestCase("not a number")]
		[TestCase("1.0.0")]
		[TestCase("1.0.0em")]
		[TestCase("%15")]
		public void InvalidVerticalOffsetShouldReturnUnmodifiedText(string invalidAmount)
		{
			LogAssert.ignoreFailingMessages = true;
			const string text = "text";
			VerticalOffsetStrategy strategy = new(invalidAmount);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual(text, actual);
		}


		[TestCase("1")]
		[TestCase("2.5")]
		[TestCase("1em")]
		[TestCase("10%")]
		public void ValidWidthShouldReturnTextWithWidth(string validWidth)
		{
			const string text = "text";
			WidthStrategy strategy = new(validWidth);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual($"<width={validWidth}>{text}</width>", actual);
		}


		[TestCase("not a number")]
		[TestCase("1.0.0")]
		[TestCase("1.0.0em")]
		[TestCase("%15")]
		public void InvalidWidthShouldReturnUnmodifiedText(string invalidWidth)
		{
			LogAssert.ignoreFailingMessages = true;
			const string text = "text";
			WidthStrategy strategy = new(invalidWidth);
			string actual = strategy.ApplyStyle(text);
			Assert.AreEqual(text, actual);
		}

	}
}