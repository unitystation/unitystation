
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a right click menu item that can be rendered and shown to the user. Represents
/// top-level (object) items as well as their sub-menu options (actions that can be done to the object)
/// </summary>
public class RightClickMenuItem
{
	public readonly Color BackgroundColor;
	public readonly Sprite IconSprite;
	public readonly Color IconColor;
	public readonly Sprite BackgroundSprite;
	public readonly string Label;
	public readonly List<Color> palette;
	public readonly bool keepMenuOpen;

	public readonly List<RightClickMenuItem> SubMenus;
	public readonly Action Action;


	private RightClickMenuItem(Sprite iconSprite, Color iconColor, Sprite backgroundSprite, Color backgroundColor,
		string label, List<RightClickMenuItem> subMenus, Action action, List<Color> palette = null, bool keepMenuOpen = true)
	{
		this.BackgroundColor = backgroundColor;
		this.IconSprite = iconSprite;
		this.IconColor = iconColor;
		this.Label = label;
		this.Action = action;
		this.SubMenus = subMenus;
		this.BackgroundSprite = backgroundSprite;
		this.palette = palette;
		this.keepMenuOpen = keepMenuOpen;
	}

	/// <summary>
	/// Create a right click menu item for a sub-menu (an action that can be performed on an object)
	/// </summary>
	/// <param name="color">background color</param>
	/// <param name="sprite">sprite of icon</param>
	/// <param name="backgroundSprite">background sprite of icon, can be null</param>
	/// <param name="label">label to show</param>
	/// <param name="action">action to invoke when it is clicked</param>
	/// <param name="keepMenuOpen">should the menu stay open when the action is performed</param>
	public static RightClickMenuItem CreateSubMenuItem(Color color, Sprite sprite, Sprite backgroundSprite, string label, Action action, List<Color> palette = null, bool keepMenuOpen = true)
	{
		return new RightClickMenuItem(sprite, Color.white, backgroundSprite, color, label, null, action, palette, keepMenuOpen);
	}

	/// <summary>
	/// Create a right click menu item for an object, which has one or more sub menu
	/// items.
	/// </summary>
	/// <param name="color">background color</param>
	/// <param name="sprite">sprite of icon</param>
	/// <param name="backgroundSprite">background sprite of icon, can be null</param>
	/// <param name="label">label to show</param>
	/// <param name="subMenus">submenu items to show for this object</param>
	/// <param name="keepMenuOpen">should the menu stay open when the action is performed</param>
	public static RightClickMenuItem CreateObjectMenuItem(Color color, Sprite sprite, Sprite backgroundSprite, string label,
		List<RightClickMenuItem> subMenus, List<Color> palette = null, bool keepMenuOpen = true)
	{
		return new RightClickMenuItem(sprite, Color.white, backgroundSprite,  color, label, subMenus, null, palette, keepMenuOpen);
	}
	public static RightClickMenuItem CreateObjectMenuItem(Color color, Sprite sprite, Sprite backgroundSprite, string label,
		List<RightClickMenuItem> subMenus, Color iconColor, List<Color> palette = null, bool keepMenuOpen = true)
	{
		return new RightClickMenuItem(sprite, iconColor, backgroundSprite,  color, label, subMenus, null, palette, keepMenuOpen);
	}


}
