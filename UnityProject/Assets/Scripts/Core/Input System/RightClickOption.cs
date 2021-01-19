using System;
using UnityEngine;

/// <summary>
/// An item that can show up in a right click menu
/// </summary>
[CreateAssetMenu(fileName = "New RightClickOption", menuName = "Interaction/Right Click Option")]
public class RightClickOption : ScriptableObject
{
	[Tooltip("Icon to display for this option in the radial menu.")]
	public Sprite icon;
	[Tooltip("Sprite to show behind the icon.")]
	public Sprite backgroundSprite;
	[Tooltip("Text to show for this option in the radial menu.")]
	public String label;
	[Tooltip("background color of the radial menu item")]
	public Color backgroundColor = RightClickManager.ButtonColor;
	[Tooltip("Should the menu stay open when this action is performed.")]
	public bool keepMenuOpen = true;

	/// <summary>
	/// Default to the RightClickOption at the specified path if option is null. Convenience method
	/// for defaulting to a programmer-defined instance of this SO.
	/// </summary>
	/// <param name="defaultOptionPath">Path to the RightClickOption resource that should be used
	/// for this option if option is null, for example: "ScriptableObjects/Interaction/RightclickOptions/Pull"</param>
	/// <param name="option">option, can be null</param>
	/// <returns>option if it's not null, otherwise returns the RightClickOption at the specified path</returns>
	public static RightClickOption DefaultIfNull(string defaultOptionPath, RightClickOption option)
	{
		return option ? option : Resources.Load<RightClickOption>(defaultOptionPath);
	}


	/// <summary>
	/// Create a Right click Menu item whose appearance is based on the settings in this RightClickOption.
	/// </summary>
	/// <param name="action">action to invoke when clicked</param>
	public RightClickMenuItem AsMenu(Action action)
	{
		var menu = RightClickMenuItem.CreateSubMenuItem(backgroundColor, icon, backgroundSprite, label, action, keepMenuOpen: keepMenuOpen);
		return menu;
	}
}