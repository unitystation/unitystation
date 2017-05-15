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
//					Debug.Log("SPRITE MANAGER ERROR, KEY " + key + "NOT FOUND IN SPRITES");
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

        private void InitializeSpriteSheets() {
            PlayerSprites["human"] = Resources.LoadAll<Sprite>("mobs/human");
            PlayerSprites["human_face"] = Resources.LoadAll<Sprite>("mobs/human_face");

            PlayerSprites["suit"] = Resources.LoadAll<Sprite>("mobs/clothes/suit");
            PlayerSprites["belt"] = Resources.LoadAll<Sprite>("mobs/clothes/belt");
            PlayerSprites["feet"] = Resources.LoadAll<Sprite>("mobs/clothes/feet");
            PlayerSprites["head"] = Resources.LoadAll<Sprite>("mobs/clothes/head");
            PlayerSprites["mask"] = Resources.LoadAll<Sprite>("mobs/clothes/mask");
            PlayerSprites["ears"] = Resources.LoadAll<Sprite>("mobs/clothes/ears");
            PlayerSprites["back"] = Resources.LoadAll<Sprite>("mobs/clothes/back");
            PlayerSprites["eyes"] = Resources.LoadAll<Sprite>("mobs/clothes/eyes");
            PlayerSprites["hands"] = Resources.LoadAll<Sprite>("mobs/clothes/hands");
            PlayerSprites["uniform"] = Resources.LoadAll<Sprite>("mobs/clothes/uniform");
            PlayerSprites["underwear"] = Resources.LoadAll<Sprite>("mobs/clothes/underwear");

            PlayerSprites["guns_lefthand"] = Resources.LoadAll<Sprite>("mobs/inhands/guns_lefthand");
            PlayerSprites["guns_righthand"] = Resources.LoadAll<Sprite>("mobs/inhands/guns_righthand");
            PlayerSprites["items_lefthand"] = Resources.LoadAll<Sprite>("mobs/inhands/items_lefthand");
            PlayerSprites["items_righthand"] = Resources.LoadAll<Sprite>("mobs/inhands/items_righthand");
            PlayerSprites["clothing_lefthand"] = Resources.LoadAll<Sprite>("mobs/inhands/clothing_lefthand");
            PlayerSprites["clothing_righthand"] = Resources.LoadAll<Sprite>("mobs/inhands/clothing_righthand");

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

			DoorSprites["atmos"] = Resources.LoadAll<Sprite>("doors/station/atmos");
			DoorSprites["command"] = Resources.LoadAll<Sprite>("doors/station/command");
			DoorSprites["engineering"] = Resources.LoadAll<Sprite>("doors/station/engineering");
			DoorSprites["maintenance"] = Resources.LoadAll<Sprite>("doors/station/maintenance");
			DoorSprites["medical"] = Resources.LoadAll<Sprite>("doors/station/medical");
			DoorSprites["mining"] = Resources.LoadAll<Sprite>("doors/station/mining");
			DoorSprites["overlays"] = Resources.LoadAll<Sprite>("doors/station/overlays");
			DoorSprites["publicdoor"] = Resources.LoadAll<Sprite>("doors/station/public");
			DoorSprites["research"] = Resources.LoadAll<Sprite>("doors/station/research");
			DoorSprites["science"] = Resources.LoadAll<Sprite>("doors/station/science");
			DoorSprites["security"] = Resources.LoadAll<Sprite>("doors/station/security");
			DoorSprites["virology"] = Resources.LoadAll<Sprite>("doors/station/virology");

			MonitorSprites["monitors"] = Resources.LoadAll<Sprite>("obj/monitors");

			BloodSprites["blood"] = Resources.LoadAll<Sprite>("blood");

			LightSprites["lights"] = Resources.LoadAll<Sprite>("lighting");

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
		virology
	}
		
	public enum BloodSplatSize{
		small,
		medium,
		large
	}
}
