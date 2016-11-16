using UnityEngine;
using System.Collections;


namespace SS.PlayGroup{
	[RequireComponent (typeof (SpriteRenderer))]

	public class CustomPlayerPrefs{

		public int body{ get; set; }
		public int suit{ get; set; }
		public int belt{ get; set; }
		public int head{ get; set; }
		public int shoes{ get; set; }
		public int underWear{ get; set; }
		public int uniform{ get; set; }
		public int leftH{ get; set; }
		public int rightH{ get; set; }

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
		private SpriteRenderer leftHandRend;
		private SpriteRenderer rightHandRend;

		private Sprite[] playerSheet;
		private Sprite[] suitSheet;
		private Sprite[] beltSheet;
		private Sprite[] feetSheet;
		private Sprite[] headSheet;
		private Sprite[] faceSheet;
		private Sprite[] maskSheet;
		private Sprite[] underwearSheet;
		private Sprite[] uniformSheet;
		private Sprite[] leftHandSheet;
		private Sprite[] rightHandSheet;

		//All sprites should be facing down by default
		public CustomPlayerPrefs baseSprites;

		/// <summary>
		/// Is Something in the Left Hand
		/// </summary>
		public bool isLeftHandFull{ get; set; }

		/// <summary>
		/// Is Something in the Right Hand
		/// </summary>
		public bool isRightHandFull{ get; set; }

		/// <summary>
		/// What is the control ui hand selector set at
		/// Set this from control UI so it doesn't matter for 
		/// networked player objects (for photon and shiz)
		/// </summary>
		public bool isRightHandSelector = true;




	// Use this for initialization
	void Start () {
			
			playerRend = GetComponent<SpriteRenderer>();

			StartCoroutine (LoadSpriteSheets ()); //load sprite sheet resources

			isRightHandFull = false;
			isLeftHandFull = false;



	}
	
		//for applying the player prefs when it is eventually built
		public void SetSprites(CustomPlayerPrefs startPrefs){
		
			baseSprites = startPrefs;
		
		
		}

		//turning character input and sprite update
		public void FaceDirection(Vector2 direction){

			if (direction == Vector2.down) {
				
				playerRend.sprite = playerSheet [baseSprites.body]; // 36 
				suitRend.sprite = suitSheet [baseSprites.suit]; //236 
				beltRend.sprite = beltSheet [baseSprites.belt]; //62 
				headRend.sprite = headSheet [baseSprites.head]; //221 
				feetRend.sprite = feetSheet [baseSprites.shoes]; //36 
				underwearRend.sprite = underwearSheet [baseSprites.underWear]; //52 
				uniformRend.sprite = uniformSheet [baseSprites.uniform]; //16
				ChangeDirLeftItem(direction);
				ChangeDirRightItem(direction);
			}
			if (direction == Vector2.up) {

				playerRend.sprite = playerSheet [baseSprites.body + 1]; 
				suitRend.sprite = suitSheet [baseSprites.suit + 1]; 
				beltRend.sprite = beltSheet [baseSprites.belt + 1]; 
				headRend.sprite = headSheet [baseSprites.head + 1]; 
				feetRend.sprite = feetSheet [baseSprites.shoes + 1]; 
				underwearRend.sprite = underwearSheet [baseSprites.underWear + 1]; 
				uniformRend.sprite = uniformSheet [baseSprites.uniform + 1];
				ChangeDirLeftItem(direction);
				ChangeDirRightItem(direction);
			}
			if (direction == Vector2.right) {

				playerRend.sprite = playerSheet [baseSprites.body + 2]; 
				suitRend.sprite = suitSheet [baseSprites.suit + 2]; 
				beltRend.sprite = beltSheet [baseSprites.belt + 2]; 
				headRend.sprite = headSheet [baseSprites.head + 2]; 
				feetRend.sprite = feetSheet [baseSprites.shoes + 2]; 
				underwearRend.sprite = underwearSheet [baseSprites.underWear + 2]; 
				uniformRend.sprite = uniformSheet [baseSprites.uniform + 2];
				ChangeDirLeftItem(direction);
				ChangeDirRightItem(direction);
			}
			if (direction == Vector2.left) {

				playerRend.sprite = playerSheet [baseSprites.body + 3]; 
				suitRend.sprite = suitSheet [baseSprites.suit + 3]; 
				beltRend.sprite = beltSheet [baseSprites.belt + 3]; 
				headRend.sprite = headSheet [baseSprites.head + 3]; 
				feetRend.sprite = feetSheet [baseSprites.shoes + 3]; 
				underwearRend.sprite = underwearSheet [baseSprites.underWear + 3]; 
				uniformRend.sprite = uniformSheet [baseSprites.uniform + 3];
				ChangeDirLeftItem(direction);
				ChangeDirRightItem(direction);
			}


	


		}

		//REAL SHIT METHOD FIX IT LATER OKAY - doobly
		public void PickedUpItem(int spriteNum){

			int itemSelector;
			if (spriteNum == 6) { //kitchen knifeitem
			
				itemSelector = 502; //kitchen handitem int for kitchen knife
				//yes this needs alot of refactoring until the suckiness has been disolved - doobly
			} else {

				itemSelector = 0;

			}

			if (isRightHandSelector) {
			
				baseSprites.rightH = itemSelector;
				rightHandRend.sprite = rightHandSheet [baseSprites.rightH];
			
			} else {
			
				baseSprites.leftH = itemSelector;
				leftHandRend.sprite = leftHandSheet [baseSprites.leftH];

			
			}
		}

				void ChangeDirLeftItem(Vector2 direction){
			
			if (leftHandRend != null && isLeftHandFull) {
			
				if (direction == Vector2.down) {
				//down sprite
				
					leftHandRend.sprite = leftHandSheet [baseSprites.leftH - 3];

				}
				if (direction == Vector2.up) {
				
					leftHandRend.sprite = leftHandSheet [baseSprites.leftH - 2];
				}

				if (direction == Vector2.right) {

					leftHandRend.sprite = leftHandSheet [baseSprites.leftH - 1];

				}

				if (direction == Vector2.left){

					leftHandRend.sprite = leftHandSheet [baseSprites.leftH];
				}
			
			}
				}

		void ChangeDirRightItem(Vector2 direction){

			if (rightHandRend != null && isRightHandFull) {

				if (direction == Vector2.down) {
					//down sprite

					rightHandRend.sprite = rightHandSheet [baseSprites.rightH];

				}
				if (direction == Vector2.up) {

					rightHandRend.sprite = rightHandSheet [baseSprites.rightH + 1];
				}

				if (direction == Vector2.right) {

					rightHandRend.sprite = rightHandSheet [baseSprites.rightH + 2];

				}

				if (direction == Vector2.left){

					rightHandRend.sprite = rightHandSheet [baseSprites.rightH + 3];
				}

			}
		}

		//COROUTINES

		IEnumerator LoadSpriteSheets(){

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
				case "leftHand":
					leftHandRend = child;
					leftHandRend.sprite = null;
					break; 			
				case "rightHand":
				rightHandRend = child;
				rightHandRend.sprite = null;
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
			leftHandSheet = Resources.LoadAll<Sprite> ("mobs/inhands/items_lefthand");
			rightHandSheet = Resources.LoadAll<Sprite> ("mobs/inhands/items_righthand");



			yield return null;
		
		}
	}
}


