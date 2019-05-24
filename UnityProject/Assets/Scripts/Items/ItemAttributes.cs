using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using Random = System.Random;

[RequireComponent(typeof(ObjectBehaviour))]
public class ItemAttributes : NetworkBehaviour
{
	private const string MaskInternalsFlag = "MASKINTERNALS";
	private const string ObjItemClothing = "/obj/item/clothing";
	private static DmiIconData dmi;
	private static DmObjectData dm;

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

	public int clothingReference = -1;
	/// <summary>
	/// Raw dictionary of attributes
	/// </summary>
	private Dictionary<string, string> dmDic;
	private string hier;

	[SyncVar(hook = "ConstructItem")] public string hierarchy;
	/// <summary>
	/// Custom inventory(?) icon, if present
	/// </summary>
	private string icon;
	private string icon_state;

	//reference numbers for item on inhands spritesheet. should be one corresponding to player facing down
	public int inHandReferenceLeft;
	public int inHandReferenceRight;

	private DmiIcon inventoryIcon;
	private string[] invSheetPaths;
	private string item_color;
	private string item_state;

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

	/// <summary>
	/// Invoked when cloned copies the item attribute hier
	/// </summary>
	/// <param name="fromObject">Object to copy <see cref="hierarchy"/> from</param>
	private void OnClonedServer(GameObject fromObject)
	{
		hierarchy = fromObject.GetComponent<ItemAttributes>().hierarchy;
	}

	public float? TryParseFloat(string attr)
	{
		return float.TryParse(TryGetAttr(attr), out float i) ? (float?)i : null;
	}

	public List<string> TryParseList(string attr)
	{
		return
			TryGetAttr(attr)
			.Trim()
			.Replace("list(", "")
			.Replace(")", "")
			.Split(ListSplitters, StringSplitOptions.RemoveEmptyEntries)
			.ToList();
	}

	public void ConstructItem(string hierString)
	{
		//randomize clothing!
		RandomizeClothHierIfEmpty();

		//don't do anything if hierarchy string is empty
		hier = hierString.Trim();
		if (hier.Length == 0)
		{
			return;
		}

		//init datafiles
		if (!dmi)
		{
			dmi = Resources.Load("DmiIconData")as DmiIconData;
		}
		if (!dm)
		{
			dm = Resources.Load("DmObjectData")as DmObjectData;
		}

		dmDic = dm.getObject(hier);

		//basic attributes
		itemName = TryGetAttr("name");
		itemDescription = TryGetAttr("desc");

		icon = TryGetAttr("icon");

		//states
		icon_state = TryGetAttr("icon_state");
		item_color = TryGetAttr("item_color"); //also a state
		item_state = TryGetAttr("item_state");
		string[] states = { icon_state, item_color, item_state };

		throwDamage = TryParseFloat("throwforce" ) ?? throwDamage;
		throwSpeed  = TryParseFloat("throw_speed") ?? throwSpeed;
		throwRange  = TryParseFloat("throw_range") ?? throwRange;
		hitDamage   = TryParseFloat("force"      ) ?? hitDamage;
		attackVerb  = TryParseList ("attack_verb") ?? attackVerb;

		spriteType    = UniItemUtils.GetMasterType(hier);
		itemType      = UniItemUtils.GetItemType(hier);
		invSheetPaths = UniItemUtils.GetItemClothSheetHier(itemType);

		int[] inHandOffsets = TryGetInHand();
		inHandReferenceLeft  = inHandOffsets[0];
		inHandReferenceRight = inHandOffsets[1];

		inventoryIcon = UniItemUtils.GetInventoryIcon(hier, invSheetPaths, icon, icon_state);

		clothingReference = TryGetClothingOffset(states);

		//determine item type via sheet name if hier name failed
		if (itemType == ItemType.None)
		{
			itemType = UniItemUtils.GetItemType(inventoryIcon.getName());
		}

		CanConnectToTank = TryGetConnectedToTank();

		//inventory item sprite
		Sprite stateSprite = UniItemUtils.TryGetStateSprite(inventoryIcon, icon_state);

		var childSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
		childSpriteRenderer.sprite = stateSprite;
		//assign an order in layer so we don't have arbitrary ordering
		childSpriteRenderer.sortingOrder = clothingReference;

		CheckEvaCapatibility();
	}

	/// <summary>
	/// Determines if mask is connected to tank.
	/// Does this based on wheter <see cref="MaskInternalsFlag"/> is present in <see cref="dmDic"/> flags
	/// </summary>
	/// <returns>Wheter it is connected or not</returns>
	private bool TryGetConnectedToTank()
	{
		if (!dmDic.ContainsKey("flags")) { return false; }

		string[] flags = dmDic["flags"].Split(',');

		return flags.Any(flag => flag.Trim().Equals(MaskInternalsFlag));
	}

