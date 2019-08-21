using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using Random = System.Random;

[RequireComponent(typeof(SpriteHandlerData))]
[RequireComponent(typeof(Pickupable))]
[RequireComponent(typeof(ObjectBehaviour))]
[RequireComponent(typeof(RegisterItem))]
[RequireComponent(typeof(CustomNetTransform))]
public class ItemAttributes : NetworkBehaviour, IRightClickable
{
	private const string MaskInternalsFlag = "MASKINTERNALS";
	private const string ObjItemClothing = "/obj/item/clothing";
	private static DmiIconData dmi;
	public static DmObjectData dm;

	/// <summary>
	/// This is used as a Lazy initialized backing field for <see cref="HierList"/>
	/// and calls <see cref="InitializeHierList"/> when <see cref="HierList"/> is first accessed
	/// </summary>
	private static readonly Lazy<string[]> hierList = new Lazy<string[]>(InitializeHierList);
	private static string[] HierList => hierList.Value;

	//on-player references
	private static readonly string[] onPlayer = {
		"mob/uniform",
		"mob/underwear",
		"mob/ties",
		"mob/back",
		"mob/belt_mirror",
		"mob/belt",
		"mob/eyes",
		"mob/ears",
		"mob/hands",
		"mob/feet",
		"mob/head",
		"mob/mask",
		"mob/neck",
		"mob/suit"
	};

	/// <summary>
	/// Remember in hands is Left then right so [0] = Left, [1] = right
	/// </summary>
	public SpriteHandlerData spriteHandlerData;

	public SpriteHandler InventoryIcon;

	public string itemName;
	public string itemDescription;

	public ItemType itemType = ItemType.None;
	public ItemSize size;
	public SpriteType spriteType;

	/// <summary>
	/// True if this is a mask that can connect to a tank
	/// </summary>
	[FormerlySerializedAs("ConnectedToTank")]
	public bool CanConnectToTank;


	/// throw-related fields
	[Tooltip("Damage when we click someone with harm intent")]
	[Range(0, 100)]
	public float hitDamage = 0;

	public DamageType damageType = DamageType.Brute;

	[Tooltip("How painful it is when someone throws it at you")]
	[Range(0, 100)]
	public float throwDamage = 0;

	[Tooltip("How many tiles to move per 0.1s when being thrown")]
	public float throwSpeed = 2;

	[Tooltip("Max throw distance")]
	public float throwRange = 7;

	[Tooltip("Sound to be played when we click someone with harm intent")]
	public string hitSound = "GenericHit";

	///<Summary>
	/// Can this item protect humans against spess?
	///</Summary>
	public bool IsEVACapable { get; private set; }

	public List<string> attackVerb = new List<string>();
	private static readonly char[] ListSplitters = new [] { ',', ' ' };



