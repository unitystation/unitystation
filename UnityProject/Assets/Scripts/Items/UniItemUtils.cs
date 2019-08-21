using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Util methods / constants for working with uni items.
/// </summary>
public static class UniItemUtils
{
	private static readonly string ObjItemClothingHier = "/obj/item/clothing";
	public static readonly string ClothingHierIdentifier = "cloth";
	public static readonly  string HeadsetHierIdentifier = "headset";
	public static readonly  string BackPackHierIdentifier = "storage/backpack";
	public static readonly  string BagHierIdentifier = "storage/bag";

	// cached dm stuff
	private static DmObjectData dm;
	private static DmiIconData dmi;

	private static void EnsureInit()
	{
		if (dm == null)
		{
			dm = Resources.Load("DmObjectData")as DmObjectData;
		}

		if (dmi == null)
		{
			dmi = Resources.Load("DmiIconData")as DmiIconData;
		}
	}

	/// <summary>
	/// Try to get the sprite for this icon with the specified state
	/// </summary>
	/// <param name="dmiIcon">icon to get the state of</param>
	/// <param name="icon_state">state to get</param>
	/// <returns>the sprite for that state, null if unable to get it</returns>
	public static Sprite TryGetStateSprite(DmiIcon dmiIcon, string icon_state)
	{
		EnsureInit();
		if (dmiIcon == null || dmiIcon.getName().Equals(""))
		{

			Logger.Log($"DmiIcon '{dmiIcon}' is null, unable to get state '{icon_state}'", Category.DmMetadata);
			return null;
		}

		DmiState iState = dmiIcon.getState(icon_state);
		if (!iState.state.Equals(""))
		{
			return dmiIcon.spriteSheet[iState.offset];
		}

		Logger.Log($"Failed to find inventory sprite '{icon_state}' in icon '{dmiIcon.icon}'", Category.DmMetadata);
		return null;
	}


	/// <summary>
	/// Tries to get the inventory icon sprite for the unicloth with the specified hier string. If there are multiple
	/// sprites, just gets the first.
	/// </summary>
	/// <param name="hier"></param>
	/// <returns>first sprite for the specified hier</returns>
	//public static Sprite GetInventoryIconSprite(string hier)
	//{
	//	EnsureInit();
	//	DmiIcon icon = GetInventoryIcon(hier);
	//	var objectAttrs = dm.getObject(hier);
	//	objectAttrs.TryGetValue("icon_state", out var icon_state);

	//	return TryGetStateSprite(icon, icon_state);
	//}

	/// <summary>
	/// Gets the entire set of attributes associated with this particular hier
	/// </summary>
	/// <param name="hier"></param>
	/// <returns></returns>
	public static Dictionary<string,string> GetObjectAttributes(string hier)
	{
		EnsureInit();
		return dm.getObject(hier);
	}

	/// <summary>
	/// Tries to get the inventory icon for the unicloth with the specified hier string
	/// </summary>
	/// <param name="hier">hier to get the icon for</param>
	/// <param name="invSheetPaths">Pass the cached value here to speed up the method.
	/// Sheet paths to get it from, if null will be looked up from the hier.</param>
	/// <param name="icon">Pass a value here to speed up the method.
	/// icon to get, if null will be looked up from the hier.</param>
	/// <param name="icon_state">Pass a value here to speed up the method.
	/// icon_state of the icon to get, if null will be looked up from the hier.</param>
	/// <returns>DmiIcon for the specified hier, empty DmiIcon if unable</returns>
	//public static DmiIcon GetInventoryIcon(string hier, string[] invSheetPaths = null, string icon = null, string icon_state = null)
	//{
	//	EnsureInit();
	//	var objectAttrs = icon == null || icon_state == null ? dm.getObject(hier) : null;
	//	if (icon == null)
	//	{
	//		objectAttrs.TryGetValue("icon", out icon);
	//		icon = icon ?? "";
	//	}
	//	if (icon_state == null)
	//	{
	//		objectAttrs.TryGetValue("icon_state", out icon_state);
	//		icon_state = icon_state ?? "";
	//	}

	//	invSheetPaths = invSheetPaths ?? GetItemClothSheetHier(GetItemType(hier));
	//	//determining invIcon
	//	for (int i = 0; i < invSheetPaths.Length; i++)
	//	{
	//		string iconPath = DmiIconData.getIconPath(invSheetPaths[i]); //add extension junk
	//		if (!iconPath.Equals("") && DmiIconData.Data.ContainsKey(iconPath) && icon.Equals(""))
	//		{
	//			//					Logger.Log(name + ": iSheet = dmi.DataHier[" + iconPath +"] = " + dmi.Data[iconPath]);
	//			return DmiIconData.Data[iconPath];
	//		}
	//	}

