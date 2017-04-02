using UnityEngine;
using System.Collections;

namespace UI
{
	public enum ItemSize
	{
		Small,
		Medium,
		Large
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
		ID,PDA,Food,Knife
	}

	public class ItemAttributes: MonoBehaviour
	{
		// item name. Only useful if you want to check if what item it is. (e.g. ExtinguisherCabinet)
		public string itemName;
		//reference numbers for item on  inhands spritesheet. should be one corresponding to player facing down

		public SpriteType spriteType;

		public int inHandReferenceRight;
		public int inHandReferenceLeft;
		public int clothingReference = -1;

		//this and other reference ints should be read in from the master dictionary once we get all that shit figured out - for now it's hard coded
		//change values manually in the inspector -- wealth

		public ItemSize size;
		public ItemType type;

        //Below methods add a code to the start of the sprite reference to indicate which spritesheet to use:
		//1 = items
		//2 = clothing
		//3 = guns
		public int NetworkInHandRefLeft()
		{ 
			string code = SpriteTypeCode();
			string newRef = code + inHandReferenceLeft.ToString();
			int i = -1;
			int.TryParse(newRef, out i);
			return i; 
		}
			
		public int NetworkInHandRefRight()
		{ 
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
