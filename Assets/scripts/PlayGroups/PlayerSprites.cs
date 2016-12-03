using UnityEngine;
using System.Collections;
using Sprites;
using UI;


namespace PlayGroup{
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

		public SpriteRenderer playerRend;
		public  SpriteRenderer suitRend;
		public  SpriteRenderer beltRend;
		public  SpriteRenderer feetRend;
		public  SpriteRenderer headRend;
		public  SpriteRenderer faceRend;
		public  SpriteRenderer maskRend;
		public  SpriteRenderer underwearRend;
		public  SpriteRenderer uniformRend;
		public  SpriteRenderer leftHandRend;
		public  SpriteRenderer rightHandRend;

	

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


        /// <summary>
        /// Holds direction all sprites are currently facing
        /// </summary>
        public Vector2 currentDirection;

		public Rigidbody2D thisRigi;

	


	// Use this for initialization
	void Start () {
			
	

			isRightHandFull = false;
			isLeftHandFull = false;
            currentDirection = Vector2.down;



	}
	
		//for applying the player prefs when it is eventually built
		public void SetSprites(CustomPlayerPrefs startPrefs){
		

			baseSprites = startPrefs;
			FaceDirection (Vector2.down);
		
		}

		void FixedUpdate(){

			if (thisRigi != null) {
				var localVel = transform.InverseTransformDirection (thisRigi.velocity);

//				Debug.Log ("LocalVel " + localVel);
				if (localVel.x > 1f) {
				
					FaceDirection (Vector2.right);
				}
				if (localVel.x < -1f) {

					FaceDirection (Vector2.left);
				}
				if (localVel.y < -1f) {

					FaceDirection (Vector2.down);
				}
				if (localVel.y > 1f) {

					FaceDirection (Vector2.up);
				}
			}


		}

		//turning character input and sprite update
		public void FaceDirection(Vector2 direction){

			if (baseSprites != null && SpriteManager.control.playerSprites != null) {
				if (direction == Vector2.down) {

					playerRend.sprite = SpriteManager.control.playerSprites.playerSheet [baseSprites.body]; // 36 

					suitRend.sprite = SpriteManager.control.playerSprites.suitSheet [baseSprites.suit]; //236 
					beltRend.sprite = SpriteManager.control.playerSprites.beltSheet [baseSprites.belt]; //62 
					headRend.sprite = SpriteManager.control.playerSprites.headSheet [baseSprites.head]; //221 
					feetRend.sprite = SpriteManager.control.playerSprites.feetSheet [baseSprites.shoes]; //36 
					underwearRend.sprite = SpriteManager.control.playerSprites.underwearSheet [baseSprites.underWear]; //52 
					uniformRend.sprite = SpriteManager.control.playerSprites.uniformSheet [baseSprites.uniform]; //16
					ChangeDirLeftItem (direction);
					ChangeDirRightItem (direction);
				}
				if (direction == Vector2.up) {

					playerRend.sprite = SpriteManager.control.playerSprites.playerSheet [baseSprites.body + 1]; 
					suitRend.sprite = SpriteManager.control.playerSprites.suitSheet [baseSprites.suit + 1]; 
					beltRend.sprite = SpriteManager.control.playerSprites.beltSheet [baseSprites.belt + 1]; 
					headRend.sprite = SpriteManager.control.playerSprites.headSheet [baseSprites.head + 1]; 
					feetRend.sprite = SpriteManager.control.playerSprites.feetSheet [baseSprites.shoes + 1]; 
					underwearRend.sprite = SpriteManager.control.playerSprites.underwearSheet [baseSprites.underWear + 1]; 
					uniformRend.sprite = SpriteManager.control.playerSprites.uniformSheet [baseSprites.uniform + 1];
					ChangeDirLeftItem (direction);
					ChangeDirRightItem (direction);
				}
				if (direction == Vector2.right) {

					playerRend.sprite = SpriteManager.control.playerSprites.playerSheet [baseSprites.body + 2]; 
					suitRend.sprite = SpriteManager.control.playerSprites.suitSheet [baseSprites.suit + 2]; 
					beltRend.sprite = SpriteManager.control.playerSprites.beltSheet [baseSprites.belt + 2]; 
					headRend.sprite = SpriteManager.control.playerSprites.headSheet [baseSprites.head + 2]; 
					feetRend.sprite = SpriteManager.control.playerSprites.feetSheet [baseSprites.shoes + 2]; 
					underwearRend.sprite = SpriteManager.control.playerSprites.underwearSheet [baseSprites.underWear + 2]; 
					uniformRend.sprite = SpriteManager.control.playerSprites.uniformSheet [baseSprites.uniform + 2];
					ChangeDirLeftItem (direction);
					ChangeDirRightItem (direction);
				}
				if (direction == Vector2.left) {

					playerRend.sprite = SpriteManager.control.playerSprites.playerSheet [baseSprites.body + 3]; 
					suitRend.sprite = SpriteManager.control.playerSprites.suitSheet [baseSprites.suit + 3]; 
					beltRend.sprite = SpriteManager.control.playerSprites.beltSheet [baseSprites.belt + 3]; 
					headRend.sprite = SpriteManager.control.playerSprites.headSheet [baseSprites.head + 3]; 
					feetRend.sprite = SpriteManager.control.playerSprites.feetSheet [baseSprites.shoes + 3]; 
					underwearRend.sprite = SpriteManager.control.playerSprites.underwearSheet [baseSprites.underWear + 3]; 
					uniformRend.sprite = SpriteManager.control.playerSprites.uniformSheet [baseSprites.uniform + 3];
					ChangeDirLeftItem (direction);
					ChangeDirRightItem (direction);
				}

			}

            currentDirection = direction;
	


		}