	//	if (!icon.Equals(""))
	//	{
	//		//				Logger.Log(name + ": iSheet = dmi.DataIcon["+icon+"] = "+iSheet);
	//		//return DmiIconData.Data[icon];
	//	}
	//	//pretty bad choice, should use this only as last resort as it's usually pretty inaccurate
	//	DmiIcon invIcon = dmi.getIconByState(icon_state);
	//	if (invIcon != null)
	//	{

	//		Logger.Log($"UniItemUtils is doing bad dmi.getIconByState({icon_state}) = {invIcon.icon}", Category.DmMetadata);
	//		return invIcon;
	//	}
	//	//			Logger.LogError();
	//	return new DmiIcon();
	//}

	/// <summary>
	/// Get the master (top level) sprite type of the specified hier
	/// </summary>
	/// <param name="hier"></param>
	/// <returns>master sprite type of the hier - Clothing or Items</returns>
	public static SpriteType GetMasterType(string hier)
	{
		EnsureInit();
		if (hier.StartsWith(ObjItemClothingHier))
		{
			return SpriteType.Clothing;
		}

		return SpriteType.Items;
	}

	public static string GetInvIconPrefix(SpriteType st)
	{
		EnsureInit();
		switch (st)
		{
			case SpriteType.Clothing:
				return ObjItemClothingHier;
			default:
				return "";
		}
	}

	/// <summary>
	/// Gets the detailed item type
	/// </summary>
	/// <param name="hier">hier string to get the item type of</param>
	/// <returns>the detailed item type</returns>
	public static ItemType GetItemType(string hier)
	{
		EnsureInit();
		var masterType = GetMasterType(hier);
		var iconPrefix = GetInvIconPrefix(masterType);

		//	Logger.Log("getItemType for " + s);
		string sCut;
		if (!iconPrefix.Equals("") && hier.StartsWith(iconPrefix))
		{
			sCut = hier.Substring(iconPrefix.Length + 1).Split('/')[0];
			//				Logger.Log("sCut = "+ sCut);
		}
		else
		{
			if (hier.Contains("storage"))
			{
				sCut = "back";
			}
			else
			{
				//All other unknowns:
				sCut = hier;
			}
		}

		switch (sCut)
		{
			case "uniform":
			case "uniforms":
			case "under":
			case "underwear":
				return ItemType.Uniform;
			case "back":
			case "cloaks":
				return ItemType.Back;
			case "belt_mirror":
			case "belt":
			case "belts":
				return ItemType.Belt;
			case "eyes":
			case "glasses":
				return ItemType.Glasses;
			case "radio":
			case "ears":
				return ItemType.Ear;
			case "gloves":
			case "hands":
				return ItemType.Gloves;
			case "shoes":
			case "feet":
				return ItemType.Shoes;
			case "head":
			case "hats":
				return ItemType.Hat;
			case "mask":
			case "masks":
				return ItemType.Mask;
			case "tie":
			case "ties":
			case "neck":
				return ItemType.Neck;
			case "suit":
			case "flightsuit":
			case "suits":
				return ItemType.Suit;
			default:
				//GetItemType will be called several times on failure, with different string parameters
				//				Logger.Log("Could not find item type for " + sCut + ". Will attempt fallbacks if any exist.");
				return ItemType.None;
		}
	}

	/// <summary>
	/// Gets the item cloth sheet hier(s) for the specified type of item
	/// </summary>
	/// <param name="type">type of item to get the item cloth sheet hiers for</param>
	/// <returns>the hiers</returns>
	public static string[] GetItemClothSheetHier(ItemType type)
	{
		EnsureInit();
		string p = "obj/clothing/";
		switch (type)
		{
			case ItemType.Belt:
				return new [] { p + "belts" };
			case ItemType.Back:
				return new [] { "obj/storage" };
			case ItemType.Glasses:
				return new [] { p + "glasses" };
			case ItemType.Gloves:
				return new [] { p + "gloves" };
			case ItemType.Hat:
				return new [] { p + "hats" };
			case ItemType.Mask:
				return new [] { p + "masks" };
			case ItemType.Shoes:
				return new [] { p + "shoes" };
			case ItemType.Suit:
				return new [] { p + "suits" };
			case ItemType.Neck:
				return new [] { p + "ties", p + "neck" };
			case ItemType.Uniform:
				return new [] { p + "uniforms" };
			default:
				return new [] { "" };
		}
	}
}