	/// <summary>
	/// Updates <see cref="IsEVACapable"/>
	/// </summary>
	private void CheckEvaCapatibility()
	{
		IsEVACapable =
			hier.Contains("/obj/item/clothing/head/helmet/space/hardsuit/") ||
			hier.Contains("/obj/item/clothing/suit/space/hardsuit/");
	}

	/// <summary>
	/// Use this method to retrieve item info at runtime (right click the component from editor)
	/// </summary>
	[ContextMenu("GetItemInfo")]
	private void DebugInfo()
	{
		//Logger.Log(GetItemDebugInfo());
		Logger.Log("hier: " + hier, Category.DmMetadata);
		Logger.Log("is server: " + isServer, Category.DmMetadata);
		Logger.Log("is eva capable: " + IsEVACapable, Category.DmMetadata);
	}

	/// <summary>
	/// Item information summarized in a human readable format
	/// </summary>
	/// <returns>Formated item information</returns>
	private string GetItemDebugInfo()
	{
		return string.Format(
			$"name={itemName}, type={itemType}, spriteType={spriteType} ({itemDescription}) : {icon_state} / {item_state} / " +
			$"C: {clothingReference}, L: {inHandReferenceLeft}, R: {inHandReferenceRight}, I: {inventoryIcon.icon}{'\n'}" +
			$"{dmDic.Keys.Aggregate("", (current, key) => current + key + ": " + dmDic[key] + "\n")}");
	}

	private static string GetMasterTypeHandsString(SpriteType masterType)
	{
		switch (masterType)
		{
			case SpriteType.Clothing: return "clothing";

			default: return "items";
		}
	}

	private string TryGetAttr(string key)
	{
		return TryGetAttr(dmDic, key);
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
	private int TryGetClothingOffset(string[] states)
	{
		string[] onPlayerClothSheetHier = GetOnPlayerClothSheetHier(itemType);
		for (int i = 0; i < states.Length; i++)
		{
			if (String.IsNullOrEmpty(states[i])) continue;

			var icons = itemType == ItemType.None ?
					onPlayer :
					onPlayerClothSheetHier;

			DmiState state = dmi.searchStateInIcon(states[i], icons, 4, false);

			if (state == null) continue;

			return state.offset;
		}

		//Logger.LogError("No clothing offset found!  ClothHier=" + onPlayerClothSheetHier[0] + ", " + GetItemDebugInfo());
		return -1;
	}

	private int[] TryGetInHand()
	{
		if (String.IsNullOrEmpty(item_state))
		{
			return new[] { -1, -1 };
		}

		string searchString = GetMasterTypeHandsString(spriteType);
		DmiState stateLH = dmi.searchStateInIconShallow(item_state, $"mob/inhands/{searchString}_lefthand");
		DmiState stateRH = dmi.searchStateInIconShallow(item_state, $"mob/inhands/{searchString}_righthand");

		return new[]
		{
			stateLH == null ? -1 : stateLH.offset,
			stateRH == null ? -1 : stateRH.offset
		};
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
	/// <para>If <see cref="hierarchy"/> is null or empty and <see cref="spriteType"/> is of type <see cref="SpriteType.Clothing"/>
	/// sets the hierarchy to a randomly selected item from <see cref="HierList"/></para>
	/// <seealso cref="String.IsNullOrEmpty(string)"/>
	/// </summary>
	private void RandomizeClothHierIfEmpty()
	{
		if (String.IsNullOrEmpty(hierarchy) && spriteType == SpriteType.Clothing)
		{
			hierarchy = HierList[new Random().Next(HierList.Length)];
		}
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

	/// <summary>
	/// Adds a code to the start of the sprite reference to indicate which spritesheet to use
	/// </summary>
	/// <returns>The reference with the code pre-concatenated</returns>
	public int NetworkInHandRefLeft()
	{
		if (inHandReferenceLeft == -1)
		{
			return -1;
		}

		string newRef = SpriteTypeCode() + inHandReferenceLeft;
		return int.TryParse(newRef, out int i) ? i : -1;
	}

	/// <summary>
	/// Adds a code to the start of the sprite reference to indicate which spritesheet to use
	/// </summary>
	/// <returns>The reference with the code pre-concatenated</returns>
	public int NetworkInHandRefRight()
	{
		if (inHandReferenceRight == -1)
		{
			return -1;
		}

		string newRef = SpriteTypeCode() + inHandReferenceRight;
		return int.TryParse(newRef, out int i) ? i : -1;
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

	[ContextMethod("Examine", "Magnifying_glass")]
	public void OnExamine()
	{
		if (!string.IsNullOrEmpty(itemDescription))
		{
			ChatRelay.Instance.AddToChatLogClient(itemDescription, ChatChannel.Examine);
		}
	}
}