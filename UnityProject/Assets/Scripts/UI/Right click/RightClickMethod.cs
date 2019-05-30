
using System;
using UnityEngine;

/// <summary>
/// FOR DEVELOPMENT USE ONLY, NOT FOR PRODUCTION CODE PATHS.
///
/// Attribute you can put on any method of a component to cause that method to be invokable
/// from the right click menu for any objects that component is attached to. Can be useful for debugging.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RightClickMethod : Attribute
{
	public readonly string label;
	public readonly string bgColorHex;
	public readonly string spritePath;

	/// <summary>
	/// Cause the attributed method to be invokable via the right click menu.
	/// </summary>
	/// <param name="label">Label to show under the menu option.</param>
	/// <param name="bgColorHex">Hex string (#ffffff) representing the background color of the option. Defaults to gray.</param>
	/// <param name="spritePath">Path to the sprite to show for the option, such as
	/// "UI/RightClickButtonIcon/question_mark". Defaults to question mark</param>
	public RightClickMethod(string label, string bgColorHex = "#444444", string spritePath = "UI/RightClickButtonIcon/question_mark.png")
	{
		this.label = label;
		this.bgColorHex = bgColorHex;
		this.spritePath = spritePath;
	}

	/// <summary>
	/// Creates a Menu whose appearance is based on the properties specified in this attribute.
	/// Does not wire up the Action or submenus.
	/// </summary>
	public RightclickManager.Menu AsMenu()
	{
		var menu = new RightclickManager.Menu();
		if (ColorUtility.TryParseHtmlString(bgColorHex, out var color))
		{
			menu.Colour = color;
		}
		else
		{
			Logger.LogWarningFormat("Unable to parse hex color string {0} in RightClickMethod. Please ensure this is a" +
			                        " valid hex color string like #223344. Defaulting to gray.", Category.UI, bgColorHex);
			menu.Colour = Color.gray;
		}

		menu.Label = label;
		var sprite = Resources.Load<Sprite>(spritePath);
		if (sprite == null)
		{
			Logger.LogWarningFormat("Unable to load sprite at path {0} in RightClickMethod. Please ensure this is a" +
			                        " valid path to a sprite. Defaulting to question mark.", Category.UI, spritePath);
			sprite = Resources.Load<Sprite>("UI/RightClickButtonIcon/question_mark.png");
		}

		menu.Sprite = sprite;

		return menu;
	}
}
