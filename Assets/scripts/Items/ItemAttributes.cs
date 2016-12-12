using UnityEngine;
using System.Collections;


namespace UI {

    public enum ItemSize {
        Small, Medium, Large
    }

    [System.Serializable]
    public enum ItemType {
        None, Glasses, Hat, Neck, Mask, Ear, Suit, Armor, Gloves, Shoes, Belt, Bag, ID, PDA
    }

    public class ItemAttributes: MonoBehaviour {
        // item name. Only useful if you want to check if what item it is. (e.g. ExtinguisherCabinet)
        public string itemName;
        //reference numbers for item on  inhands spritesheet. should be one corresponding to player facing down
        public int inHandReferenceRight; 
        public int inHandReferenceLeft;

        //this and other reference ints should be read in from the master dictionary once we get all that shit figured out - for now it's hard coded
        //change values manually in the inspector -- wealth

        public ItemSize size;
        public ItemType type;
    }
}
