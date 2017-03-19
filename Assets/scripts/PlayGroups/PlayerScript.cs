using UnityEngine;
using System.Collections;
using UI;

namespace PlayGroup
{
	public class PlayerScript: Photon.PunBehaviour
	{
		// the maximum distance the player needs to be to an object to interact with it
		public float interactionDistance = 2f;
		//Is this controlled by the player or other players
		public bool IsMine { get; set; }
		//is running new Version of player script (will soon be the main version)
		public bool isVersion2 = false;
        
		[HideInInspector]
		public PlayerMove playerMove;
		[HideInInspector]
		public PlayerSprites playerSprites;


		void Awake()
		{
			playerSprites = gameObject.GetComponent<PlayerSprites>();
		}

		void Start()
		{
		    playerMove = gameObject.GetComponent<PlayerMove>();

			//Add player sprite controller component
			//TODO EQUIPMENT AND PLAYERLIST NEEDS WORK
			if (photonView.isMine) { //This prefab is yours, take control of it
				StartCoroutine("WaitForMapLoad");
			} else {
				BoxCollider2D boxColl = gameObject.GetComponent<BoxCollider2D>();
				boxColl.isTrigger = true;
			}
			if (PhotonNetwork.connectedAndReady) {
				gameObject.name = photonView.owner.NickName;
				if (!UIManager.Instance.playerListUIControl.window.activeInHierarchy) {
					UIManager.Instance.playerListUIControl.window.SetActive(true);
				}
				//Add it to the global playerlist
				PlayerList.Instance.AddPlayer(gameObject);
			}
		}

		//This fixes the bug of master client setting equipment before the UI is read (because it is the one that loads the map)
		IEnumerator WaitForMapLoad()
		{
			yield return new WaitForSeconds(1f);
			PlayerManager.SetPlayerForControl(this.gameObject);
		}

		//THIS IS ONLY USED FOR LOCAL PLAYER
		public void MovePlayer(Vector2 direction)
		{
			//At the moment it just checks if the input window is open and if it is false then allow move
			if (!UIManager.Chat.chatInputWindow.activeSelf && IsMine) {
				//playerSprites.FaceDirection(direction); //Handles the playersprite change on direction change
			}
		}

		public float DistanceTo(Vector3 position)
		{
			return (transform.position - position).magnitude;
		}

		public bool IsInReach(Transform transform)
		{
			return DistanceTo(transform.position) <= interactionDistance;
		}
	}
}
