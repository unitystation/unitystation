using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Sprites {
    public class SpriteManager: MonoBehaviour {

        public static SpriteManager control; //All of the instantiate players will just reference sprites here
        
        public Dictionary<string, Sprite[]> playerSprites = new Dictionary<string, Sprite[]>();

        void Awake() {
            if(control == null) {
                control = this;
            } else {
                Destroy(this);
            }
        }

        void Start() {
            playerSprites["human"] = Resources.LoadAll<Sprite>("mobs/human");
            playerSprites["suit"] = Resources.LoadAll<Sprite>("mobs/clothes/suit");
            playerSprites["belt"] = Resources.LoadAll<Sprite>("mobs/clothes/belt");
            playerSprites["feet"] = Resources.LoadAll<Sprite>("mobs/clothes/feet");
            playerSprites["head"] = Resources.LoadAll<Sprite>("mobs/clothes/head");
            playerSprites["human_face"] = Resources.LoadAll<Sprite>("mobs/human_face");
            playerSprites["mask"] = Resources.LoadAll<Sprite>("mobs/clothes/mask");
            playerSprites["underwear"] = Resources.LoadAll<Sprite>("mobs/clothes/underwear");
            playerSprites["uniform"] = Resources.LoadAll<Sprite>("mobs/clothes/uniform");
            playerSprites["items_lefthand"] = Resources.LoadAll<Sprite>("mobs/inhands/items_lefthand");
            playerSprites["items_righthand"] = Resources.LoadAll<Sprite>("mobs/inhands/items_righthand");
        }
    }
}
