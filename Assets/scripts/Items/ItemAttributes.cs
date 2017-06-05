using System;
using UnityEngine;
using System.Collections;

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
		//todo: place dm and dmi scriptableobjects here
		private static DmiIconData dmi;
		private static DmObjectData dm;
		
		public string hierarchy; //the bare minimum you need to to make magic work
		
		// item name and description. still public in case you want to override datafiles
		public string itemName; //dm "name"
		public string itemDescription; //dm "desc"

		public SpriteType spriteType; 
		public ItemType type; 		  //perhaps replace this one with a list like /obj/item/clothing/suit ?
		
		//override reference numbers for item on  inhands spritesheet. should be one corresponding to player facing down
		public int inHandReferenceRight; // dmi "offset" from first found : dm's "item_state"; "icon_state" ..
		public int inHandReferenceLeft;  // .. from sheet "clothing/guns/items_righthand" (depends on InHandSpriteType)
		public int clothingReference = -1;

	    public ItemSize size; //dm "w_class"; 
	
//		dm datafile info
		private string name;
		private string desc;
		private string icon_state;
		private string item_state;
		//todo: use these in following methods instead!
		private int inHandLeft;
		private int inHandRight;
		private int clothing;
		
		
		private void OnEnable()
		{
			//todo init stuff here
			if (hierarchy.Length != 0)
			{
				if (!dmi)
				{
					Debug.LogWarning("DMI is null! loading it...");
					dmi = Resources.Load("DmiIconData") as DmiIconData;
				}
				if (!dm)
				{
					Debug.LogWarning("DM is null! loading it...");
					dm = Resources.Load("DmObjectData") as DmObjectData;
				}
			
				var dmDictionary = dm.getObject(hierarchy);
				
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
