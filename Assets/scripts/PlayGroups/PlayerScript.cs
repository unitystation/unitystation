using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UI;
using PlayGroup;
using InputControl;
using System;

namespace PlayGroup
{
	public class PlayerScript: ManagedNetworkBehaviour
	{
		// the maximum distance the player needs to be to an object to interact with it
		public const float interactionDistance = 2f;

		public PlayerNetworkActions playerNetworkActions { get; set; }

		public WeaponNetworkActions weaponNetworkActions { get; set; }

		public SoundNetworkActions soundNetworkActions { get; set; }

		public PlayerHealth playerHealth { get; set; }

		public PlayerMove playerMove { get; set; }

		public PlayerSprites playerSprites { get; set; }

		public PlayerSync playerSync { get; set; }

		public InputController inputController { get; set; }

		public HitIcon hitIcon { get; set; }

		public JobType JobType = JobType.NULL;

		public GameObject ghost;

		private float pingUpdate = 0f;

		[SyncVar(hook = "OnNameChange")]
		public string playerName = " ";

		public override void OnStartClient()
		{
			//Local player is set a frame or two after OnStartClient
			StartCoroutine(WaitForLoad());
			base.OnStartClient();
		}

		IEnumerator WaitForLoad()
		{
			yield return new WaitForSeconds(2f);
			OnNameChange(playerName);
		}

		//isLocalPlayer is always called after OnStartClient
		public override void OnStartLocalPlayer()
		{
			Init();

			base.OnStartLocalPlayer();
		}

		//You know the drill
		public override void OnStartServer()
		{
			Init();
			base.OnStartServer();
		}

		void Start()
		{
			playerNetworkActions = GetComponent<PlayerNetworkActions>();
			playerSync = GetComponent<PlayerSync>();
			playerHealth = GetComponent<PlayerHealth>();
			weaponNetworkActions = GetComponent<WeaponNetworkActions>();
			soundNetworkActions = GetComponent<SoundNetworkActions>();
			inputController = GetComponent<InputController>();
			hitIcon = GetComponentInChildren<HitIcon>();
		}
			
		void Init()
		{
			if (isLocalPlayer) {
				UIManager.ResetAllUI();
				UIManager.DisplayManager.SetCameraFollowPos();
				int rA = UnityEngine.Random.Range(0, 3);
				SoundManager.PlayVarAmbient(rA);
				playerMove = GetComponent<PlayerMove>();
				playerSprites = GetComponent<PlayerSprites>();
				GetComponent<InputController>().enabled = true;
				if (!UIManager.Instance.playerListUIControl.window.activeInHierarchy) {
					UIManager.Instance.playerListUIControl.window.SetActive(true);
				}
					
				if (!PlayerManager.HasSpawned) {
					//First
					CmdTrySetName(PlayerManager.PlayerNameCache);
				} else {
					//Manual after respawn
					CmdSetNameManual(PlayerManager.PlayerNameCache);
				}

                PlayerManager.SetPlayerForControl(this.gameObject);
               
                // I (client) have connected to the server, ask what my job preference is
                UIManager.Instance.GetComponent<ControlDisplays>().jobSelectWindow.SetActive(true);

            } else if ( isServer )
	        {
		        playerMove = GetComponent<PlayerMove>();
	        }
        }

	    public bool canNotInteract()
	    {
		    return playerMove == null || !playerMove.allowInput || playerMove.isGhost;
	    }
	    
        public override void UpdateMe(){
			//Read out of ping in toolTip
			pingUpdate += Time.deltaTime;
			if (pingUpdate >= 5f) {
				pingUpdate = 0f;
				int ping = CustomNetworkManager.Instance.client.GetRTT();
				UIManager.SetToolTip = "ping: " + ping.ToString();
			}
		}

		[Command]
		void CmdTrySetName(string name)
		{
			if (PlayerList.Instance != null)
				playerName = PlayerList.Instance.CheckName(name);
		}

		[Command]
		void CmdSetNameManual(string name)
		{
			playerName = name;
		}

		// On playerName variable change across all clients, make sure obj is named correctly
		// and set in Playerlist for that client
		public void OnNameChange(string newName)
		{
			playerName = newName;
			gameObject.name = newName;
			if (string.IsNullOrEmpty(newName)) {
				Debug.LogError("NO NAME PROVIDED!");
				return;
			}
			if (!PlayerList.Instance.connectedPlayers.ContainsKey(newName)) {
				PlayerList.Instance.connectedPlayers.Add(newName, gameObject);
			}
			PlayerList.Instance.RefreshPlayerListText();
		}

		public float DistanceTo(Vector3 position)
		{
			return (transform.position - position).magnitude;
		}

		public bool IsInReach(Transform transform, float interactDist = interactionDistance)
		{
			//if(pickUpCoolDown)
			//    return false;
			//StartCoroutine(PickUpCooldown());
			//TODO: reimplement this timer higher up like in the InputController
			return DistanceTo(transform.position) <= interactDist;
		}
	}
}
