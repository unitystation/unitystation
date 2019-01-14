using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

[RequireComponent(typeof(ObjectBehaviour))]
public class ItemAttributes : NetworkBehaviour
{
	private const string ObjItemClothing = "/obj/item/clothing";
	private static DmiIconData dmi;
	private static DmObjectData dm;
	private static string[] hierList = { };

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

	//	public ClothEnum cloth;
	private int clothingOffset = -1;
	public int clothingReference = -1;
	private string desc;

	private Dictionary<string, string> dmDic;
	//dm "w_class";

	//		dm datafile info
	private string hier;

	[SyncVar(hook = "ConstructItem")] public string hierarchy;
	private string icon;
	private string icon_state;
	private int inHandLeft = -1;

	public int inHandReferenceLeft;

	//reference numbers for item on inhands spritesheet. should be one corresponding to player facing down
	public int inHandReferenceRight;

	private int inHandRight = -1;
	private DmiIcon inventoryIcon;
	private string[] invSheetPaths;
	private string item_color;
	private string item_state;

	//dm "name"
	public string itemDescription;
	//the bare minimum you need to to make magic work

	// item name and description.
	public string itemName;

	public ItemType itemType = ItemType.None;
	private SpriteType masterType;
	private new string name;

	public ItemSize size;
	//dm "desc"

	public SpriteType spriteType;
	public ItemType type;

	/// throw-related fields
	[TooltipAttribute("How painful it is when someone throws it at you")] [Range(0,100)]
	public float throwDamage = 2;
	[TooltipAttribute("How many tiles to move per 0.1s when being thrown")]
	public float throwSpeed = 2;
	[TooltipAttribute("Max throw distance")]
	public float throwRange = 7;
	[TooltipAttribute("Damage when we click someone with harm intent")] [Range(0, 100)]
	public float hitDamage = 2;
	[TooltipAttribute("Sound to be played when we click someone with harm intent")]
	public string hitSound = "GenericHit";

	///<Summary>
	/// Can this item protect humans against spess?
	///</Summary>
	public bool evaCapable { get; private set; }

	public List<string> attackVerb = new List<string>();
	private static readonly char[] ListSplitters = new [] { ',', ' ' };

	public override void OnStartClient()
	{
		StartCoroutine(WaitForLoad());
		base.OnStartClient();
	}

	private IEnumerator WaitForLoad()
	{
		//		yield return new WaitForSeconds(2f);
		ConstructItem(hierarchy);
		yield return null;
	}

	//    Enum test:
	//
	//    private void OnEnable()
	//    {
	//        if (hierarchy == null || hierarchy.Equals(""))
	//        {
	//            hierarchy = cloth.GetDescription();
	//        }
	//        ConstructItem(hierarchy);
	//    }

	public float? TryParseFloat(string attr)
	{
		float i;
		return float.TryParse(tryGetAttr(attr), out i) ? (float?)i : null;
	}
	public List<string> TryParseList(string attr)
	{
		var list = new List<string>();
		list.AddRange(tryGetAttr(attr).Trim().Replace("list(", "").Replace(")", "").Split(ListSplitters, StringSplitOptions.RemoveEmptyEntries));
		return list;
	}

	public void ConstructItem(string hierString)
	{
		//randomize clothing!
		randomizeClothHierIfEmpty();

		//don't do anything if hierarchy string is empty
		hier = hierString.Trim();
		if (hier.Length == 0)
		{
			return;
		}

		//init datafiles
		if (!dmi)
		{
			//				Logger.Log("Item DMI data loading...");
			dmi = Resources.Load("DmiIconData")as DmiIconData;
		}
		if (!dm)
		{
			//				Logger.Log("Item DM data loading...");
			dm = Resources.Load("DmObjectData")as DmObjectData;
		}

		//raw dictionary of attributes
		dmDic = dm.getObject(hier);

		//basic attributes
		name = tryGetAttr("name");
		desc = tryGetAttr("desc");

		//custom inventory(?) icon, if present
		icon = tryGetAttr("icon");

		//			states
		icon_state = tryGetAttr("icon_state");
		item_color = tryGetAttr("item_color"); //also a state
		item_state = tryGetAttr("item_state");
		string[] states = { icon_state, item_color, item_state };

		throwDamage = TryParseFloat("throwforce") ?? throwDamage;
		throwSpeed = TryParseFloat("throw_speed") ?? throwSpeed;
		throwRange = TryParseFloat("throw_range") ?? throwRange;
		hitDamage = TryParseFloat("force") ?? hitDamage;
		attackVerb = TryParseList("attack_verb") ?? attackVerb;

		masterType = getMasterType(hier); // aka SpriteType
		itemType = getItemType(hier, getInvIconPrefix(masterType));
		invSheetPaths = getItemClothSheetHier(itemType);
		//			size = getItemSize(tryGetAttr("w_class"));
		int[] inHandOffsets = tryGetInHand();
		inHandLeft = inHandOffsets[0];
		inHandRight = inHandOffsets[1];
		inventoryIcon = tryGetInventoryIcon();
		clothingOffset = tryGetClothingOffset(states);

		//determine item type via sheet name if hier name failed
		if (itemType == ItemType.None)
		{
			itemType = getItemType(inventoryIcon.getName());
		}

		//inventory item sprite
		Sprite stateSprite = tryGetStateSprite(inventoryIcon, icon_state);

		//finally setting things
		inHandReferenceLeft = inHandLeft;
		inHandReferenceRight = inHandRight;
		clothingReference = clothingOffset;
		type = itemType;
		itemName = name;
		itemDescription = desc;
		spriteType = masterType;
		GetComponentInChildren<SpriteRenderer>().sprite = stateSprite;

		//			Logger.Log(name + " size=" + size + " type=" + type + " spriteType="
		//			          + spriteType + " (" + desc + ") : "
		//			          + icon_state + " / " + item_state + " / C: " + clothingReference
		//			          + ", L: " + inHandReferenceLeft + ", R: " + inHandReferenceRight + ", I: " + inventoryIcon.icon + '\n'
		//			          +	dmDic.Keys.Aggregate("", (current, key) => current + (key + ": ") + dmDic[key] + "\n"));
		CheckEvaCapatibility();
	}

