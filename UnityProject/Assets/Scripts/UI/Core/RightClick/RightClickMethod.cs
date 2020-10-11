
using System;
using System.Reflection;
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
	public readonly string bgSpritePath;

	/// <summary>
	/// Cause the attributed method to be invokable via the right click menu.
	/// </summary>
	/// <param name="label">Label to show under the menu option. If null, will default to the attributed method's name</param>
	/// <param name="bgColorHex">Hex string (#ffffff) representing the background color of the option. Defaults to gray.</param>
	/// <param name="spritePath">Path to the sprite to show for the option, such as
	/// "UI/RightClickButtonIcon/question_mark". Defaults to question mark</param>
	/// <param name="bgSpritePath">Path to the sprite to show for the option, such as
	/// "UI/RightClickButtonIcon/question_mark". Defaults to null, showing no background.</param>
	public RightClickMethod(string label = null, string bgColorHex = "#FF0000", string spritePath = "UI/RightClickButtonIcon/question_mark", string bgSpritePath = null)
	{
		this.label = label;
		this.bgColorHex = bgColorHex;
		this.spritePath = spritePath;
		this.bgSpritePath = bgSpritePath;
	}

	/// <summary>
	/// Creates a RightClickMenuItem whose appearance is based on the properties specified in this attribute,
	/// wired to the specified action (which should be the method it is attributed on)
	/// </summary>
	/// <param name="attributedMethod">method this was attributed to</param>
	/// <param name="attributedMethod">component instance whose method should be invoked when this option is clicked.</param>
	public RightClickMenuItem AsMenu(MethodInfo attributedMethod, Component forComponent)
	{
		var labelToUse = label ?? attributedMethod.Name;

		var colorToUse = Color.gray;
		if (ColorUtility.TryParseHtmlString(bgColorHex, out var color))
		{
			colorToUse = color;
		}
		else
		{
			Logger.LogWarningFormat("Unable to parse hex color string {0} in RightClickMethod. Please ensure this is a" +
			                        " valid hex color string like #223344. Defaulting to gray.", Category.UI, bgColorHex);
		}

		var sprite = Resources.Load<Sprite>(spritePath);
		if (sprite == null)
		{
			Logger.LogWarningFormat("Unable to load sprite at path {0} in RightClickMethod. Please ensure this is a" +
			                        " valid path to a sprite. Defaulting to question mark.", Category.UI, spritePath);
			sprite = Resources.Load<Sprite>("UI/RightClickButtonIcon/question_mark.png");
		}

		Sprite bgSprite = null;
		if (bgSpritePath != null)
		{
			bgSprite = Resources.Load<Sprite>(bgSpritePath);
			if (bgSprite == null)
			{
				Logger.LogWarningFormat(
					"Unable to load bgSprite at path {0} in RightClickMethod. Please ensure this is a" +
					" valid path to a sprite. Defaulting to question no background.", Category.UI, bgSpritePath);
			}
		}

		return RightClickMenuItem.CreateSubMenuItem(colorToUse, sprite, bgSprite, labelToUse, (Action) Delegate.CreateDelegate(typeof(Action), forComponent, attributedMethod));
	}
}
