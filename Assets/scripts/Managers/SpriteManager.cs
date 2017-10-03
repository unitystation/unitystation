using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Sprites {
    public class Sprites {
        private Dictionary<string, Sprite[]> sprites = new Dictionary<string, Sprite[]>();

        public Sprite[] this[string key] {
            get {
				if (sprites.ContainsKey(key)) {
//					Debug.Log("Sprite found with key: " + key);
					return sprites[key];
				} else {
					Debug.Log("SPRITE MANAGER ERROR, KEY " + key + "NOT FOUND IN SPRITES");
					return null; 
				}
            }
            set {
                sprites[key] = value;
            }
        }
    }

    public class SpriteManager: MonoBehaviour {

	    private static SpriteManager spriteManager;
        private Sprites playerSprites = new Sprites();
        private Sprites wallSprites = new Sprites();
		private Sprites doorSprites = new Sprites();
		private Sprites monitorSprites = new Sprites();
		private Sprites bloodSprites = new Sprites();
		private Sprites lightSprites = new Sprites();
		private Sprites fireSprites = new Sprites();
        private Sprites wireSprites = new Sprites();
        public DmiIconData dmi;
        private void InitializeSpriteSheets() {
	        if (spriteManager.dmi == null)
	        {
		        spriteManager.dmi = Resources.Load("DmiIconData") as DmiIconData;
	        }

//	        if (dmi != null)
//	        {
		        PlayerSprites["human"] = dmi.getSprites("mob/human");
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

//		        ConnectSprites["wall"] = dmi.getSprites("turf/walls/wall");
//		        ConnectSprites["wall_reinforced"] = dmi.getSprites("turf/walls/reinforced_wall");
//		        ConnectSprites["rusty_wall"] = dmi.getSprites("turf/walls/rusty_wall");
//		        ConnectSprites["rusty_wall_reinforced"] = dmi.getSprites("turf/walls/rusty_reinforced_wall");
//		        ConnectSprites["shuttle_wall"] = dmi.getSprites("turf/walls/shuttle_wall");
//		        ConnectSprites["sandstone_wall"] = dmi.getSprites("turf/walls/sandstone_wall");
//		        ConnectSprites["rock_wall"] = dmi.getSprites("turf/walls/rock_wall");

//		        ConnectSprites["window"] = dmi.getSprites("obj/smooth_structures/window");
//		        ConnectSprites["window_reinforced"] = dmi.getSprites("obj/smooth_structures/reinforced_window");

//		        ConnectSprites["table"] = dmi.getSprites("obj/smooth_structures/table");
//		        ConnectSprites["table_reinforced"] = dmi.getSprites("obj/smooth_structures/reinforced_table");
//		        ConnectSprites["table_wood"] = dmi.getSprites("obj/smooth_structures/wood_table");
//		        ConnectSprites["table_poker"] = dmi.getSprites("obj/smooth_structures/poker_table");

//		        ConnectSprites["lattice"] = dmi.getSprites("obj/smooth_structures/lattice");
//		        ConnectSprites["carpet"] = dmi.getSprites("turf/floors/carpet");
//		        ConnectSprites["catwalk"] = dmi.getSprites("obj/smooth_structures/catwalk");
                    
                //door list of sprites. you need to load the proper spritesheet to make it animate properly. the root folder is /Assets/resources/icons/
                //Horizontal Doors Sprites
                /*
		        DoorSprites["atmos"] = dmi.getSprites("obj/doors/airlocks/station/atmos");
		        DoorSprites["command"] = dmi.getSprites("obj/doors/airlocks/station/command");
		        DoorSprites["engineering"] = dmi.getSprites("obj/doors/airlocks/station/engineering");
		        DoorSprites["maintenance"] = dmi.getSprites("obj/doors/airlocks/station/maintenance");
		        DoorSprites["medical"] = dmi.getSprites("obj/doors/airlocks/station/medical");
		        DoorSprites["mining"] = dmi.getSprites("obj/doors/airlocks/station/mining");		        
                DoorSprites["publicdoor"] = dmi.getSprites("obj/doors/airlocks/station/public");
		        DoorSprites["research"] = dmi.getSprites("obj/doors/airlocks/station/research");
		        DoorSprites["science"] = dmi.getSprites("obj/doors/airlocks/station/science");
		        DoorSprites["security"] = dmi.getSprites("obj/doors/airlocks/station/security");
		        DoorSprites["virology"] = dmi.getSprites("obj/doors/airlocks/station/virology");
                DoorSprites["shuttle"] = dmi.getSprites("obj/doors/airlocks/shuttle/shuttle");
                DoorSprites["glassDoor"] = dmi.getSprites("obj/doors/airlocks/station2/glass");
                */
                //end of Horizontal doors.
                
                //Vertical Doors Sprites
                DoorSprites["airLock"] = dmi.getSprites("obj/doors/airlocks/external/external");
                //end of Horizontal doors.
                //doors Ovelays
                DoorSprites["overlaysHorizontal"] = dmi.getSprites("obj/doors/airlocks/station/overlays");
                DoorSprites["overlaysVertical"] = dmi.getSprites("obj/doors/airlocks/external/overlays");
                //end of doors overlays

                MonitorSprites["monitors"] = dmi.getSprites("obj/monitors");

		        BloodSprites["blood"] = dmi.getSprites("effects/blood");

//		        LightSpr.ites["lights"] = dmi.getSprites("effects/lighting");
		        
		        
	// old ones:	        
//	        PlayerSprites["human"] = Resources.LoadAll<Sprite>("mobs/human");
//            PlayerSprites["human_face"] = Resources.LoadAll<Sprite>("mobs/human_face");
//
//            PlayerSprites["suit"] = Resources.LoadAll<Sprite>("mobs/clothes/suit");
//            PlayerSprites["belt"] = Resources.LoadAll<Sprite>("mobs/clothes/belt");
//            PlayerSprites["feet"] = Resources.LoadAll<Sprite>("mobs/clothes/feet");
//            PlayerSprites["head"] = Resources.LoadAll<Sprite>("mobs/clothes/head");
//            PlayerSprites["mask"] = Resources.LoadAll<Sprite>("mobs/clothes/mask");
//            PlayerSprites["ears"] = Resources.LoadAll<Sprite>("mobs/clothes/ears");
//            PlayerSprites["back"] = Resources.LoadAll<Sprite>("mobs/clothes/back");
//            PlayerSprites["eyes"] = Resources.LoadAll<Sprite>("mobs/clothes/eyes");
//            PlayerSprites["hands"] = Resources.LoadAll<Sprite>("mobs/clothes/hands");
//            PlayerSprites["uniform"] = Resources.LoadAll<Sprite>("mobs/clothes/uniform");
//            PlayerSprites["underwear"] = Resources.LoadAll<Sprite>("mobs/clothes/underwear");
//
//            PlayerSprites["guns_lefthand"] = Resources.LoadAll<Sprite>("mobs/inhands/guns_lefthand");
//            PlayerSprites["guns_righthand"] = Resources.LoadAll<Sprite>("mobs/inhands/guns_righthand");
//            PlayerSprites["items_lefthand"] = Resources.LoadAll<Sprite>("mobs/inhands/items_lefthand");
//            PlayerSprites["items_righthand"] = Resources.LoadAll<Sprite>("mobs/inhands/items_righthand");
//            PlayerSprites["clothing_lefthand"] = Resources.LoadAll<Sprite>("mobs/inhands/clothing_lefthand");
//            PlayerSprites["clothing_righthand"] = Resources.LoadAll<Sprite>("mobs/inhands/clothing_righthand");
//
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
//
//			DoorSprites["atmos"] = Resources.LoadAll<Sprite>("doors/station/atmos");
//			DoorSprites["command"] = Resources.LoadAll<Sprite>("doors/station/command");
//			DoorSprites["engineering"] = Resources.LoadAll<Sprite>("doors/station/engineering");
//			DoorSprites["maintenance"] = Resources.LoadAll<Sprite>("doors/station/maintenance");
//			DoorSprites["medical"] = Resources.LoadAll<Sprite>("doors/station/medical");
//			DoorSprites["mining"] = Resources.LoadAll<Sprite>("doors/station/mining");
//			DoorSprites["overlays"] = Resources.LoadAll<Sprite>("doors/station/overlays");
//			DoorSprites["publicdoor"] = Resources.LoadAll<Sprite>("doors/station/public");
//			DoorSprites["research"] = Resources.LoadAll<Sprite>("doors/station/research");
//			DoorSprites["science"] = Resources.LoadAll<Sprite>("doors/station/science");
//			DoorSprites["security"] = Resources.LoadAll<Sprite>("doors/station/security");
//			DoorSprites["virology"] = Resources.LoadAll<Sprite>("doors/station/virology");
//
//			MonitorSprites["monitors"] = Resources.LoadAll<Sprite>("obj/monitors");
//
//			BloodSprites["blood"] = Resources.LoadAll<Sprite>("blood");
//
			LightSprites["lights"] = Resources.LoadAll<Sprite>("lighting");
			FireSprites["fire"] = Resources.LoadAll<Sprite>("icons/effects/fire");
            
            string FileLocation = "obj/power_cond/power_cond_";
            string FileType = "";
            string[] Keys = { "red", "blue", "cyan", "green", "orange", "pink", "white", "yellow" };
            for (int i = 0; i < Keys.Length; i++)
            {
                string Key = Keys[i];
                Sprite[] sprites = Resources.LoadAll<Sprite>(FileLocation + Key + FileType);
                wireSprites[Key] = sprites;
            }
            //	        }
            //	        else
            //	        {
            //		        Debug.LogError("wtf man, dmi is still null!");
            //	        }
        }

        public static SpriteManager Instance {
            get {
                if(!spriteManager) {
                    spriteManager = FindObjectOfType<SpriteManager>();
                    spriteManager.InitializeSpriteSheets();
                }
                return spriteManager;
            }
        }

        public static Sprites PlayerSprites {
            get {
                return Instance.playerSprites;
            }
        }

        public static Sprites ConnectSprites {
            get {
                return Instance.wallSprites;
            }
        }

		public static Sprites DoorSprites {
			get { 
				return Instance.doorSprites;
			}
		}

		public static Sprites MonitorSprites {
			get { 
				return Instance.monitorSprites;
			}
		}

		public static Sprites BloodSprites{
			get{
				return Instance.bloodSprites;
			}
		}

		public static Sprites LightSprites{
			get{
				return Instance.lightSprites;
			}
		}

		public static Sprites FireSprites{
			get{
				return Instance.fireSprites;
			}
		}
        public static Sprites WireSprites
        {
            get
            {
                return Instance.wireSprites;
            }
        }
    }

	public enum DoorType{
		atmos,
		command,
		engineering,
		maintenance,
		medical,
		mining,
		overlays,
		publicdoor,
		research,
		science,
		security,
		virology,
        shuttle,
        airLock,
        glassDoor
    }
		
	public enum BloodSplatSize{
		small,
		medium,
		large
	}

	public enum BloodSplatType
	{
		Generic,
		BloodLoss,
		Brute,
		Bullet,
		//whatever
	}
}
