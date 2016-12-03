using UnityEngine;
using UnityEngine.EventSystems;
using UI;
using Network;

//Handles control and spawn of player prefab


namespace PlayGroup
{

	public class PlayerManager : Photon.PunBehaviour, IPunObservable
	{
		public static PlayerManager control;

		[Header("The current Health of our player")]
		public float Health = 100f;

		[Header("The PlayerPrefabs. Instantiate for each player")]
		public Transform playerPrefab;

		[Header("SpawnPoint: TODO: SpawnPoints Array")]
		public Transform spawnPoint;


		public GameObject LocalPlayerObj;

		[HideInInspector]
		public static PlayerScript LocalPlayerScript;

		[HideInInspector]
		public PlayerScript playerScript; //For access via other parts of the game

	
		public bool hasSpawned = false;



		//True, when the user is firing
		bool IsFiring;

	

		public void Awake()
		{

			if (control == null) {
			
				control = this;
			
			} else {
			
				Destroy (this);
			
			}


			if (hasSpawned) {

				Camera2DFollow.followControl.target = LocalPlayerObj.transform;
			
			}

		}

		public void SetPlayerForControl(GameObject playerObjToControl){
		
			LocalPlayerObj = playerObjToControl;
			LocalPlayerScript = playerObjToControl.GetComponent<PlayerScript> ();
			LocalPlayerScript.isMine = true; // Set this object to yours, the rest are for network players

			PlayerManager.control.playerScript = LocalPlayerScript; // Set this on the manager so it can be accessed by other components/managers
			Camera2DFollow.followControl.target = LocalPlayerObj.transform;
		
		
		}

	
		//CHECK HERE FOR AN EXAMPLE OF INSTANTIATING ITEMS ON PHOTON
		public void CheckIfSpawned(){ 

			Debug.Log ("CHECK IF SPAWNED");
			if (!hasSpawned){

				if (GameData.control.isInGame && NetworkManager.control.isConnected) {

		

				
				
					PhotonNetwork.Instantiate (this.playerPrefab.name, spawnPoint.position, Quaternion.identity, 0); //TODO: More spawn points and a way to iterate through them
						hasSpawned = true;
			
				

				return;
			}

			}
//						if (hasSpawned && GameData.control.isInGame && PlayerManager.control.playerScript == null && NetworkManager.control.isConnected) { //if we lost the player reference somehow (unforeseen problems) then just give the ref back
//						
//							PlayerManager.control.playerScript = LocalPlayerScript; 
//							Camera2DFollow.followControl.target = LocalPlayerObj.transform;
//						
//						}


		}

		public void Update()
		{
			// only process controls if local player exists
			if(hasSpawned && LocalPlayerScript != null)
			{
				this.ProcessInputs();

				if (this.Health <= 0f)
				{
					//TODO DEATH
				}
			}

	
		}


		/// <summary>
		/// Processes the inputs. This MUST ONLY BE USED when the player has authority over this Networked GameObject (photonView.isMine == true)
		/// </summary>
		void ProcessInputs()
		{
			//INPUT CONTROLS HERE
			if (Input.GetKeyUp (KeyCode.W) || Input.GetKeyUp (KeyCode.A) || Input.GetKeyUp (KeyCode.S) || Input.GetKeyUp (KeyCode.D)) {
				LocalPlayerScript.physicsMove.MoveInputReleased ();
			}

			//hold key down inputs. clampPos is used to snap player to an axis on movement
			if (Input.GetKey (KeyCode.D) && !LocalPlayerScript.physicsMove.isMoving || Input.GetKey (KeyCode.D) && LocalPlayerScript.physicsMove.isMoving && LocalPlayerScript.physicsMove._moveDirection == Vector2.left) {
				//RIGHT
				LocalPlayerScript.MovePlayer (Vector2.right);

			} 
			if (Input.GetKey (KeyCode.A) && !LocalPlayerScript.physicsMove.isMoving || Input.GetKey (KeyCode.A) && LocalPlayerScript.physicsMove.isMoving && LocalPlayerScript.physicsMove._moveDirection == Vector2.right) {
				//LEFT
				LocalPlayerScript.MovePlayer (Vector2.left);

			}
			if (Input.GetKey (KeyCode.S) && !LocalPlayerScript.physicsMove.isMoving || Input.GetKey (KeyCode.S) && LocalPlayerScript.physicsMove.isMoving && LocalPlayerScript.physicsMove._moveDirection == Vector2.up) {
				//DOWN
				LocalPlayerScript.MovePlayer (Vector2.down);

			} 
			if (Input.GetKey (KeyCode.W) && !LocalPlayerScript.physicsMove.isMoving || Input.GetKey (KeyCode.W) && LocalPlayerScript.physicsMove.isMoving && LocalPlayerScript.physicsMove._moveDirection == Vector2.down) {
				LocalPlayerScript.MovePlayer (Vector2.up);

			} 


		}

	



		// IPunObservable implementation

		public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
		{
			if (stream.isWriting)
			{
				// We own this player: send the others our data
				stream.SendNext(this.IsFiring);
				stream.SendNext(this.Health);
			}
			else
			{
				// Network player, receive data
				this.IsFiring = (bool)stream.ReceiveNext();
				this.Health = (float)stream.ReceiveNext();
			}
		}




	}
}
