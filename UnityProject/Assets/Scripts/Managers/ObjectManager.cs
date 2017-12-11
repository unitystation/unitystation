using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PlayGroup;
using UI;

public class ObjectManager : MonoBehaviour
{
    private static ObjectManager objectManager;

    public static ObjectManager Instance
    {
        get
        {
            if (!objectManager)
            {
                objectManager = FindObjectOfType<ObjectManager>();
            }
            return objectManager;
        }
    }

    //Factories will only be available serverside, referencing from client will return null exception
    public static ClothFactory clothFactory { get; private set; }
    [Header("How many prefabs to preload")]
    public int preLoadClothAmount = 15;

    //Server only
    public static void StartPoolManager()
    {
        GameObject pM = (GameObject)Instantiate(Resources.Load("PoolManager") as GameObject);
        if (clothFactory != null || !CustomNetworkManager.Instance._isServer)
            return;

        clothFactory = pM.GetComponent<ClothFactory>();

        //Preload to save on Instantiation during gameplay
        ClothFactory.Instance.PreLoadCloth(Instance.preLoadClothAmount);
    }
}

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

public enum SLOT_FLAGS
{//slot_flags
    SLOT_BELT, SLOT_POCKET, SLOT_BACK,
    SLOT_ID, SLOT_MASK, SLOT_NECK,
    SLOT_EARS, SLOT_HEAD, ALL
}

public enum RESISTANCE_FLAGS
{ //resistance_flags
    FLAMMABLE, FIRE_PROOF, ACID_PROOF,
    LAVA_PROOF, INDESTRUCTIBLE
}

public enum ORIGIN_TECH
{
    materials, magnets, engineering,
    programming, combat, powerstorage,
    biotech, syndicate, plasmatech,
    bluespace, abductor
}

public enum FLAGS_INV
{
    HIDEHAIR, HIDEEARS, HIDEFACE,
    HIDEEYES, HIDEFACIALHAIR, HIDEGLOVES,
    HIDESHOES, HIDEJUMPSUIT
}

public enum FLAGS_COVER
{
    MASKCOVERSEYES, MASKCOVERSMOUTH, HEADCOVERSEYES,
    HEADCOVERSMOUTH, GLASSESCOVERSEYES
}

public enum FLAGS
{//flags 
 //visor_flags 
    CONDUCT, ABSTRACT, NODROP, DROPDEL,
    NOBLUDGEON, MASKINTERNALS, BLOCK_GAS_SMOKE_EFFECT,
    STOPSPRESSUREDMAGE, THICKMATERIAL, SS_NO_FIRE,
    SS_NO_INIT, SS_BACKGROUND

}


public enum BODYPARTS
{//body_parts_covered
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
    None, Glasses, Hat, Neck,
    Mask, Ear, Suit, Uniform,
    Gloves, Shoes, Belt, Back,
    ID, PDA, Food,
    Knife,
    Gun
}
