using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gives the object the ability to define radial menu options. There are 2 approaches for controlling which
/// options are available.
/// 1. Register / Unregister manually. Other components on the object can add / remove right click options as their
/// state changes (using Add/RemoveRightClickOption).
/// 2. IRightClickable - generate on-demand (when right click happens). A component on this
/// object can implement IRightClickable
/// to generate the options based on its current state when the right click occurs.
///
/// It is recommended to stick with one approach for a given component.
///
/// The actual rendering and interaction of the right click menu happens in
/// RightclickManager. This component just allows a way to dynamically control which menu options
/// this object should have.
/// </summary>
public class RightClickMenu : MonoBehaviour
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
	/// Current right click options in the menu, with the callback that should be invoked
	/// when the option is clicked. This is an alternative to generating the options on demand,
	/// when the user clicks.
	/// </summary>
	private Dictionary<RightClickOption,Action> optionToCallback;

	/// <summary>
	/// Adds a right click option to this menu. Error will be logged if it already
	/// exists in this menu.
	/// </summary>
	/// <param name="toAdd">option to add</param>
	/// <param name="onClick">callback to invoke when clicked.</param>
	public void AddRightClickOption(RightClickOption toAdd, Action onClick)
	{
		if (optionToCallback == null)
		{
			optionToCallback = new Dictionary<RightClickOption, Action>();
		}
		if (optionToCallback.ContainsKey(toAdd))
		{
			Logger.LogWarningFormat("Attempted to add right click option {0} which was" +
			                      " already in the menu. This attempt to add will be ignored.", Category.UI, toAdd.label);
		}
		else
		{
			optionToCallback.Add(toAdd, onClick);
		}
	}

	/// <summary>
	/// Static / convenient way of adding a right click option for a specific game object. Can be more convenient and less verbose
	/// than using the non-static method because the behavior doesn't need to GetComponent the RightClickMenu and
	/// can specify a default. Also adds RightClickMenu as a component to the gameObject if it doesn't exist.
	/// </summary>
	/// <param name="toAdd">Right click option to add. Can be null. If null, will load the option from
	/// defaultOptionPath instead. This can be used to override the default.</param>
	/// <param name="defaultOptionPath">Path to the RightClickOption resource that should be used
	/// for this option if toAdd is null, for example: "ScriptableObjects/Interaction/RightclickOptions/Pull"</param>
	/// <param name="forObject">Object this right click option is for. Should almost always be "gameObject" when calling
	/// this method from a component on that gameObject. If rightclickmenu doesn't exist on that object, it will
	/// be added.</param>
	/// <param name="onClick">action to invoke when the option is selected</param>
	/// <returns>the right click option that was added - toAdd if it was not null, otherwise the RightClickOption
	/// at defaultOptionPath</returns>
	public static RightClickOption AddRightClickOption(string defaultOptionPath, GameObject forObject,
		Action onClick, RightClickOption toAdd = null)
	{
		//default if undefined
		var option = RightClickOption.DefaultIfNull(defaultOptionPath, toAdd);
		var rightClickMenu = forObject.GetComponent<RightClickMenu>();
		if (rightClickMenu == null)
		{
			rightClickMenu = forObject.AddComponent<RightClickMenu>();
		}
		rightClickMenu.AddRightClickOption(option, onClick);

		return option;
	}

	/// <summary>
	/// Removes the right click option from the menu.
	/// </summary>
	/// <param name="toRemove">option to remove</param>
	public void RemoveRightClickOption(RightClickOption toRemove)
	{
		if (optionToCallback == null || !optionToCallback.ContainsKey(toRemove))
		{
			Logger.LogWarningFormat("Attempted to remove right click option {0} which was" +
			                        " not in the menu. This attempt to add will be ignored.", Category.UI, toRemove.label);
		}
		else
		{
			optionToCallback.Remove(toRemove);
		}
	}

	/// <summary>
	/// Static / convenient way of removing a right click option for a game object. Removes the right click option from the menu.
	/// </summary>
	/// <param name="toRemove">option to remove</param>
	/// <param name="forObject">Object this right click option is for. Should almost always be "gameObject" when calling
	/// this method from a component on that gameObject.</param>
	public static void RemoveRightClickOption(RightClickOption toRemove, GameObject forObject)
	{
		var rightClickMenu = forObject.GetComponent<RightClickMenu>();
		if (rightClickMenu != null)
		{
			rightClickMenu.RemoveRightClickOption(toRemove);
		}
	}

	/// <summary>
	///
	/// </summary>
	/// <returns>The list of currently defined options that should appear for this object and the
	/// action to invoke when it is selected. Do not modify this.</returns>
	public Dictionary<RightClickOption, Action> GetCurrentOptions()
	{
		var options = new Dictionary<RightClickOption, Action>();

		//get options from our dictionary.
		if (optionToCallback != null)
		{
			foreach (var option in optionToCallback)
			{
				options.Add(option.Key, option.Value.Invoke);
			}
		}

		//check all components which generate right click options on demand
		var rightClickables = GetComponents<IRightClickable>();
		if (rightClickables != null)
		{
			foreach (var rightClickable in rightClickables)
			{
				var generatedOptions = rightClickable.GenerateRightClickOptions();
				if (generatedOptions == null) continue;
				foreach (var generatedOption in generatedOptions)
				{
					options.Add(generatedOption.Key, generatedOption.Value);
				}
			}
		}

		return options;
	}

	/// <summary>
	/// Create a Menu based on the current configuration in this RightClickMenu
	/// </summary>
	/// <param name="order">Ordering to use for right click options.</param>
	/// <returns>a Menu containing all the info necessary for rendering this object's right click menu. The
	///  returned Menu contains this object's menu item and the sub menus contain the actions for this
	/// object.</returns>
	public RightclickManager.Menu AsMenu(RightClickOptionOrder order)
	{
		RightclickManager.Menu newMenu = new RightclickManager.Menu();

		newMenu.colour = backgroundColor;
		newMenu.title = nameOverride != null && nameOverride.Trim().Length != 0 ? nameOverride : gameObject.name.Replace("(clone)","");

		if (iconOverride == null)
		{
			SpriteRenderer firstSprite = GetComponentInChildren<SpriteRenderer>();
			if (firstSprite != null)
			{
				newMenu.sprite = firstSprite.sprite;
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
			newMenu.sprite = iconOverride;
		}



		//Sort current options by RightClickOptionOrder
		var sortedOptions = GetCurrentOptions().Keys.ToList();
		sortedOptions.Sort(order.Compare);

		foreach (var rightClickOption in sortedOptions)
		{
			var action = GetCurrentOptions()[rightClickOption];

			//create menu items for each right click option
			RightclickManager.Menu newSubMenu = new RightclickManager.Menu();
			newSubMenu.colour = rightClickOption.backgroundColor;
			newSubMenu.Item = gameObject;
			newSubMenu.title = rightClickOption.label;
			newSubMenu.sprite = rightClickOption.icon;
			newSubMenu.Action = action;

			newMenu.SubMenus.Add(newSubMenu);
		}

		return newMenu;
	}

	/// <summary>
	/// Ensures that the RightClickMenu component exists on this object. Used because
	/// it would be tedious to manually add it to all objects that it's needed on.
	/// </summary>
	/// <param name="obj"></param>
	public static RightClickMenu EnsureComponentExists(GameObject obj)
	{
		var rightClick = obj.GetComponent<RightClickMenu>();
		return rightClick == null ? obj.AddComponent<RightClickMenu>() : rightClick;
	}
}
