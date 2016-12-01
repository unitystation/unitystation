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
	[HideInInspector]
	public PlayerSprites playerSprites;

		[Header("Temp Player Preferences - start sprites (facingDown)")]
	public int bodyNumber;
	public int suitNumber;
	public int beltNumber;
	public int headNumber;
	public int shoesNumber;
	public int underWearNumber;
	public int uniformNumber;



	void Start () {

			//add physics move component and set default movespeed
		physicsMove = gameObject.AddComponent<PhysicsMove> ();
		physicsMove.moveSpeed = moveSpeed;

			//Add player sprite controller component
		playerSprites = gameObject.AddComponent<PlayerSprites> ();
			Invoke ("SetPlayerPrefs", 0.1f);

		//SPAWN POSITION
	
		
		
	}


	void Update () {

			//TODO photon input needs to be handled if the object isMine = false


				
	}

		//THIS IS ONLY USED FOR LOCAL PLAYER
		public void MovePlayer(Vector2 direction){
			if (!UIManager.control.chatControl.chatInputWindow.activeSelf && isMine) { //At the moment it just checks if the input window is open and if it is false then allow move
			
			physicsMove.MoveInDirection (direction); //Tile based physics move
			playerSprites.FaceDirection (direction); //Handles the playersprite change on direction change
		
		}
		}

		public void MoveNetworkPlayer(Vector2 direction){ //TODO hook up the photon recieve stream and process input for networked players

				physicsMove.MoveInDirection (direction); //Tile based physics move
				playerSprites.FaceDirection (direction); //Handles the playersprite change on direction change

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
