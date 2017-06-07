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
		private ItemType iType = ItemType.None;
		private new string name;
		private string desc;
		private string icon_state;
		private string item_state;
		private int inHandLeft = -1;
		private int inHandRight = -1;
		private int clothing = -1;

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
		
		private string tryGetAttr(string key)
		{
			if (dmDic != null && dmDic.ContainsKey(key))
			{
				return dmDic[key];
			}
//			Debug.Log("tryGetAttr fail using key: " + key);
			return "";
		}

		private void OnEnable()
		{
			//todo loader component?
			
			//randomize clothing! uncomment only if you spawn without any clothes on!
			//randomizeClothHierIfEmpty();

			//don't do anything if hierarchy string is empty
			hier = hierarchy.Trim();
			if (hier.Length == 0) return;
			
			//init datafiles
			if (!dmi)
			{
				Debug.Log("Item DMI data loading...");
				dmi = Resources.Load("DmiIconData") as DmiIconData;
			}
			if (!dm)
			{
				Debug.Log("Item DM data loading...");
				dm = Resources.Load("DmObjectData") as DmObjectData;
			}
				
			//raw dictionary of attributes
			dmDic = dm.getObject(hier);

			//basic attributes
			name = tryGetAttr("name");
			desc = tryGetAttr("desc");
			icon_state = tryGetAttr("icon_state");
			string icon = tryGetAttr("icon");
				
			//inhands
			item_state = tryGetAttr("item_state");
			if (!item_state.Equals(""))
			{
				var stateLH = dmi.searchStateInIconShallow(item_state, "mob/inhands/clothing_lefthand");
				if (stateLH != null)
				{
					inHandLeft = stateLH.offset;
				}

				var stateRH = dmi.searchStateInIconShallow(item_state, "mob/inhands/clothing_righthand");
				if (stateRH != null)
				{
					inHandRight = stateRH.offset;
				}
					
				//clothingReference get attempt for item_state
				var searchStateInIcon = dmi.searchStateInIcon(item_state, onPlayer, 4, false);
				if (searchStateInIcon != null)
				{
					clothing = searchStateInIcon.offset;
				}
			}
			
			//itemSize
//			size = getItemSize(tryGetAttr("w_class"));
				
			//DmiIcon for dropped/inventory, the item type might be determined from its name:
			DmiIcon iSheet = new DmiIcon();

			string iSheetHier = "";
			
			//clothing stuff				
			var clothPath = "/obj/item/clothing";
			if (hier.StartsWith(clothPath))
			{
				//clothingReference get attempt for icon_state, if item_state failed
				if (clothing == -1)
				{
					var searchStateInIcon = dmi.searchStateInIcon(icon_state, onPlayer, 4, false);
					if (searchStateInIcon != null)
					{
						clothing = searchStateInIcon.offset;
					}
				}

				iType = getItemType(hier, clothPath);
				iSheetHier = getItemClothSheetHier(iType);
//				Debug.Log(name + " iSheetHier = "+iSheetHier);
			}
			
			//todo: blocks for other item types?
			
			//determining iSheet
			if (!iSheetHier.Equals("") && dmi.Data.ContainsKey(iSheetHier+".dmi") && icon.Equals(""))
			{
				iSheet = dmi.Data[iSheetHier+".dmi"];
//				Debug.Log(name + ": iSheet = dmi.DataHier["+iSheetHier+".dmi"+"] = "+iSheet);
			}
			else if (!icon.Equals(""))
			{
				iSheet = dmi.Data[icon];
//				Debug.Log(name + ": iSheet = dmi.DataIcon["+icon+"] = "+iSheet);
			}
			else
			{ //pretty bad choice, should use this as last resort
				iSheet = dmi.getIconByState(icon_state);
//				Debug.Log(name + ": iSheet = dmi.getIconByState("+icon_state+") = "+iSheet);
			}
				
			//determine item type via sheet name if hier name failed
			if (iType == ItemType.None)
			{
				iType = getItemType(iSheet.getName());
			}
				
			//inventory item sprite
			DmiState iState = iSheet.getState(icon_state);
			Sprite stateSprite = iSheet.spriteSheet[iState.offset];
				
				
			//finally setting things
			inHandReferenceLeft = inHandLeft;
			inHandReferenceRight = inHandRight;
			clothingReference = clothing;
			type = iType;
			itemName = name;
			itemDescription = desc;
			GetComponentInChildren<SpriteRenderer>().sprite = stateSprite;
				
			Debug.Log(name + " size=" + size + " type=" + type + " spriteType=" 
			          + spriteType + " (" + desc + ") : " 
			          + icon_state + " / " + item_state + " / C: " + clothingReference 
			          + ", L: " + inHandReferenceLeft + ", R: " + inHandReferenceRight + ", I: " + iSheet.icon + '\n'
			          +	dmDic.Keys.Aggregate("", (current, key) => current + (key + ": ") + dmDic[key] + "\n"));
		}

		private static string getItemClothSheetHier(ItemType type)
		{
			var p = "icons/obj/clothing/";
			switch (type)
			{
				case ItemType.Belt: return p + "belts";
				case ItemType.Back: return p + "cloaks";
				case ItemType.Glasses: return p + "glasses";
				case ItemType.Gloves: return p + "gloves";
				case ItemType.Hat: return p + "hats";
				case ItemType.Mask: return p + "masks";
				case ItemType.Shoes: return p + "shoes";
				case ItemType.Suit: return p + "suits";
//				case ItemType.Neck: return p + "neck"; //not sure what to do with this one
				case ItemType.Neck: return p + "ties";
				case ItemType.Uniform: return p + "uniforms";
				default:	return "";
			}

		}

		private void randomizeClothHierIfEmpty()
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
			Debug.Log("getItemType for "+ s);
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
