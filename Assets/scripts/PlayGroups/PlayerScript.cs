using UnityEngine;
using System.Collections;
using Game;
using UI;

namespace PlayGroup{
public class PlayerScript : MonoBehaviour {

			
	public float moveSpeed = 0.1f;

	public bool isMine = false; //Is this controlled by the player or other players

	[HideInInspector]
	public PhysicsMove physicsMove;

	public PlayerSprites playerSprites;

		public PhotonView photonView;
	

		[Header("Temp Player Preferences - start sprites (facingDown)")]
	public int bodyNumber;
	public int suitNumber;
	public int beltNumber;
	public int headNumber;
	public int shoesNumber;
	public int underWearNumber;
	public int uniformNumber;



	void Start () {

			GameObject searchPlayerList = GameObject.FindGameObjectWithTag ("PlayerList");
			if (searchPlayerList != null) {
			
				transform.parent = searchPlayerList.transform;
			} else {
			
				Debug.LogError ("Scene is missing PlayerList GameObject!!");
			
			}
			//add physics move component and set default movespeed
		physicsMove = gameObject.AddComponent<PhysicsMove> ();
		physicsMove.moveSpeed = moveSpeed;

			//Add player sprite controller component

			SetPlayerPrefs ();

			if (photonView.isMine) { //This prefab is yours, take control of it
			
				PlayerManager.control.SetPlayerForControl (this.gameObject); 
			
			}

	
		
		
	}


	void Update () {

		

				
	}

		//THIS IS ONLY USED FOR LOCAL PLAYER
		public void MovePlayer(Vector2 direction){
			if (!UIManager.control.chatControl.chatInputWindow.activeSelf && isMine) { //At the moment it just checks if the input window is open and if it is false then allow move
			
			physicsMove.MoveInDirection (direction); //Tile based physics move
//			playerSprites.FaceDirection (direction); //Handles the playersprite change on direction change
		
		}
		}



		//Temp
		void SetPlayerPrefs(){

			CustomPlayerPrefs newPrefs = new CustomPlayerPrefs ();
			newPrefs.body = bodyNumber;
			newPrefs.suit = suitNumber;
			newPrefs.belt = beltNumber;
			newPrefs.head = headNumber;
			newPrefs.shoes = shoesNumber;
			newPrefs.underWear = underWearNumber;
			newPrefs.uniform = uniformNumber;

			playerSprites.SetSprites (newPrefs);
		}

	}


	
}
