﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class Sprites
{
	private readonly Dictionary<string, Sprite[]> sprites = new Dictionary<string, Sprite[]>();

	public Sprite[] this [string key]
	{
		get
		{
			if (sprites.ContainsKey(key))
			{
				//					Logger.Log("Sprite found with key: " + key);
				return sprites[key];
			}
			Logger.Log("SPRITE MANAGER ERROR, KEY " + key + "NOT FOUND IN SPRITES");
			return null;
		}
		set { sprites[key] = value; }
	}
}

public class SpriteManager : MonoBehaviour
{
	private static SpriteManager spriteManager;
	private readonly Sprites bloodSprites = new Sprites();
	private readonly Sprites doorSprites = new Sprites();
	private readonly Sprites fireSprites = new Sprites();
	private readonly Sprites lightSprites = new Sprites();
	private readonly Sprites monitorSprites = new Sprites();
	private readonly Sprites playerSprites = new Sprites();
	private readonly Sprites screenUISprites = new Sprites();
	private readonly Sprites wallSprites = new Sprites();
	private readonly Sprites wireSprites = new Sprites();

	public DmiIconData dmi;

	public static SpriteManager Instance
	{
		get
		{
			if (!spriteManager)
			{
				spriteManager = FindObjectOfType<SpriteManager>();
			}
			return spriteManager;
		}
	}

	/// <summary>
	/// The shroud sprite. Used on the topLayer TileMap to hide things, like walls
	/// </summary>
	public Sprite shroudSprite;

	public static Sprites PlayerSprites => Instance.playerSprites;

	public static Sprites ConnectSprites => Instance.wallSprites;

	public static Sprites DoorSprites => Instance.doorSprites;

	public static Sprites MonitorSprites => Instance.monitorSprites;

	public static Sprites BloodSprites => Instance.bloodSprites;

	public static Sprites LightSprites => Instance.lightSprites;

	public static Sprites FireSprites => Instance.fireSprites;

	public static Sprites WireSprites => Instance.wireSprites;

	public static Sprites ScreenUISprites => Instance.screenUISprites;

	//Sprite Accessories:
	public TextAsset hairJson;
	public TextAsset facialHairJson;
	public TextAsset underwearJson;
	public TextAsset socksJson;
	//TODO undershirts (json data is available for it)

	private AccessoryCollection hairCollection;
	private AccessoryCollection facialHairCollection;
	private AccessoryCollection underwearCollection;
	private AccessoryCollection socksCollection;

	public static List<SpriteAccessory> HairCollection => Instance.hairCollection.spriteAccessories;
	public static List<SpriteAccessory> FacialHairCollection => Instance.facialHairCollection.spriteAccessories;
	public static List<SpriteAccessory> UnderwearCollection => Instance.underwearCollection.spriteAccessories;
	public static List<SpriteAccessory> SocksCollection => Instance.socksCollection.spriteAccessories;

	void Awake()
	{
		InitializeSpriteSheets();
		InitializeSpriteAccessories();
	}

