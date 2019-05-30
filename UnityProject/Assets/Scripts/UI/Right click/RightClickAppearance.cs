using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Allows an object to customize its appearance in the right click menu. If this component is not
/// present on the object, the default appearance will be used for its menu option.
/// </summary>
public class RightClickAppearance : MonoBehaviour
{
	[Tooltip("Name to show for this object in the right click menu. Leave blank to use" +
	         " the gameObject name.")]
	public string nameOverride;

	[Tooltip("Background color to show for this object in the right click menu.")]
	public Color backgroundColor = Color.gray;

	[Tooltip("Icon to show for this object in the right click menu. Leave blank to use" +
	         " the first SpriteRenderer of this object as the icon.")]
	public Sprite iconOverride;

	/// <summary>
	/// Create a Menu based on the current configuration in this RightClickMenu. Only defines
	/// the appearance of the menu, doesn't add any children or action.
	/// </summary>
	public RightclickManager.Menu AsMenu()
	{
		RightclickManager.Menu newMenu = new RightclickManager.Menu();

		newMenu.Colour = backgroundColor;
		newMenu.Label = nameOverride != null && nameOverride.Trim().Length != 0 ? nameOverride : gameObject.name.Replace("(clone)","");

		if (iconOverride == null)
		{
			SpriteRenderer firstSprite = GetComponentInChildren<SpriteRenderer>();
			if (firstSprite != null)
			{
				newMenu.Sprite = firstSprite.sprite;
			}
			else
			{
				Logger.LogWarningFormat("Could not determine sprite to use for right click menu" +
				                        " for object {0}. Please specify a sprite in the RightClickMenu component" +
				                        " for this object.", Category.UI, gameObject.name);
			}
		}
		else
		{
			newMenu.Sprite = iconOverride;
		}

		return newMenu;
	}
}
