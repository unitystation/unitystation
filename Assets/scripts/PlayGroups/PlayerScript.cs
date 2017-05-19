using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Equipment;
using UI;
using PlayGroup;
using InputControl;

namespace PlayGroup {
    public class PlayerScript: NetworkBehaviour {
        // the maximum distance the player needs to be to an object to interact with it
        public const float interactionDistance = 2f;
        public PlayerNetworkActions playerNetworkActions { get; set; }
		public WeaponNetworkActions weaponNetworkActions { get; set; }
		public SoundNetworkActions soundNetworkActions { get; set; }
		public PlayerMove playerMove { get; set;}
		public PlayerSprites playerSprites { get; set;}
		public InputController inputController { get; set; }
		public HitIcon hitIcon { get; set; }

		public GameObject ghost;

        [SyncVar(hook = "OnNameChange")]
        public string playerName = " ";
        private bool pickUpCoolDown = false;


        public override void OnStartClient() {
            //Local player is set a frame or two after OnStartClient
            //Wait to check if this is local player
            StartCoroutine(CheckIfNetworkPlayer());
            base.OnStartClient();
        }

        //isLocalPlayer is always called after OnStartClient
        public override void OnStartLocalPlayer() {
            Init();

            base.OnStartLocalPlayer();
        }

        //You know the drill
        public override void OnStartServer() {
            Init();
            base.OnStartServer();
        }

        void Start() {
            playerNetworkActions = GetComponent<PlayerNetworkActions>();
			weaponNetworkActions = GetComponent<WeaponNetworkActions>();
			soundNetworkActions = GetComponent<SoundNetworkActions>();
			inputController = GetComponent<InputController>();
			hitIcon = GetComponentInChildren<HitIcon>();
        }

        void Init() {
            if(isLocalPlayer) {
				playerMove = GetComponent<PlayerMove>();
				playerSprites = GetComponent<PlayerSprites>();
                GetComponent<InputControl.InputController>().enabled = true;
                if(!UIManager.Instance.playerListUIControl.window.activeInHierarchy) {
                    UIManager.Instance.playerListUIControl.window.SetActive(true);
                }
                PlayerManager.SetPlayerForControl(this.gameObject);
                CmdTrySetName(PlayerPrefs.GetString("PlayerName"));
            }
        }

        [Command]
        void CmdTrySetName(string name) {
			if(PlayerList.Instance != null)
            playerName = PlayerList.Instance.CheckName(name);
        }
        // On playerName variable change across all clients, make sure obj is named correctly
        // and set in Playerlist for that client
        public void OnNameChange(string newName) {
            gameObject.name = newName;
			if(!PlayerList.Instance.connectedPlayers.ContainsKey(newName)) {
                PlayerList.Instance.connectedPlayers.Add(newName, gameObject);
            }
            PlayerList.Instance.RefreshPlayerListText();
        }

        IEnumerator CheckIfNetworkPlayer() {
            yield return new WaitForSeconds(1f);
			if(!isLocalPlayer) {
                OnNameChange(playerName);
            }
        }

        public float DistanceTo(Vector3 position) {
            return (transform.position - position).magnitude;
        }

		public bool IsInReach(Transform transform, float interactDist = interactionDistance) {
            //if(pickUpCoolDown)
            //    return false;
            //StartCoroutine(PickUpCooldown());
			//TODO: reimplement this timer higher up like in the InputController
			return DistanceTo(transform.position) <= interactDist;
        }

        IEnumerator PickUpCooldown() {
            pickUpCoolDown = true;
            yield return new WaitForSeconds(0.1f);
            pickUpCoolDown = false;
        }

    }
}
