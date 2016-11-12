using UnityEngine;
using System.Collections.Generic;
using MovementEffects;

namespace SS.PlayGroup{
	[RequireComponent (typeof (SpriteRenderer))]

	public class CustomPlayerPrefs{

		public int body{ get; set; }
		public int suit{ get; set; }
		public int belt{ get; set; }
		public int hat{ get; set; }
		public int shoes{ get; set; }
		public int underWear{ get; set; }
		public int uniform{ get; set; }

	}

public class PlayerSprites : MonoBehaviour {

		private SpriteRenderer playerRend;
		private SpriteRenderer suitRend;
		private SpriteRenderer beltRend;
		private SpriteRenderer feetRend;
		private SpriteRenderer headRend;
		private SpriteRenderer faceRend;
		private SpriteRenderer maskRend;
		private SpriteRenderer underwearRend;
		private SpriteRenderer uniformRend;

		private Sprite[] playerSheet;
		private Sprite[] suitSheet;
		private Sprite[] beltSheet;
		private Sprite[] feetSheet;
		private Sprite[] headSheet;
		private Sprite[] faceSheet;
		private Sprite[] maskSheet;
		private Sprite[] underwearSheet;
		private Sprite[] uniformSheet;

		//All sprites should be facing down by default
		public CustomPlayerPrefs baseSprites;


	// Use this for initialization
	void Start () {
			
			playerRend = GetComponent<SpriteRenderer>();

			Timing.RunCoroutine (LoadSpriteSheets ()); //load sprite sheet resources

	}
	
		public void SetSprites(CustomPlayerPrefs startPrefs){
		
			baseSprites = startPrefs;
		
		}

		//turning character input and sprite update
		public void FaceDirection(Vector2 direction){

			if (direction == Vector2.down) {
				
				playerRend.sprite = playerSheet [baseSprites.body]; // 36 
				suitRend.sprite = suitSheet [baseSprites.suit]; //236 
				beltRend.sprite = beltSheet [baseSprites.belt]; //62 
				headRend.sprite = headSheet [baseSprites.hat]; //221 
				feetRend.sprite = feetSheet [baseSprites.shoes]; //36 
				underwearRend.sprite = underwearSheet [baseSprites.underWear]; //52 
				uniformRend.sprite = uniformSheet [baseSprites.uniform]; //16 
			}
			if (direction == Vector2.up) {

				playerRend.sprite = playerSheet [baseSprites.body + 1]; 
				suitRend.sprite = suitSheet [baseSprites.suit + 1]; 
				beltRend.sprite = beltSheet [baseSprites.belt + 1]; 
				headRend.sprite = headSheet [baseSprites.hat + 1]; 
				feetRend.sprite = feetSheet [baseSprites.shoes + 1]; 
				underwearRend.sprite = underwearSheet [baseSprites.underWear + 1]; 
				uniformRend.sprite = uniformSheet [baseSprites.uniform + 1]; 
			}
			if (direction == Vector2.right) {

				playerRend.sprite = playerSheet [baseSprites.body + 2]; 
				suitRend.sprite = suitSheet [baseSprites.suit + 2]; 
				beltRend.sprite = beltSheet [baseSprites.belt + 2]; 
				headRend.sprite = headSheet [baseSprites.hat + 2]; 
				feetRend.sprite = feetSheet [baseSprites.shoes + 2]; 
				underwearRend.sprite = underwearSheet [baseSprites.underWear + 2]; 
				uniformRend.sprite = uniformSheet [baseSprites.uniform + 2]; 
			}
			if (direction == Vector2.left) {

				playerRend.sprite = playerSheet [baseSprites.body + 3]; 
				suitRend.sprite = suitSheet [baseSprites.suit + 3]; 
				beltRend.sprite = beltSheet [baseSprites.belt + 3]; 
				headRend.sprite = headSheet [baseSprites.hat + 3]; 
				feetRend.sprite = feetSheet [baseSprites.shoes + 3]; 
				underwearRend.sprite = underwearSheet [baseSprites.underWear + 3]; 
				uniformRend.sprite = uniformSheet [baseSprites.uniform + 3]; 
			}


	


		}

		//COROUTINES

		IEnumerator<float> LoadSpriteSheets(){

			foreach(SpriteRenderer child in this.GetComponentsInChildren<SpriteRenderer>())
			{

				switch (child.name)
				{
				case "suit":
					suitRend = child;
					break;
				case "belt":
					beltRend = child;
					break;
				case "feet":
					feetRend = child;
					break;
				case "head":
					headRend = child;
					break;
				case "face":
					faceRend = child;
					break;
				case "mask":
					maskRend = child;
					break;
				case "underwear":
					underwearRend = child;
					break;
				case "uniform":
					uniformRend = child;
					break;
				}

			}
			playerSheet = Resources.LoadAll<Sprite>("mobs/human");
			suitSheet = Resources.LoadAll<Sprite>("mobs/suit");
			beltSheet = Resources.LoadAll<Sprite>("mobs/belt");
			feetSheet = Resources.LoadAll<Sprite>("mobs/feet");
			headSheet = Resources.LoadAll<Sprite>("mobs/head");
			faceSheet = Resources.LoadAll<Sprite>("mobs/human_face");
			maskSheet = Resources.LoadAll<Sprite>("mobs/mask");
			underwearSheet = Resources.LoadAll<Sprite>("mobs/underwear");
			uniformSheet = Resources.LoadAll<Sprite>("mobs/uniform");



			yield return 0f;
		}
	}
}


