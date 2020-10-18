
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

/// <summary>
/// Represents an individual right click option created by an IRightClickable.
/// Wires a RightClickOption to an Action and allows customization of its appearance / behavior.
/// </summary>
public class RightClickableElement
{
	private const string RIGHT_CLICK_OPTION_FOLDER = "ScriptableObjects/Interaction/RightclickOptions";
	private static Dictionary<string, RightClickOption> optionNameToOption;

	private readonly RightClickOption option;
	private readonly Action action;
	private readonly string nameOverride;
	private readonly Color? bgColorOverride;
	private readonly Sprite spriteOverride;
	private readonly Sprite bgSpriteOverride;

	private RightClickableElement(RightClickOption option, Action action, string nameOverride, Color? bgColorOverride, Sprite spriteOverride, Sprite bgSpriteOverride)
	{
		this.option = option;
		this.action = action;
		this.nameOverride = nameOverride;
		this.bgColorOverride = bgColorOverride;
		this.spriteOverride = spriteOverride;
		this.bgSpriteOverride = bgSpriteOverride;
	}

	/// <summary>
	/// Create a RightClickableElement whose appearance will be based on the
	/// RightClickOption with the specified name. Shorthand to avoid having to specify
	/// the full resource path of the RightClickOption.
	/// </summary>
	/// <param name="optionName">name of the option as it exists in the
	/// Resources/ScriptableObjects/Interaction/RightclickOptions folder</param>
	/// <param name="action">Action to invoke when the option is clicked.</param>
	/// <param name="bgColorOverride">Color to use instead of the RightClickOption's normal
	/// background color.</param>
	/// <param name="spriteOverride">Sprite to use instead of the RightClickOption's normal sprite.</param>
	/// <param name="bgSpriteOverride">Sprite to use instead of the RightClickOption's normal background sprite.</param>
	/// <param name="nameOverride">Name to use instead of the RightClickOption's name</param>
	/// <returns>RightClickableElement encapsulating this info</returns>
	public static RightClickableElement FromOptionName(string optionName, Action action,
		Color? bgColorOverride = null, string nameOverride = null, Sprite spriteOverride = null, Sprite bgSpriteOverride = null)

	{
		if (optionNameToOption == null)
		{
			initOptionDict();
		}

		if (optionNameToOption.TryGetValue(optionName, out var option))
		{
			return new RightClickableElement(option, action, nameOverride, bgColorOverride, spriteOverride, bgSpriteOverride);
		}
		else
		{
			Logger.LogWarningFormat("Unable to find right click option with name {0}. Ensure" +
			                      " the RightClickOption scriptable object exists in the folder {1}." +
			                      " A default option will be displayed instead with the same name.",
									Category.UI, optionName, RIGHT_CLICK_OPTION_FOLDER);
			return FromOptionName("Default", action, bgColorOverride, nameOverride != null ? nameOverride : optionName, spriteOverride, bgSpriteOverride);
		}
	}

	private static void initOptionDict()
	{
		optionNameToOption = new Dictionary<string, RightClickOption>();
		var allOptions = Resources.LoadAll<RightClickOption>(RIGHT_CLICK_OPTION_FOLDER);

		foreach (var option in allOptions)
		{
			optionNameToOption.Add(option.name, option);
		}
	}

	public static IComparer<RightClickableElement> CompareBy(RightClickOptionOrder rightClickOptionOrder)
	{
		return Comparer<RightClickableElement>.Create((r1,r2) => rightClickOptionOrder.Compare(r1.option, r2.option));
	}

	/// <summary>
	/// Create a menu item based on this element.
	/// </summary>
	/// <returns></returns>
	public RightClickMenuItem AsMenu()
	{
		var label = nameOverride ?? option.label;
		var color = bgColorOverride ?? option.backgroundColor;
		var sprite = spriteOverride ? spriteOverride : option.icon;
		var bgSprite = bgSpriteOverride ? bgSpriteOverride : option.backgroundSprite;

		return RightClickMenuItem.CreateSubMenuItem(color, sprite, bgSprite, label, action);
	}
}