	private void CheckEvaCapatibility()
	{
		if (hier.Contains("/obj/item/clothing/head/helmet/space/hardsuit/") ||
			hier.Contains("/obj/item/clothing/suit/space/hardsuit/"))
		{
			evaCapable = true;
		}
		else
		{
			evaCapable = false;
		}
	}

	private static Sprite tryGetStateSprite(DmiIcon dmiIcon, string icon_state)
	{
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

	[ContextMenu("GetItemInfo")]
	private void DebugInfo()
	{
		//Use this method to retrieve item info at runtime (right click the component from editor)
		//	Debug.Log(getItemDebugInfo());
		Debug.Log("hier: " + hier);
		Debug.Log("is server: " + isServer);
		Debug.Log("is eva capable: " + evaCapable);
	}
	private string getItemDebugInfo()
	{
		return string.Format(
			$"name={name}, type={itemType}, spriteType={spriteType} ({desc}) : {icon_state} / {item_state} / " +
			$"C: {clothingReference}, L: {inHandLeft}, R: {inHandRight}, I: {inventoryIcon.icon}{'\n'}" +
			$"{dmDic.Keys.Aggregate("", (current, key) => current + key + ": " + dmDic[key] + "\n")}");
	}

	private static SpriteType getMasterType(string hs)
	{
		if (hs.StartsWith(ObjItemClothing))
		{
			return SpriteType.Clothing;
		}

		return SpriteType.Items;
	}

	private static string getMasterTypeHandsString(SpriteType masterType)
	{
		switch (masterType)
		{
			case SpriteType.Clothing:
				return "clothing";
			default:
				return "items";
		}
	}

	private string tryGetAttr(string key)
	{
		return tryGetAttr(dmDic, key);
	}

	public static string tryGetAttr(Dictionary<string, string> dmDic, string key)
	{
		if (dmDic != null && dmDic.ContainsKey(key))
		{
			return dmDic[key];
		}
		//			Logger.Log("tryGetAttr fail using key: " + key);
		return "";
	}

	public bool hasDataLoaded()
	{
		return dm != null && dmi != null;
	}

	private /*static*/ DmiIcon tryGetInventoryIcon( /*DmiIconData dmi, string[] invSheetPaths, string icon = ""*/ )
	{
		//determining invIcon
		for (int i = 0; i < invSheetPaths.Length; i++)
		{
			string iconPath = DmiIconData.getIconPath(invSheetPaths[i]); //add extension junk
			if (!iconPath.Equals("") && DmiIconData.Data.ContainsKey(iconPath) && icon.Equals(""))
			{
				//					Logger.Log(name + ": iSheet = dmi.DataHier[" + iconPath +"] = " + dmi.Data[iconPath]);
				return DmiIconData.Data[iconPath];
			}
		}

		if (!icon.Equals(""))
		{
			//				Logger.Log(name + ": iSheet = dmi.DataIcon["+icon+"] = "+iSheet);
			return DmiIconData.Data[icon];
		}
		//pretty bad choice, should use this only as last resort as it's usually pretty inaccurate
		DmiIcon invIcon = dmi.getIconByState(icon_state);
		if (invIcon != null)
		{

			Logger.Log($"{name} is doing bad dmi.getIconByState({icon_state}) = {invIcon.icon}", Category.DmMetadata);
			return invIcon;
		}
		//			Logger.LogError();
		return new DmiIcon();
	}

	//getting stuff from whatever states provided (expected order is item_state, item_color, icon_state)
	private /*static*/ int tryGetClothingOffset(string[] states)
	{
		string[] onPlayerClothSheetHier = getOnPlayerClothSheetHier(itemType);
		for (int i = 0; i < states.Length; i++)
		{
			if (!states[i].Equals(""))
			{
				DmiState state = dmi.searchStateInIcon(states[i], itemType == ItemType.None ? onPlayer : onPlayerClothSheetHier, 4, false);
				if (state != null)
				{
					return state.offset;
				}
			}
		}

		//Logger.LogError("No clothing offset found!  ClothHier=" + onPlayerClothSheetHier[0] + ", " + getItemDebugInfo());
		return -1;
	}

	private /*static*/ int[] tryGetInHand()
	{
		if (item_state.Equals(""))
		{
			return new [] {-1, -1 };
		}

		string searchString = getMasterTypeHandsString(masterType);

		DmiState stateLH = dmi.searchStateInIconShallow(item_state, "mob/inhands/" + searchString + "_lefthand");

		DmiState stateRH = dmi.searchStateInIconShallow(item_state, "mob/inhands/" + searchString + "_righthand");

		return new [] { stateLH == null ? -1 : stateLH.offset, stateRH == null ? -1 : stateRH.offset };
	}

	private static string getInvIconPrefix(SpriteType st)
	{
		switch (st)
		{
			case SpriteType.Clothing:
				return ObjItemClothing;
			default:
				return "";
		}
	}

	private static string[] getItemClothSheetHier(ItemType type)
	{
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

	private static string[] getOnPlayerClothSheetHier(ItemType type)
	{
		string p = "mob/";
		switch (type)
		{
			case ItemType.Belt:
				return new [] { p + "belt", p + "belt_mirror" };
			case ItemType.Back:
				return new [] { p + "back" };
			case ItemType.Glasses:
				return new [] { p + "eyes" };
			case ItemType.Gloves:
				return new [] { p + "hands" };
			case ItemType.Hat:
				return new [] { p + "head" };
			case ItemType.Ear:
				return new [] { p + "ears" };
			case ItemType.Mask:
				return new [] { p + "mask" };
			case ItemType.Shoes:
				return new [] { p + "feet" };
			case ItemType.Suit:
				return new [] { p + "suit" };
			case ItemType.Neck:
				return new [] { p + "ties", p + "neck" };
			case ItemType.Uniform:
				return new [] { p + "uniform" };
			default:
				return new [] { "" };
		}
	}

	private /*static*/ void randomizeClothHierIfEmpty()
	{
		if (hierList.Length == 0)
		{
			TextAsset asset = Resources.Load(Path.Combine("metadata", "hier"))as TextAsset;
			if (asset != null)
			{
				List<string> objects = asset.text.Split('\n').ToList();
				objects.RemoveAll(x => !x.Contains("cloth"));
				hierList = objects.ToArray();
			}
			//        Logger.Log("HierList loaded. size=" + hierList.Length);
		}
		if (hierarchy.Length == 0 && spriteType == SpriteType.Clothing)
		{
			hierarchy = hierList[new Random().Next(hierList.Length)];
		}
	}

	private static ItemType getItemType(string s, string cutOff = "")
	{
		//	Logger.Log("getItemType for " + s);
		string sCut;
		if (!cutOff.Equals("") && s.StartsWith(cutOff))
		{
			sCut = s.Substring(cutOff.Length + 1).Split('/')[0];
			//				Logger.Log("sCut = "+ sCut);
		}
		else
		{
			if (s.Contains("storage"))
			{
				sCut = "back";
			}
			else
			{
				//All other unknowns:
				sCut = s;
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

	private static ItemSize getItemSize(string s)
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

	//Below methods add a code to the start of the sprite reference to indicate which spritesheet to use:
	//1 = items
	//2 = clothing
	//3 = guns
	public int NetworkInHandRefLeft()
	{
		if (inHandReferenceLeft == -1)
		{
			return -1;
		}

		string code = SpriteTypeCode();
		string newRef = code + inHandReferenceLeft;
		int i = -1;
		int.TryParse(newRef, out i);
		return i;
	}

	public int NetworkInHandRefRight()
	{
		if (inHandReferenceRight == -1)
		{
			return -1;
		}

		string code = SpriteTypeCode();
		string newRef = code + inHandReferenceRight;
		int i = -1;
		int.TryParse(newRef, out i);
		return i;
	}

	private string SpriteTypeCode()
	{
		int i = -1;
		switch (spriteType)
		{
			case SpriteType.Items:
				i = 1;
				break;
			case SpriteType.Clothing:
				i = 2;
				break;
			case SpriteType.Guns:
				i = 3;
				break;
		}
		return i.ToString();
	}

	public void OnHoverStart()
	{
		// Show the parenthesis for an item's description only if the item has a description
		if (itemDescription.Length > 0)
		{
			UIManager.SetToolTip = itemName + " (" + itemDescription + ")";
		}
		else
		{
			UIManager.SetToolTip = itemName;
		}
	}

	public void OnHoverEnd()
	{
		UIManager.SetToolTip = "";
	}

	// Sends examine event to all monobehaviors on gameobject
	public void SendExamine()
	{
		SendMessage("OnExamine");
	}

	// When right clicking on an item, examine the item
	public void OnHover()
	{
		if (Input.GetMouseButtonDown(1))
		{
			SendExamine();
		}
	}

	[ContextMethod("Examine", "Magnifying_glass")]
	public void OnExamine()
	{
		if (!string.IsNullOrEmpty(itemDescription))
		{
			ChatRelay.Instance.AddToChatLogClient(itemDescription, ChatChannel.Examine);
		}
	}
}