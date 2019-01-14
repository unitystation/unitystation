using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Facepunch.Steamworks;


public class PlayerScript : ManagedNetworkBehaviour
	{
		// the maximum distance the player needs to be to an object to interact with it
		//1.75 is the optimal distance to now have any direction click too far
		//NOTE FOR ANYONE EDITING THIS IN THE FUTURE: Character's head is slightly below the top of the tile
		//hence top reach is slightly lower than bottom reach, where the legs go exactly to the bottom of the tile.
		public const float interactionDistance = 1.75f;

		public GameObject ghost;

		[SyncVar] public JobType JobType = JobType.NULL;

		private float pingUpdate;

		[SyncVar(hook = "OnNameChange")] public string playerName = " ";

		private ChatChannel selectedChannels;

		public PlayerNetworkActions playerNetworkActions { get; set; }

		public WeaponNetworkActions weaponNetworkActions { get; set; }

		public SoundNetworkActions soundNetworkActions { get; set; }

		public PlayerHealth playerHealth { get; set; }

		public PlayerMove playerMove { get; set; }

		public PushPull pushPull { get; set; }

		public PlayerSprites playerSprites { get; set; }

		private PlayerSync _playerSync; //Example of good on-demand reference init
		public PlayerSync PlayerSync => _playerSync ? _playerSync : ( _playerSync = GetComponent<PlayerSync>() );

		public RegisterTile registerTile { get; set; }

		public MouseInputController mouseInputController { get; set; }

		public HitIcon hitIcon { get; set; }

		public Vector3Int WorldPos => registerTile.WorldPosition;

		public ChatChannel SelectedChannels
		{
			get { return selectedChannels & GetAvailableChannelsMask(); }
			set { selectedChannels = value; }
		}

		private static bool verified;
		private static ulong SteamID;

		public override void OnStartClient()
		{
			//Local player is set a frame or two after OnStartClient
			StartCoroutine(WaitForLoad());
			Init();
			base.OnStartClient();
		}

		private IEnumerator WaitForLoad()
		{
			//fixme: name isn't resolved at the moment of pool creation
			//(player pools now use netIDs, but it would be nice to have names for readability)
			yield return new WaitForSeconds(2f);
			OnNameChange(playerName);
			yield return new WaitForSeconds(1f);
			//Refresh chat log:
	//s		ChatRelay.Instance.RefreshLog();
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

		private void Start()
		{
			playerNetworkActions = GetComponent<PlayerNetworkActions>();
			registerTile = GetComponent<RegisterTile>();
			playerHealth = GetComponent<PlayerHealth>();
			pushPull = GetComponent<PushPull>();
			weaponNetworkActions = GetComponent<WeaponNetworkActions>();
			soundNetworkActions = GetComponent<SoundNetworkActions>();
			mouseInputController = GetComponent<MouseInputController>();
			hitIcon = GetComponentInChildren<HitIcon>();
		}

		private void Init()
		{
			if (isLocalPlayer)
			{
				UIManager.ResetAllUI();
				UIManager.DisplayManager.SetCameraFollowPos();
				int rA = Random.Range(0, 3);
				SoundManager.PlayVarAmbient(rA);
				playerMove = GetComponent<PlayerMove>();
				playerSprites = GetComponent<PlayerSprites>();
				GetComponent<MouseInputController>().enabled = true;

				if (!UIManager.Instance.playerListUIControl.window.activeInHierarchy)
				{
					UIManager.Instance.playerListUIControl.window.SetActive(true);
				}

				CmdTrySetInitialName(PlayerManager.PlayerNameCache);

				PlayerManager.SetPlayerForControl(gameObject);

				if (PlayerManager.LocalPlayerScript.JobType == JobType.NULL)
				{
					// I (client) have connected to the server, ask server what is going on, by first asking what the UI should be?

					UIManager.Display.DetermineGameMode();
				}
				UIManager.SetDeathVisibility(true);
				if ( BuildPreferences.isSteamServer ) {
					// Send request to be authenticated by the server
					if ( Client.Instance != null ) {
						Logger.Log( "Client Requesting Auth", Category.Steam );
						// Generate authentication Ticket
						var ticket = Client.Instance.Auth.GetAuthSessionTicket();
						var ticketBinary = ticket.Data;
						// Send Clientmessage to authenticate
						RequestAuthMessage.Send( Client.Instance.SteamId, ticketBinary );
					} else {
						Logger.Log( "Client NOT requesting auth", Category.Steam );
					}
				}
//				Request sync to get all the latest transform data
				new RequestSyncMessage().Send();
				SelectedChannels = ChatChannel.Local;

			}
			else if (isServer)
			{
				playerMove = GetComponent<PlayerMove>();

				//Add player to player list
				PlayerList.Instance.Add(new ConnectedPlayer
				{
					Connection = connectionToClient,
					GameObject = gameObject,
					Job = JobType
				});
			}
		}

		public bool canNotInteract()
		{
			return playerMove == null || !playerMove.allowInput || playerMove.isGhost
				|| playerHealth.IsDead || playerHealth.IsCrit;
		}

		public override void UpdateMe()
		{
			//Read out of ping in toolTip
			pingUpdate += Time.deltaTime;
			if (pingUpdate >= 5f)
			{
				pingUpdate = 0f;
				int ping = CustomNetworkManager.Instance.client.GetRTT();
				UIManager.SetToolTip = "ping: " + ping;
			}
		}

		/// <summary>
		/// Trying to set initial name, if player has none
		/// </summary>
		[Command]
		private void CmdTrySetInitialName(string name)
		{
//			Logger.Log($"TrySetName {name}");
			if (PlayerList.Instance != null)
			{
				var player = PlayerList.Instance.Get(connectionToClient);
				if ( player.HasNoName() )
				{
					player.Name = name;
				}
				playerName = player.Name;
				PlayerList.Instance.TryAddScores( player.Name );
			}
		}

		// On playerName variable change across all clients, make sure obj is named correctly
		// and set in Playerlist for that client
		public void OnNameChange(string newName)
		{
			if (string.IsNullOrEmpty(newName))
			{
				Logger.LogError("NO NAME PROVIDED!", Category.Connections);
				return;
			}
//			Logger.Log($"OnNameChange: GOName '{gameObject.name}'->'{newName}'; playerName '{playerName}'->'{newName}'");
			playerName = newName;
			gameObject.name = newName;
		}

		public bool IsHidden => !PlayerSync.ClientState.Active;

		public bool IsInReach( GameObject go, float interactDist = interactionDistance ) {
			var rt = go.RegisterTile();
			if ( rt ) {
				return IsInReach( rt, interactDist );
			} else {
				return IsInReach( go.transform.position, interactDist );
			}
		}

		/// The smart way:
		///  <inheritdoc cref="IsInReach(Vector3,float)"/>
		public bool IsInReach(RegisterTile otherObject, float interactDist = interactionDistance) {
			return IsInReach( registerTile, otherObject, interactDist );
		}
		///     Checks if the player is within reach of something
		/// <param name="otherPosition">The position of whatever we are trying to reach</param>
		/// <param name="interactDist">Maximum distance of interaction between the player and other objects</param>
		public bool IsInReach(Vector3 otherPosition, float interactDist = interactionDistance) {
			Vector3Int worldPosition = registerTile.WorldPosition;
			return IsInReach( worldPosition, otherPosition, interactDist );
		}

		///Smart way to detect reach, supports high speeds in ships. Should use it more!
		public static bool IsInReach( RegisterTile from, RegisterTile to, float interactDist = interactionDistance ) {
			return from.Matrix == to.Matrix && IsInReach( from.Position, to.Position, interactDist )
			       || IsInReach( from.WorldPosition, to.WorldPosition, interactDist );
		}

		public static bool IsInReach( Vector3 from, Vector3 to, float interactDist = interactionDistance ) {
			//If click is in diagonal direction, extend reach slightly
			int distanceX = Mathf.FloorToInt( Mathf.Abs( from.x - to.x ) );
			int distanceY = Mathf.FloorToInt( Mathf.Abs( from.y - to.y ) );
			if ( distanceX == 1 && distanceY == 1 ) {
				return ( from - to ).magnitude <= interactDist + 0.4f;
			}

			//if cardinal direction, use regular reach
			return ( from - to ).magnitude <= interactDist;
		}

		public ChatChannel GetAvailableChannelsMask(bool transmitOnly = true)
		{
			if(this == null){
				return ChatChannel.OOC;
			}
			PlayerMove pm = gameObject.GetComponent<PlayerMove>();
			if (pm.isGhost)
			{
				ChatChannel ghostTransmitChannels = ChatChannel.Ghost | ChatChannel.OOC;
				ChatChannel ghostReceiveChannels = ChatChannel.Examine | ChatChannel.System | ChatChannel.Combat;
				if (transmitOnly)
				{
					return ghostTransmitChannels;
				}
				return ghostTransmitChannels | ghostReceiveChannels;
			}

			//TODO: Checks if player can speak (is not gagged, unconcious, has no mouth)
			ChatChannel transmitChannels = ChatChannel.OOC | ChatChannel.Local;
			if (CustomNetworkManager.Instance._isServer)
			{
				PlayerNetworkActions pna = gameObject.GetComponent<PlayerNetworkActions>();
				if ( pna && pna.SlotNotEmpty("ear") )
				{
					Headset headset = pna.Inventory["ear"].Item.GetComponent<Headset>();
					if ( headset )
					{
						EncryptionKeyType key = headset.EncryptionKey;
						transmitChannels = transmitChannels | EncryptionKey.Permissions[key];
					}
				}
			}
			else
			{
				GameObject earSlotItem = UIManager.InventorySlots["ear"].Item;
				if ( earSlotItem )
				{
					Headset headset = earSlotItem.GetComponent<Headset>();
					if ( headset )
					{
						EncryptionKeyType key = headset.EncryptionKey;
						transmitChannels = transmitChannels | EncryptionKey.Permissions[key];
					}
				}
			}


			ChatChannel receiveChannels = ChatChannel.Examine | ChatChannel.System | ChatChannel.Combat;

			if (transmitOnly)
			{
				return transmitChannels;
			}
			return transmitChannels | receiveChannels;
		}

		public ChatModifier GetCurrentChatModifiers()
		{
			ChatModifier modifiers = ChatModifier.None;
			if (playerMove.isGhost)
			{
				return ChatModifier.None;
			}

			//TODO add missing modifiers
			//TODO add if for being drunk
			//ChatModifier modifiers = ChatModifier.Drunk;

			if (JobType == JobType.CLOWN)
			{
				modifiers |= ChatModifier.Clown;

			}

			return modifiers;
		}

		//Tooltips inspector bar
		public void OnHoverStart()
		{
			UIManager.SetToolTip = name;
		}

		public void OnHoverEnd()
		{
			UIManager.SetToolTip = "";
		}

		//MatrixMove is rotating (broadcast via MatrixMove)
		public void MatrixMoveStartRotation(){
			if(PlayerManager.LocalPlayer == gameObject){
				//We need to handle lighting stuff for matrix rotations for local player:
				Camera2DFollow.followControl.lightingSystem.matrixRotationMode = true;
			}
		}
		public void MatrixMoveStopRotation(){
			if(PlayerManager.LocalPlayer == gameObject){
				//We need to handle lighting stuff for matrix rotations for local player:
				Camera2DFollow.followControl.lightingSystem.matrixRotationMode = false;
			}
		}
	}
