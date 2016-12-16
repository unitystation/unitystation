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

            InitializeSpriteSheets();
        }

        private void InitializeSpriteSheets() {
            playerSprites["human"] = Resources.LoadAll<Sprite>("mobs/human");
            playerSprites["human_face"] = Resources.LoadAll<Sprite>("mobs/human_face");

            playerSprites["suit"] = Resources.LoadAll<Sprite>("mobs/clothes/suit");
            playerSprites["belt"] = Resources.LoadAll<Sprite>("mobs/clothes/belt");
            playerSprites["feet"] = Resources.LoadAll<Sprite>("mobs/clothes/feet");
            playerSprites["head"] = Resources.LoadAll<Sprite>("mobs/clothes/head");
            playerSprites["mask"] = Resources.LoadAll<Sprite>("mobs/clothes/mask");
            playerSprites["ears"] = Resources.LoadAll<Sprite>("mobs/clothes/ears");
            playerSprites["back"] = Resources.LoadAll<Sprite>("mobs/clothes/back");
            playerSprites["eyes"] = Resources.LoadAll<Sprite>("mobs/clothes/eyes");
            playerSprites["hands"] = Resources.LoadAll<Sprite>("mobs/clothes/hands");
            playerSprites["uniform"] = Resources.LoadAll<Sprite>("mobs/clothes/uniform");
            playerSprites["underwear"] = Resources.LoadAll<Sprite>("mobs/clothes/underwear");

            playerSprites["guns_lefthand"] = Resources.LoadAll<Sprite>("mobs/inhands/guns_lefthand");
            playerSprites["guns_righthand"] = Resources.LoadAll<Sprite>("mobs/inhands/guns_righthand");
            playerSprites["items_lefthand"] = Resources.LoadAll<Sprite>("mobs/inhands/items_lefthand");
            playerSprites["items_righthand"] = Resources.LoadAll<Sprite>("mobs/inhands/items_righthand");
            playerSprites["clothing_lefthand"] = Resources.LoadAll<Sprite>("mobs/inhands/clothing_lefthand");
            playerSprites["clothing_righthand"] = Resources.LoadAll<Sprite>("mobs/inhands/clothing_righthand");
        }
    }
}
