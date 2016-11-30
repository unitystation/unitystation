using UnityEngine;
using System.Collections;

namespace Sprites{
public class SpriteManager : MonoBehaviour {

	public static SpriteManager control; //All of the instantiate players will just reference sprites here
		                                 

	// Use this for initialization
	public PlayerSpriteSheet playerSprites;

	void Awake(){

		if (control == null) {
		
			control = this;
		
		} else {
		
			Destroy (this);
		
		}

	}
	void Start () {
		playerSprites = new PlayerSpriteSheet ();
		playerSprites.playerSheet = Resources.LoadAll<Sprite>("mobs/human");
		playerSprites.suitSheet = Resources.LoadAll<Sprite>("mobs/suit");
		playerSprites.beltSheet = Resources.LoadAll<Sprite>("mobs/belt");
		playerSprites.feetSheet = Resources.LoadAll<Sprite>("mobs/feet");
		playerSprites.headSheet = Resources.LoadAll<Sprite>("mobs/head");
		playerSprites.faceSheet = Resources.LoadAll<Sprite>("mobs/human_face");
		playerSprites.maskSheet = Resources.LoadAll<Sprite>("mobs/mask");
		playerSprites.underwearSheet = Resources.LoadAll<Sprite>("mobs/underwear");
		playerSprites.uniformSheet = Resources.LoadAll<Sprite>("mobs/uniform");
		playerSprites.leftHandSheet = Resources.LoadAll<Sprite> ("mobs/inhands/items_lefthand");
		playerSprites.rightHandSheet = Resources.LoadAll<Sprite> ("mobs/inhands/items_righthand");
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

public class PlayerSpriteSheet{


	public Sprite[] playerSheet;
	public Sprite[] suitSheet;
	public Sprite[] beltSheet;
	public Sprite[] feetSheet;
	public Sprite[] headSheet;
	public Sprite[] faceSheet;
	public Sprite[] maskSheet;
	public Sprite[] underwearSheet;
	public Sprite[] uniformSheet;
	public Sprite[] leftHandSheet;
	public Sprite[] rightHandSheet;


}
}