	public void SetUpFromClothingData(ClothingData ClothingData, ClothingVariantType CVT = ClothingVariantType.Default, int variant = -1) {
		if (spriteHandlerData == null) {
			spriteHandlerData = new SpriteHandlerData();
		}		if (!(variant > -1))
		{
			switch (CVT)
			{
				case ClothingVariantType.Default:
					spriteHandlerData.Infos = new SpriteDataForSH();
					spriteHandlerData.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Base.InHandsLeft));
					spriteHandlerData.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Base.InHandsRight));
					InventoryIcon.Infos = new SpriteDataForSH();
					InventoryIcon.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Base.ItemIcon));
					Logger.Log(ClothingData.Base.ItemIcon.Sprites.Length.ToString());
					break;
				case ClothingVariantType.Tucked:
					spriteHandlerData.Infos = new SpriteDataForSH();
					if (ClothingData.Base_Adjusted.InHandsLeft != null)
					{ spriteHandlerData.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Base_Adjusted.InHandsLeft)); }
					else
					{ spriteHandlerData.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Base.InHandsLeft)); }

					if (ClothingData.Base_Adjusted.InHandsRight != null)
					{ spriteHandlerData.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Base_Adjusted.InHandsRight)); }
					else
					{ spriteHandlerData.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Base.InHandsLeft)); }

					InventoryIcon.Infos = new SpriteDataForSH();
					if (ClothingData.Base_Adjusted.ItemIcon != null)
					{ InventoryIcon.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Base_Adjusted.ItemIcon)); }
					else
					{ InventoryIcon.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Base.ItemIcon)); }
					break;

				case ClothingVariantType.Skirt:
					spriteHandlerData.Infos = new SpriteDataForSH();
					if (ClothingData.DressVariant.InHandsLeft != null)
					{ spriteHandlerData.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.DressVariant.InHandsLeft)); }
					else
					{ spriteHandlerData.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Base.InHandsLeft)); }

					if (ClothingData.DressVariant.InHandsRight != null)
					{ spriteHandlerData.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.DressVariant.InHandsRight)); }
					else
					{ spriteHandlerData.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Base.InHandsLeft)); }

					InventoryIcon.Infos = new SpriteDataForSH();
					if (ClothingData.DressVariant.ItemIcon != null)
					{ InventoryIcon.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.DressVariant.ItemIcon)); }
					else
					{ InventoryIcon.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Base.ItemIcon)); }
					break;
			}
		}
		else {
			if (ClothingData.Variants.Count > variant)
			{
				spriteHandlerData.Infos = new SpriteDataForSH();
				if (ClothingData.Variants[variant].InHandsLeft != null)
				{ spriteHandlerData.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Variants[variant].InHandsLeft)); }
				else
				{ spriteHandlerData.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Base.InHandsLeft)); }

				if (ClothingData.Variants[variant].InHandsRight != null)
				{ spriteHandlerData.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Variants[variant].InHandsRight)); }
				else
				{ spriteHandlerData.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Base.InHandsLeft)); }

				InventoryIcon.Infos = new SpriteDataForSH();
				if (ClothingData.Variants[variant].ItemIcon != null)
				{ InventoryIcon.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Variants[variant].ItemIcon)); }
				else
				{ InventoryIcon.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(ClothingData.Base.ItemIcon)); }

			}
		}
		InventoryIcon.PushTexture();
	}


	private static string GetMasterTypeHandsString(SpriteType masterType)
	{
		switch (masterType)
		{
			case SpriteType.Clothing: return "clothing";

			default: return "items";
		}
	}


	/// <summary>
	/// Checks wheter the given <paramref name="dmDic"/> is null and the item exists and return the value with the given <paramref name="key"/>
	/// </summary>
	/// <param name="dmDic">Dictionary to get the value from</param>
	/// <param name="key">Key to search for in <paramref name="dmDic"/></param>
	/// <returns>Value accosiated with the <paramref name="key"/> in <paramref name="dmDic"/>,
	/// <see cref="String.Empty"/> if not found or <paramref name="dmDic"/> is null</returns>
	public static string TryGetAttr(Dictionary<string, string> dmDic, string key)
	{
		if (dmDic != null && dmDic.ContainsKey(key))
		{
			return dmDic[key];
		}
		return String.Empty;
	}

	/// <summary>
	/// Whether <see cref="dm"/> and <see cref="dmi"/> are both non-null
	/// </summary>
	/// <returns>True if neither <see cref="dm"/> or <see cref="dmi"/> is null</returns>
	public bool HasDataLoaded()
	{
		return dm != null && dmi != null;
	}

	/// <summary>
	/// Getting stuff from whatever states provided
	/// </summary>
	/// <param name="states">Expected order is {item_state, item_color, icon_state}</param>
	/// <returns></returns>
	public static int TryGetClothingOffset(string[] states, ItemType itemType)
	{
		string[] onPlayerClothSheetHier = GetOnPlayerClothSheetHier(itemType);
		for (int i = 0; i < states.Length; i++)
		{
			if (String.IsNullOrEmpty(states[i])) continue;

			var icons = itemType == ItemType.None ?
					onPlayer :
					onPlayerClothSheetHier;

			//DmiState state = dmi.searchStateInIcon(states[i], icons, 4, false);

			//if (state == null) continue;

			//return state.offset;
		}

		//Logger.LogError("No clothing offset found!  ClothHier=" + onPlayerClothSheetHier[0] + ", " + GetItemDebugInfo());
		return -1;
	}


	private static string[] GetOnPlayerClothSheetHier(ItemType type)
	{
		string p = "mob/";
		string r = null;
		switch (type)
		{
			case ItemType.Glasses: r = "eyes"   ; break;
			case ItemType.Hat    : r = "head"   ; break;
			case ItemType.Mask   : r = "mask"   ; break;
			case ItemType.Ear    : r = "ears"   ; break;
			case ItemType.Suit   : r = "suit"   ; break;
			case ItemType.Uniform: r = "uniform"; break;
			case ItemType.Gloves : r = "hands"  ; break;
			case ItemType.Shoes  : r = "feet"   ; break;
			case ItemType.Back   : r = "back"   ; break;

			case ItemType.Neck: return new[] { p + "ties", p + "neck"        };
			case ItemType.Belt: return new[] { p + "belt", p + "belt_mirror" };
		}
		return new[] { r == null ? "" : p + r };
	}


	/// <summary>
	/// Used to initialize <see cref="HierList"/>
	/// </summary>
	/// <returns>The value <see cref="HierList"/> is set to</returns>
	private static string[] InitializeHierList()
	{
		string path = Path.Combine("metadata", "hier");
		TextAsset asset = Resources.Load(path) as TextAsset;
		if (asset != null)
		{
			var hiers = asset.text.Split('\n').Where(h => h.Contains("cloth"));
			return hiers.ToArray();
		}
		Logger.LogError($"Couldn't initialize {nameof(HierList)} asset \"{path}\" is null", Category.DmMetadata);
		return null;
	}

	/// <summary>
	/// Item size based on the input string, <see cref="ItemSize.Small"/> by default
	/// </summary>
	/// <param name="s"><see cref="string"/> to get size from</param>
	/// <returns>Size based on <paramref name="s"/>, <see cref="ItemSize.Small"/> if <paramref name="s"/> doesn't match any other size</returns>
	private static ItemSize GetItemSize(string s)
	{
		switch (s)
		{
			case "WEIGHT_CLASS_TINY":
				return ItemSize.Tiny;
			case "WEIGHT_CLASS_SMALL":
				return ItemSize.Small;
			case "WEIGHT_CLASS_NORMAL":
				return ItemSize.Medium;
			case "WEIGHT_CLASS_BULKY":
				return ItemSize.Large;
			case "WEIGHT_CLASS_HUGE":
				return ItemSize.Huge;
			default:
				return ItemSize.Small;
		}
	}

	private string SpriteTypeCode()
	{
		int i = -1;
		switch (spriteType)
		{
			case SpriteType.Items   : i = 1; break;
			case SpriteType.Clothing: i = 2; break;
			case SpriteType.Guns    : i = 3; break;
		}
		return i.ToString();
	}

	public void OnHoverStart()
	{
		// Show the parenthesis for an item's description only if the item has a description
		UIManager.SetToolTip =
			itemName +
			(String.IsNullOrEmpty(itemDescription) ?
				"" :
				$" ({itemDescription})");
	}

	public void OnHoverEnd()
	{
		UIManager.SetToolTip = String.Empty;
	}

	// Sends examine event to all monobehaviors on gameobject
	public void SendExamine()
	{
		SendMessage("OnExamine");
	}

	// When right clicking on an item, examine the item
	public void OnHover()
	{
		if (CommonInput.GetMouseButtonDown(1))
		{
			SendExamine();
		}
	}

	private void OnExamine()
	{
		if (!string.IsNullOrEmpty(itemDescription))
		{
			ChatRelay.Instance.AddToChatLogClient(itemDescription, ChatChannel.Examine);
		}
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		return RightClickableResult.Create()
			.AddElement("Examine", OnExamine);
	}
}