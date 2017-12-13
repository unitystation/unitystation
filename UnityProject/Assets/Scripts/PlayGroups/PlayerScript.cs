using InputControl;
using System.Collections;
using PlayGroups.Input;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace PlayGroup
{
    public class PlayerScript : ManagedNetworkBehaviour
    {
        // the maximum distance the player needs to be to an object to interact with it
		//1.75 is the optimal distance to now have any direction click too far
		//NOTE FOR ANYONE EDITING THIS IN THE FUTURE: Character's head is slightly below the top of the tile
		//hence top reach is slightly lower than bottom reach, where the legs go exactly to the bottom of the tile.
        public const float interactionDistance = 1.75f;

        public PlayerNetworkActions playerNetworkActions { get; set; }

        public WeaponNetworkActions weaponNetworkActions { get; set; }

        public SoundNetworkActions soundNetworkActions { get; set; }

        public PlayerHealth playerHealth { get; set; }

        public PlayerMove playerMove { get; set; }

        public PlayerSprites playerSprites { get; set; }

        public PlayerSync playerSync { get; set; }

        public InputController inputController { get; set; }

        public HitIcon hitIcon { get; set; }

        [SyncVar]
        public JobType JobType = JobType.NULL;

        public GameObject ghost;

        private float pingUpdate = 0f;

        private ChatChannel selectedChannels;

        [SyncVar(hook = "OnNameChange")] public string playerName = " ";

        public ChatChannel SelectedChannels
        {
            get { return selectedChannels & GetAvailableChannels(); }
            set { this.selectedChannels = value; }
        }

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
            if (isLocalPlayer)
            {
                UIManager.ResetAllUI();
                UIManager.DisplayManager.SetCameraFollowPos();
                int rA = UnityEngine.Random.Range(0, 3);
                SoundManager.PlayVarAmbient(rA);
                playerMove = GetComponent<PlayerMove>();
                playerSprites = GetComponent<PlayerSprites>();
                GetComponent<InputController>().enabled = true;

                if (!UIManager.Instance.playerListUIControl.window.activeInHierarchy)
                {
                    UIManager.Instance.playerListUIControl.window.SetActive(true);
                }

                if (!PlayerManager.HasSpawned)
                {
                    //First
                    CmdTrySetName(PlayerManager.PlayerNameCache);
                }
                else
                {
                    //Manual after respawn
                    CmdSetNameManual(PlayerManager.PlayerNameCache);
                }

                PlayerManager.SetPlayerForControl(gameObject);

                if (PlayerManager.LocalPlayerScript.JobType == JobType.NULL)
                {
                    // I (client) have connected to the server, ask what my job preference is
                    UIManager.Instance.GetComponent<ControlDisplays>().jobSelectWindow.SetActive(true);
                }

                SelectedChannels = ChatChannel.Local;
            }
            else if (isServer)
            {
                playerMove = GetComponent<PlayerMove>();
            }
        }

        public bool canNotInteract()
        {
            return playerMove == null || !playerMove.allowInput || playerMove.isGhost;
        }

        public override void UpdateMe()
        {
            //Read out of ping in toolTip
            pingUpdate += Time.deltaTime;
            if (pingUpdate >= 5f)
            {
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
            if (string.IsNullOrEmpty(newName))
            {
                Debug.LogError("NO NAME PROVIDED!");
                return;
            }
            if (!PlayerList.Instance.connectedPlayers.ContainsKey(newName))
            {
                PlayerList.Instance.connectedPlayers.Add(newName, gameObject);
            }
            PlayerList.Instance.RefreshPlayerListText();
        }

        public float DistanceTo(Vector3 position)
        {
			//Because characters are taller than they are wider, their reach upwards/downards was greater
			//Flooring that shit fixes it
			Vector3Int pos = new Vector3Int(
				Mathf.FloorToInt(transform.position.x),
				Mathf.FloorToInt(transform.position.y),
				Mathf.FloorToInt(transform.position.z)
			);
            return (pos - position).magnitude;
        }

        /// <summary>
        /// Checks if the player is within reach of something
        /// </summary>
        /// <param name="position">The position of whatever we are trying to reach</param>
        /// <param name="interactDist">Maximum distance of interaction between the player and other objects</param>
        public bool IsInReach(Vector3 position, float interactDist = interactionDistance)
        {
			//If click is in diagonal direction, extend reach slightly
			float distanceX = Mathf.FloorToInt(Mathf.Abs(transform.position.x - position.x));
			float distanceY = Mathf.FloorToInt(Mathf.Abs(transform.position.y - position.y));
			if(distanceX == 1 && distanceY == 1) {
				return DistanceTo(position) <= interactDist + 0.4f;
			}

			//if cardinal direction, use regular reach
			return DistanceTo(position) <= interactDist;
        }

        public ChatChannel GetAvailableChannels(bool transmitOnly = true)
        {
			if(playerMove.isGhost)
			{
				if(transmitOnly)
				{
					return ChatChannel.Ghost | ChatChannel.OOC;
				}
				else
				{
					return ~ChatChannel.None;
				}
			}

            //TODO: Checks if player can speak (is not gagged, unconcious, has no mouth)
            ChatChannel transmitChannels = ChatChannel.OOC | ChatChannel.Local;

            GameObject headset = UIManager.InventorySlots.EarSlot.Item;
            if(headset) {
                EncryptionKeyType key = headset.GetComponent<Headset>().EncryptionKey;
                transmitChannels = transmitChannels | EncryptionKey.Permissions[key];
            }
            ChatChannel receiveChannels = (ChatChannel.Examine | ChatChannel.System);

            if (transmitOnly)
            {
                return transmitChannels;
            }
            else
            {
                return transmitChannels | receiveChannels;
            }
        }

        public ChatModifier GetCurrentChatModifiers()
        {
			if (playerMove.isGhost)
			{
				return ChatModifier.None;
			}

			//TODO add missing modifiers
			ChatModifier modifiers = ChatModifier.Drunk;

            if (JobType == JobType.CLOWN)
            {
                modifiers |= ChatModifier.Clown;
            }

            return modifiers;
        }

        //Tooltips inspector bar
        public void OnMouseEnter()
        {
            UI.UIManager.SetToolTip = this.name;
        }

        public void OnMouseExit()
        {
            UI.UIManager.SetToolTip = "";
        }
    }
}