	private void InitializeSpriteAccessories()
	{
		hairCollection = JsonUtility.FromJson<AccessoryCollection>(hairJson.text);
		facialHairCollection = JsonUtility.FromJson<AccessoryCollection>(facialHairJson.text);
		underwearCollection = JsonUtility.FromJson<AccessoryCollection>(underwearJson.text);
		socksCollection = JsonUtility.FromJson<AccessoryCollection>(socksJson.text);
	}
	private void InitializeSpriteSheets()
	{
		if (Instance.dmi == null)
		{
			Instance.dmi = Resources.Load("DmiIconData")as DmiIconData;
		}

		PlayerSprites["mob"] = dmi.getSprites("mob/mob");
		PlayerSprites["human"] = dmi.getSprites("mob/human");
		PlayerSprites["human_parts_greyscale"] = dmi.getSprites("mob/human_parts_greyscale");
		PlayerSprites["human_face"] = dmi.getSprites("mob/human_face");
		PlayerSprites["suit"] = dmi.getSprites("mob/suit");
		PlayerSprites["belt"] = dmi.getSprites("mob/belt");
		PlayerSprites["feet"] = dmi.getSprites("mob/feet");
		PlayerSprites["head"] = dmi.getSprites("mob/head");
		PlayerSprites["mask"] = dmi.getSprites("mob/mask");
		PlayerSprites["ears"] = dmi.getSprites("mob/ears");
		PlayerSprites["back"] = dmi.getSprites("mob/back");
		PlayerSprites["neck"] = dmi.getSprites("mob/ties"); //there is also mob/neck!
		PlayerSprites["eyes"] = dmi.getSprites("mob/eyes");
		PlayerSprites["hands"] = dmi.getSprites("mob/hands");
		PlayerSprites["uniform"] = dmi.getSprites("mob/uniform");
		PlayerSprites["underwear"] = dmi.getSprites("mob/underwear");
		PlayerSprites["guns_lefthand"] = dmi.getSprites("mob/inhands/guns_lefthand");
		PlayerSprites["guns_righthand"] = dmi.getSprites("mob/inhands/guns_righthand");
		PlayerSprites["items_lefthand"] = dmi.getSprites("mob/inhands/items_lefthand");
		PlayerSprites["items_righthand"] = dmi.getSprites("mob/inhands/items_righthand");
		PlayerSprites["clothing_lefthand"] = dmi.getSprites("mob/inhands/clothing_lefthand");
		PlayerSprites["clothing_righthand"] = dmi.getSprites("mob/inhands/clothing_righthand");
		//Vertical Doors Sprites
		DoorSprites["airLock"] = dmi.getSprites("obj/doors/airlocks/external/external");
		//end of Horizontal doors.
		//doors Ovelays
		DoorSprites["overlaysHorizontal"] = dmi.getSprites("obj/doors/airlocks/station/overlays");
		DoorSprites["overlaysVertical"] = dmi.getSprites("obj/doors/airlocks/external/overlays");
		//end of doors overlays

		MonitorSprites["monitors"] = dmi.getSprites("obj/monitors");

		BloodSprites["blood"] = dmi.getSprites("effects/blood");

		ConnectSprites["wall"] = Resources.LoadAll<Sprite>("walls/wall");
		ConnectSprites["wall_reinforced"] = Resources.LoadAll<Sprite>("walls/wall_reinforced");
		ConnectSprites["rusty_wall"] = Resources.LoadAll<Sprite>("walls/rusty_wall");
		ConnectSprites["rusty_wall_reinforced"] = Resources.LoadAll<Sprite>("walls/rusty_reinforced_wall");
		ConnectSprites["shuttle_wall"] = Resources.LoadAll<Sprite>("walls/shuttle_wall");
		ConnectSprites["sandstone_wall"] = Resources.LoadAll<Sprite>("walls/sandstone_wall");
		ConnectSprites["rock_wall"] = Resources.LoadAll<Sprite>("walls/rock_wall");
		ConnectSprites["window"] = Resources.LoadAll<Sprite>("windows/window");
		ConnectSprites["window_reinforced"] = Resources.LoadAll<Sprite>("windows/window_reinforced");
		ConnectSprites["table"] = Resources.LoadAll<Sprite>("tables/table");
		ConnectSprites["table_reinforced"] = Resources.LoadAll<Sprite>("tables/table_reinforced");
		ConnectSprites["table_wood"] = Resources.LoadAll<Sprite>("tables/table_wood");
		ConnectSprites["table_poker"] = Resources.LoadAll<Sprite>("tables/table_poker");
		ConnectSprites["lattice"] = Resources.LoadAll<Sprite>("floors/lattice");
		ConnectSprites["carpet"] = Resources.LoadAll<Sprite>("floors/carpet");
		ConnectSprites["catwalk"] = Resources.LoadAll<Sprite>("floors/catwalk");

		LightSprites["lights"] = Resources.LoadAll<Sprite>("lighting");
		FireSprites["fire"] = Resources.LoadAll<Sprite>("icons/effects/fire");

		ScreenUISprites["gen"] = Resources.LoadAll<Sprite>("screen_gen");
		InitWireSprites();
	}

	public void InitWireSprites()
	{
		string FileLocation = "obj/power_cond/power_cond_";
		string FileType = "";
		string[] Keys = { "red", "blue", "cyan", "green", "orange", "pink", "white", "yellow", "low", "high" };
		for (int i = 0; i < Keys.Length; i++)
		{
			string Key = Keys[i];
			Sprite[] sprites = Resources.LoadAll<Sprite>(FileLocation + Key + FileType);
			wireSprites[Key] = sprites;
		}
	}
}

public enum DoorType
{
	atmos,
	command,
	engineering,
	maintenance,
	medical,
	mining,
	overlays,
	civilian,
	research,
	science,
	security,
	virology,
	shuttle,
	airLock,
	glass,
	sliding
}

public enum BloodSplatSize
{
	small,
	medium,
	large
}

public enum BloodSplatType
{
	Generic,
	BloodLoss,
	Brute,

	Bullet
	//whatever
}

public enum WiringColor
{
	red,
	blue,
	cyan,
	green,
	orange,
	pink,
	white,
	yellow,
	low,
	high,
}

[Serializable]
public class AccessoryCollection
{
	public List<SpriteAccessory> spriteAccessories = new List<SpriteAccessory>();
}

[Serializable]
public class SpriteAccessory
{
	public string name;
	public string iconstate;
	public Gender gender = Gender.Neuter;
	public string spriteSheetName;
	public int spritePos;
}