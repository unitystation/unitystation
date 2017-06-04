using System;
using UnityEngine;
using System.Collections;

namespace UI
{
	public enum ItemSize
	{ //w_class
		Tiny,
		Small,
		Normal,
		Bulky,
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

	public enum InHandSpriteType
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
		
		// item name and description. for future tooltip.
		public readonly string itemName; //dm "name"
		public readonly string itemDescription; //dm "desc"

		public InHandSpriteType inHandSpriteType; 
		public ItemType type; 		  //todo perhaps replace this one with a list like /obj/item/clothing/suit ?
		
		//reference numbers for item on  inhands spritesheet. should be one corresponding to player facing down
		public readonly int inHandReferenceRight; // dmi "offset" from first found : dm's "item_state"; "icon_state" ..
		public readonly int inHandReferenceLeft;  // .. from sheet "clothing/guns/items_righthand" (depends on InHandSpriteType)
		public readonly int clothingReference = -1;

	    public ItemSize size; //dm "w_class"; todo replace by W_CLASS

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
			switch (inHandSpriteType) {
				case InHandSpriteType.Items:
					i = 1;
					break;
				case InHandSpriteType.Clothing:
					i = 2;
					break;
				case InHandSpriteType.Guns:
					i = 3;
					break;
			}
			return i.ToString();
		}
	}
}
