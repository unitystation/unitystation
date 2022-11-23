using System.Collections.Generic;
using UnityEngine;

namespace UI.Systems.Tooltips.HoverTooltips
{
	public interface IHoverTooltip
	{
		/// <summary>
		/// Put extra tips (such as interactions and building steps) below the object/item's description here.
		/// </summary>
		public string HoverTip();

		/// <summary>
		/// To manipulate the title of the tip incase the attributes of object/item does not contain the correct info
		/// that you want to be displayed.
		/// </summary>
		public string CustomTitle();

		/// <summary>
		/// In-case you don't want the tooltip to use the default icon from a Sprite Handler on the object.
		/// </summary>
		public Sprite CustomIcon();

		/// <summary>
		/// Shows a list of icons at the bottom of the screen that indicates a status of an object.
		/// (Such as being lit on fire or being dead)
		/// </summary>
		public List<Sprite> IconIndicators();

		public List<TextColor> InteractionsStrings();
	}

	public struct TextColor
	{
		public string Text;
		public Color Color;
	}

	public static class IntentColors
	{
		public static Color Help { get; } = Color.green;
		public static Color Harm { get; } = Color.red;
		public static Color Disarm { get; } = Color.blue;
		public static Color Grab { get; } = Color.yellow;
	}
}