		//REAL SHIT METHOD FIX IT LATER OKAY - doobly
		public void PickedUpItem(int spriteNum){

			//FIXME No longer works over photon network. Need to sync the picked up item over photon and then only handle change of 
			// direction based on that character and what hand that item is in on that specific character also

			int itemSelector;
			if (spriteNum == 6) { //kitchen knifeitem

                Debug.Log("Picked up kitchen knife");
			
				itemSelector = 502; //kitchen handitem int for kitchen knife
				//yes this needs alot of refactoring until the suckiness has been disolved - doobly
			} else {

				itemSelector = 0;

			}

            isRightHandSelector = UIManager.control.isRightHand;

			if (isRightHandSelector) {
              
				baseSprites.rightH = itemSelector;
                isRightHandFull = true;
				rightHandRend.sprite = SpriteManager.control.playerSprites.rightHandSheet [baseSprites.rightH];

                ChangeDirRightItem(currentDirection); //sets sprite direction to direction player is currently facing

            } else {
			
				baseSprites.leftH = itemSelector;
                isLeftHandFull = true;
				leftHandRend.sprite = SpriteManager.control.playerSprites.leftHandSheet [baseSprites.leftH];

                ChangeDirLeftItem(currentDirection); //sets sprite direction to direction player is currently facing

			
			}
		}

		void ChangeDirLeftItem(Vector2 direction){
            
			if (UIManager.control.hands.leftSlot.isFull) {
                
			
				if (direction == Vector2.down) {
				//down sprite
				
					leftHandRend.sprite = SpriteManager.control.playerSprites.leftHandSheet [baseSprites.leftH - 3];

				}
				if (direction == Vector2.up) {
				
					leftHandRend.sprite = SpriteManager.control.playerSprites.leftHandSheet [baseSprites.leftH - 2];
				}

				if (direction == Vector2.right) {

					leftHandRend.sprite = SpriteManager.control.playerSprites.leftHandSheet [baseSprites.leftH - 1];

				}

				if (direction == Vector2.left){

					leftHandRend.sprite = SpriteManager.control.playerSprites.leftHandSheet [baseSprites.leftH];
				}
			
			}
				}

		void ChangeDirRightItem(Vector2 direction){
         
			if (UIManager.control.hands.rightSlot.isFull) {

				if (direction == Vector2.down) {
					//down sprite

					rightHandRend.sprite = SpriteManager.control.playerSprites.rightHandSheet [baseSprites.rightH];

				}
				if (direction == Vector2.up) {

					rightHandRend.sprite = SpriteManager.control.playerSprites.rightHandSheet [baseSprites.rightH + 1];
				}

				if (direction == Vector2.right) {

					rightHandRend.sprite = SpriteManager.control.playerSprites.rightHandSheet [baseSprites.rightH + 2];

				}

				if (direction == Vector2.left){

					rightHandRend.sprite = SpriteManager.control.playerSprites.rightHandSheet [baseSprites.rightH + 3];
				}

			}
		}




	}
}


