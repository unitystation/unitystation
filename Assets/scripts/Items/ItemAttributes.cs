using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UI
{
	public enum ItemSize
	{ //w_class
		Tiny,
		Small,
		Medium, //Normal
		Large, //Bulky
		Huge
	}
	
//	public enum W_CLASS { //w_class
//		WEIGHT_CLASS_TINY, WEIGHT_CLASS_SMALL, 
//		WEIGHT_CLASS_NORMAL, WEIGHT_CLASS_BULKY, 
//		WEIGHT_CLASS_HUGE
//	}

	public enum SLOT_FLAGS {//slot_flags
		SLOT_BELT, SLOT_POCKET, SLOT_BACK, 
		SLOT_ID, SLOT_MASK, SLOT_NECK, 
		SLOT_EARS, SLOT_HEAD, ALL
	}
	
	public enum RESISTANCE_FLAGS { //resistance_flags
		FLAMMABLE, FIRE_PROOF, ACID_PROOF, 
		LAVA_PROOF, INDESTRUCTIBLE
	} 
	
	public enum ORIGIN_TECH {
		materials, magnets, engineering, 
		programming, combat, powerstorage, 
		biotech, syndicate, plasmatech, 
		bluespace, abductor
	}
	
	public enum FLAGS_INV {
		HIDEHAIR, HIDEEARS, HIDEFACE, 
		HIDEEYES, HIDEFACIALHAIR, HIDEGLOVES, 
		HIDESHOES, HIDEJUMPSUIT
	}

	public enum FLAGS_COVER {
		MASKCOVERSEYES, MASKCOVERSMOUTH, HEADCOVERSEYES, 
		HEADCOVERSMOUTH, GLASSESCOVERSEYES
	}
	
	public enum FLAGS {//flags 
		//visor_flags 
		CONDUCT, ABSTRACT, NODROP, DROPDEL, 
		NOBLUDGEON, MASKINTERNALS, BLOCK_GAS_SMOKE_EFFECT, 
		STOPSPRESSUREDMAGE, THICKMATERIAL, SS_NO_FIRE, 
		SS_NO_INIT, SS_BACKGROUND

	}
	
	
	public enum BODYPARTS {//body_parts_covered
		CHEST, GROIN, LEGS, 
		FEET, ARMS, HANDS
	}

	public enum SpriteType
	{
		Items,
		Clothing,
		Guns
	}

	[System.Serializable]
	public enum ItemType
	{
		None,Glasses,Hat,Neck,
		Mask,Ear,Suit,Uniform,
		Gloves,Shoes,Belt,Back,
		ID,PDA,Food,
		Knife,
		Gun
	}
	
	public class ItemAttributes: MonoBehaviour
	{
		private static DmiIconData dmi;
		private static DmObjectData dm;
		private static string[] hierList = {};
		
		public string hierarchy; //the bare minimum you need to to make magic work
		
		// item name and description.
		public string itemName; //dm "name"
		public string itemDescription; //dm "desc"

		public SpriteType spriteType; 
		public ItemType type; 		  
		
		//reference numbers for item on inhands spritesheet. should be one corresponding to player facing down
		public int inHandReferenceRight; 
		public int inHandReferenceLeft;  
		public int clothingReference = -1;

	    public ItemSize size; //dm "w_class"; 

		//		dm datafile info
		private string hier;
		private Dictionary<string, string> dmDic;
		private SpriteType masterType;
		private ItemType iType = ItemType.None;
		private DmiIcon inventoryIcon;
		private string[] invSheetPaths;
		private new string name;
		private string icon;
		private string desc;
		private string icon_state;
		private string item_state;
		private int inHandLeft = -1;
		private int inHandRight = -1;
		private int clothingOffset = -1;


		private void OnEnable()
		{			
			//todo: make more methods static
			
			//randomize clothing! uncomment only if you spawn without any clothes on!
			randomizeClothHierIfEmpty();

			//don't do anything if hierarchy string is empty
			hier = hierarchy.Trim();
			if (hier.Length == 0) return;
			
			//init datafiles
			if (!dmi)
			{
//				Debug.Log("Item DMI data loading...");
				dmi = Resources.Load("DmiIconData") as DmiIconData;
			}
			if (!dm)
			{
//				Debug.Log("Item DM data loading...");
				dm = Resources.Load("DmObjectData") as DmObjectData;
			}
				
			//raw dictionary of attributes
			dmDic = dm.getObject(hier);

			//basic attributes
			name = tryGetAttr("name");
			desc = tryGetAttr("desc");
			icon_state = tryGetAttr("icon_state");
			item_state = tryGetAttr("item_state");
			icon = tryGetAttr("icon");
			
			masterType = getMasterType(hier);
			iType = getItemType(hier, getInvIconPrefix(masterType));
			invSheetPaths = getItemClothSheetHier(iType);
//			size = getItemSize(tryGetAttr("w_class"));
			int[] inHandOffsets = tryGetInHand();
			inHandLeft = inHandOffsets[0];
			inHandRight = inHandOffsets[1];
            inventoryIcon = tryGetInventoryIcon();
			clothingOffset = tryGetClothingOffset();

			//determine item type via sheet name if hier name failed
			if (iType == ItemType.None)
			{
				iType = getItemType(inventoryIcon.getName());
			}
			
			//inventory item sprite
			DmiState iState = inventoryIcon.getState(icon_state);
			Sprite stateSprite = inventoryIcon.spriteSheet[iState.offset];
				
			//finally setting things
			inHandReferenceLeft = inHandLeft;
			inHandReferenceRight = inHandRight;
			clothingReference = clothingOffset;
			type = iType;
			itemName = name;
			itemDescription = desc;
			GetComponentInChildren<SpriteRenderer>().sprite = stateSprite;
				
//			Debug.Log(name + " size=" + size + " type=" + type + " spriteType=" 
//			          + spriteType + " (" + desc + ") : " 
//			          + icon_state + " / " + item_state + " / C: " + clothingReference 
//			          + ", L: " + inHandReferenceLeft + ", R: " + inHandReferenceRight + ", I: " + inventoryIcon.icon + '\n'
//			          +	dmDic.Keys.Aggregate("", (current, key) => current + (key + ": ") + dmDic[key] + "\n"));
		}

		private static SpriteType getMasterType(string hs)
		{
			if(hs.StartsWith(ObjItemClothing)) return SpriteType.Clothing;
			return SpriteType.Items;
		}

		private static string getMasterTypeHandsString(SpriteType masterType)
		{
			switch (masterType)
			{
				case SpriteType.Clothing: return "clothing";
				default: return "items";
			}
		}

		private string tryGetAttr(string key)
		{
			return tryGetAttr(dmDic, key);
		}

		private static string tryGetAttr(Dictionary<string, string> dmDic, string key)
		{
			if (dmDic != null && dmDic.ContainsKey(key))
			{
				return dmDic[key];
			}
//			Debug.Log("tryGetAttr fail using key: " + key);
			return "";
		}

		private /*static*/ DmiIcon tryGetInventoryIcon(/*DmiIconData dmi, string[] invSheetPaths, string icon = ""*/)
		{
			//determining invIcon
			for (int i = 0; i < invSheetPaths.Length; i++)
			{
				var iconPath = DmiIconData.getIconPath(invSheetPaths[i]); //add extension junk
				if (!iconPath.Equals("") && dmi.Data.ContainsKey(iconPath) && icon.Equals(""))
				{
//					Debug.Log(name + ": iSheet = dmi.DataHier[" + iconPath +"] = " + dmi.Data[iconPath]);
					return dmi.Data[iconPath];
				}
			}
			
			if (!icon.Equals(""))
			{
//				Debug.Log(name + ": iSheet = dmi.DataIcon["+icon+"] = "+iSheet);
				return dmi.Data[icon];
			}
			//pretty bad choice, should use this only as last resort as it's usually pretty inaccurate
			var invIcon = dmi.getIconByState(icon_state);
			Debug.LogWarning(name + " is doing bad dmi.getIconByState(" + icon_state + ") = " + invIcon);
			return invIcon;
		}

		private /*static*/ int tryGetClothingOffset()
		{
		//getting stuff using item_state
			if (!item_state.Equals(""))
			{
				//clothingReference get attempt for item_state
				var state = dmi.searchStateInIcon(item_state, onPlayer, 4, false);
				if (state != null)
				{
					var clothOffset = state.offset;
//					if(clothOffset != -1) Debug.Log(name + " STATE1 " + clothOffset); return clothOffset;
				}
			}
			//clothingReference get attempt for icon_state, if item_state failed
			//if we know exact item type, search state in corresponding icon, otherwise lookup in onPlayer list
			var onPlayerClothSheetHier = getOnPlayerClothSheetHier(iType);
			var state2 = dmi.searchStateInIcon(icon_state, iType == ItemType.None ? onPlayer : onPlayerClothSheetHier, 4, false);
			if (state2 != null)
			{
//				Debug.Log(name + " STATE2 " + state2.offset);
				return state2.offset;
			}
			Debug.LogError(name + ": No clothing offset found! iType=" + iType 
			               + ", SheetHier=" + onPlayerClothSheetHier[0]);
			return -1;
		}

		private /*static*/ int[] tryGetInHand()
		{
			if (item_state.Equals("")) return new[] {-1, -1};
			
			var stateLH = dmi.searchStateInIconShallow(item_state,
				"mob/inhands/" + getMasterTypeHandsString(masterType) + "_lefthand");

			var stateRH = dmi.searchStateInIconShallow(item_state,
				"mob/inhands/" + getMasterTypeHandsString(masterType) + "_righthand");
			
			return new[] {stateLH == null ? -1 : stateLH.offset,
						  stateRH == null ? -1 : stateRH.offset};
		}

		private static string getInvIconPrefix(SpriteType st)
		{
			switch (st)
			{
					case SpriteType.Clothing: return ObjItemClothing;
						default: return "";
			}
		}

		private static string[] getItemClothSheetHier(ItemType type)
		{
			var p = "obj/clothing/";
			switch (type)
			{
				case ItemType.Belt: return new[] {p + "belts"};
				case ItemType.Back: return new[] {p + "cloaks"};
				case ItemType.Glasses: return new[] {p + "glasses"};
				case ItemType.Gloves: return new[] {p + "gloves"};
				case ItemType.Hat: return new[] {p + "hats"};
				case ItemType.Mask: return new[] {p + "masks"};
				case ItemType.Shoes: return new[] {p + "shoes"};
				case ItemType.Suit: return new[]
				{
					p + "suits",
					p + "neck"
				};
				case ItemType.Neck: return new[] {p + "ties"};
				case ItemType.Uniform: return new[] {p + "uniforms"};
				default:	return new[] {""};
			}
		}
		private static string[] getOnPlayerClothSheetHier(ItemType type)
		{
			var p = "mob/";
			switch (type)
			{
				case ItemType.Belt: return new[]
				{
					p + "belt",
					p + "belt_mirror"
				};
				case ItemType.Back: return new[] {p + "back"};
				case ItemType.Glasses: return new[] {p + "eyes"};
				case ItemType.Gloves: return new[] {p + "hands"};
				case ItemType.Hat: return new[] {p + "head"};
				case ItemType.Ear: return new[] {p + "ears"};
				case ItemType.Mask: return new[] {p + "mask"};
				case ItemType.Shoes: return new[] {p + "feet"};
				case ItemType.Suit: return new[] {p + "suit"};
				case ItemType.Neck: return new[]
				{
					p + "ties",
					p + "neck"
				};
				case ItemType.Uniform: return new[] {p + "uniform"};
				default:	return new[] {""};
			}
		}
		
		//on-player references
		private static readonly string[] onPlayer =
		{
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

		private const string ObjItemClothing = "/obj/item/clothing";

		private /*static*/ void randomizeClothHierIfEmpty()
		{
			if (hierList.Length == 0)
			{
				var asset = Resources.Load(Path.Combine("metadata", "hier")) as TextAsset;
				if (asset != null)
				{
					var objects = asset.text.Split('\n').ToList();
					objects.RemoveAll(x => !x.Contains("cloth"));
					hierList = objects.ToArray();
				}
				Debug.Log("HierList loaded. size=" + hierList.Length);
			}
			if (hierarchy.Length == 0 && spriteType == SpriteType.Clothing)
			{
				hierarchy = hierList[new System.Random().Next(hierList.Length)];
			}
		}

		private static ItemType getItemType(string s, string cutOff = "")
		{
//			Debug.Log("getItemType for "+ s);
			string sCut;
			if (!cutOff.Equals("") && s.StartsWith(cutOff))
			{

				sCut = s.Substring(cutOff.Length + 1).Split('/')[0];
//				Debug.Log("sCut = "+ sCut);
			}
			else
			{
				sCut = s;
			}
			switch (sCut)
			{
					case "uniform": 
					case "uniforms": 
					case "under": 
					case "underwear": return ItemType.Uniform;
					case "back":
					case "cloaks": return ItemType.Back;
					case "belt_mirror": 
					case "belt": 
					case "belts": return ItemType.Belt;
					case "eyes": 
					case "glasses": return ItemType.Glasses;
					case "ears": return ItemType.Ear;
					case "gloves": 
					case "hands": return ItemType.Gloves;
					case "shoes": 
					case "feet": return ItemType.Shoes;
					case "head": 
					case "hats": return ItemType.Hat;
					case "mask": 
					case "masks": return ItemType.Mask;
					case "tie": 
					case "ties": 
					case "neck": return ItemType.Neck;
					case "suit": 
					case "flightsuit": 
					case "suits": return ItemType.Suit;
						default: return ItemType.None;
			}
		}

		private static ItemSize getItemSize(string s)
		{
			switch (s)
			{
					case "WEIGHT_CLASS_TINY": return ItemSize.Tiny;
					case "WEIGHT_CLASS_SMALL": return ItemSize.Small;
					case "WEIGHT_CLASS_NORMAL": return ItemSize.Medium;
					case "WEIGHT_CLASS_BULKY": return ItemSize.Large;
					case "WEIGHT_CLASS_HUGE": return ItemSize.Huge;
						default: return ItemSize.Small;
			}
		}

		//Below methods add a code to the start of the sprite reference to indicate which spritesheet to use:
		//1 = items
		//2 = clothing
		//3 = guns
		public int NetworkInHandRefLeft()
		{ 
			if (inHandReferenceLeft == -1)
				return -1;
			
			string code = SpriteTypeCode();
			string newRef = code + inHandReferenceLeft.ToString();
			int i = -1;
			int.TryParse(newRef, out i);
			return i; 
		}
			
		public int NetworkInHandRefRight()
		{ 
			if (inHandReferenceRight == -1)
				return -1;
			
			string code = SpriteTypeCode();
			string newRef = code + inHandReferenceRight.ToString();
			int i = -1;
			int.TryParse(newRef, out i);
			return i; 
		}

		private string SpriteTypeCode(){
			int i = -1;
			switch (spriteType) {
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
	}
}
