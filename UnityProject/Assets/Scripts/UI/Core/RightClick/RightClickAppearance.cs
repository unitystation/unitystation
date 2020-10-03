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

	[Tooltip("Background sprite to show. Leave empty to show no background sprite.")]
	public Sprite backgroundSprite;

	/// <summary>
	/// Create a RightClickMenuItem for the object based on the current configuration in this RightClickAppearance.
	/// </summary>
	/// <param name="subMenus">sub menu items to show underneath this object's menu</param>
	public RightClickMenuItem AsMenu(List<RightClickMenuItem> subMenus)
	{
		var label = nameOverride != null && nameOverride.Trim().Length != 0 ? nameOverride : gameObject.name.Replace("(clone)","");
		Sprite sprite = null;
		if (iconOverride == null)
		{
			SpriteRenderer firstSprite = GetComponentInChildren<SpriteRenderer>();
			if (firstSprite != null)
			{
				return RightClickMenuItem.CreateObjectMenuItem(backgroundColor, firstSprite.sprite, backgroundSprite, label, subMenus, firstSprite.color);
			}

			Logger.LogWarningFormat("Could not determine sprite to use for right click menu" +
			                        " for object {0}. Please specify a sprite in the RightClickMenu component" +
			                        " for this object.", Category.UI, gameObject.name);
		}
		else
		{
			sprite = iconOverride;
		}

		return RightClickMenuItem.CreateObjectMenuItem(backgroundColor, sprite, backgroundSprite, label, subMenus);
	}
}
