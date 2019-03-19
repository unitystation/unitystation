using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerScript : ManagedNetworkBehaviour
{
	/// maximum distance the player needs to be to an object to interact with it
	public const float interactionDistance = 1.5f;

	[SyncVar] public JobType JobType = JobType.NULL;

	private float pingUpdate;

	[SyncVar(hook = "OnNameChange")] public string playerName = " ";

	private ChatChannel selectedChannels;

	public PlayerNetworkActions playerNetworkActions { get; set; }

	public WeaponNetworkActions weaponNetworkActions { get; set; }

	/// <summary>
	/// Will be null if player is a ghost.
	/// </summary>
	public PlayerHealth playerHealth { get; set; }

	public PlayerMove playerMove { get; set; }

	/// <summary>
	/// Will be null if player is a ghost.
	/// </summary>
	public ObjectBehaviour pushPull { get; set; }

	public UserControlledSprites playerSprites { get; set; }

	private PlayerSync _playerSync; //Example of good on-demand reference init
	public PlayerSync PlayerSync => _playerSync ? _playerSync : (_playerSync = GetComponent<PlayerSync>());

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

	private void Awake()
	{
		playerNetworkActions = GetComponent<PlayerNetworkActions>();
		registerTile = GetComponent<RegisterTile>();
		playerHealth = GetComponent<PlayerHealth>();
		pushPull = GetComponent<ObjectBehaviour>();
		weaponNetworkActions = GetComponent<WeaponNetworkActions>();
		mouseInputController = GetComponent<MouseInputController>();
		hitIcon = GetComponentInChildren<HitIcon>(true);
		playerMove = GetComponent<PlayerMove>();
		playerSprites = GetComponent<UserControlledSprites>();
	}

	private void Init()
	{
		if (isLocalPlayer)
		{
			UIManager.ResetAllUI();
			UIManager.SetDeathVisibility(true);
			UIManager.DisplayManager.SetCameraFollowPos();
			int rA = Random.Range(0, 3);
			GetComponent<MouseInputController>().enabled = true;

			if (!UIManager.Instance.playerListUIControl.window.activeInHierarchy)
			{
				UIManager.Instance.playerListUIControl.window.SetActive(true);
			}

			CmdTrySetInitialName(PlayerManager.PlayerNameCache);

			PlayerManager.SetPlayerForControl(gameObject);

			if (IsGhost)
			{
				//stop the crit notification and change overlay to ghost mode
				SoundManager.Stop("Critstate");
				UIManager.PlayerHealthUI.heartMonitor.overlayCrits.SetState(OverlayState.death);
				//show ghosts
				var mask = Camera2DFollow.followControl.cam.cullingMask;
				mask |= 1 << LayerMask.NameToLayer("Ghosts");
				Camera2DFollow.followControl.cam.cullingMask = mask;

			}
			else
			{
				//play the spawn sound
				SoundManager.PlayVarAmbient(rA);
				//Hide ghosts
				var mask = Camera2DFollow.followControl.cam.cullingMask;
				mask &= ~(1 << LayerMask.NameToLayer("Ghosts"));
				Camera2DFollow.followControl.cam.cullingMask = mask;
			}

			//				Request sync to get all the latest transform data
			new RequestSyncMessage().Send();
			SelectedChannels = ChatChannel.Local;

		}
		else if (isServer)
		{
			playerMove = GetComponent<PlayerMove>();

			//Updates the player record on the server:
			PlayerList.Instance.Add(new ConnectedPlayer
			{
				Connection = connectionToClient,
					GameObject = gameObject,
					Job = JobType,
					Name = playerName
			});
		}
	}

	public bool canNotInteract()
	{
		return playerMove == null || !playerMove.allowInput || IsGhost ||
			playerHealth.ConsciousState != ConsciousState.CONSCIOUS;
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
			player.Name = name;

			playerName = player.Name;
			PlayerList.Instance.TryAddScores(player.Name);
			UpdateConnectedPlayersMessage.Send();
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

	/// <summary>
	/// True if this player is a ghost, meaning they exist in the ghost layer
	/// </summary>
	public bool IsGhost => PlayerUtils.IsGhost(gameObject);

	public bool IsInReach(GameObject go, float interactDist = interactionDistance)
	{
		var rt = go.RegisterTile();
		if (rt)
		{
			return IsInReach(rt, interactDist);
		}
		else
		{
			return IsInReach(go.transform.position, interactDist);
		}
	}

	/// The smart way:
	///  <inheritdoc cref="IsInReach(Vector3,float)"/>
	public bool IsInReach(RegisterTile otherObject, float interactDist = interactionDistance)
	{
		return IsInReach(registerTile, otherObject, interactDist);
	}
	///     Checks if the player is within reach of something
	/// <param name="otherPosition">The position of whatever we are trying to reach</param>
	/// <param name="interactDist">Maximum distance of interaction between the player and other objects</param>
	public bool IsInReach(Vector3 otherPosition, float interactDist = interactionDistance)
	{
		Vector3Int worldPosition = registerTile.WorldPosition;
		return IsInReach(worldPosition, otherPosition, interactDist);
	}

	///Smart way to detect reach, supports high speeds in ships. Should use it more!
	public static bool IsInReach(RegisterTile from, RegisterTile to, float interactDist = interactionDistance)
	{
		return from.Matrix == to.Matrix && IsInReach(from.Position, to.Position, interactDist) ||
			IsInReach(from.WorldPosition, to.WorldPosition, interactDist);
	}

	public static bool IsInReach(Vector3 from, Vector3 to, float interactDist = interactionDistance)
	{
		var distanceVector = from - to;
		return Mathf.Max( Mathf.Abs(distanceVector.x), Mathf.Abs(distanceVector.y) ) < interactDist;
	}

	public ChatChannel GetAvailableChannelsMask(bool transmitOnly = true)
	{
		if (this == null)
		{
			return ChatChannel.OOC;
		}
		PlayerMove pm = gameObject.GetComponent<PlayerMove>();
		if (IsGhost)
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
			if (pna && pna.SlotNotEmpty("ear"))
			{
				Headset headset = pna.Inventory["ear"].Item.GetComponent<Headset>();
				if (headset)
				{
					EncryptionKeyType key = headset.EncryptionKey;
					transmitChannels = transmitChannels | EncryptionKey.Permissions[key];
				}
			}
		}
		else
		{
			GameObject earSlotItem = UIManager.InventorySlots["ear"].Item;
			if (earSlotItem)
			{
				Headset headset = earSlotItem.GetComponent<Headset>();
				if (headset)
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
		if (IsGhost)
		{
			return ChatModifier.None;
		}
		if (playerHealth.IsCrit)
		{
			return ChatModifier.Mute;
		}
		if (playerHealth.IsSoftCrit)
		{
			modifiers |= ChatModifier.Whisper;
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
	public void MatrixMoveStartRotation()
	{
		if (PlayerManager.LocalPlayer == gameObject)
		{
			//We need to handle lighting stuff for matrix rotations for local player:
			Camera2DFollow.followControl.lightingSystem.matrixRotationMode = true;
		}
	}
	public void MatrixMoveStopRotation()
	{
		if (PlayerManager.LocalPlayer == gameObject)
		{
			//We need to handle lighting stuff for matrix rotations for local player:
			Camera2DFollow.followControl.lightingSystem.matrixRotationMode = false;
		}
	}
}