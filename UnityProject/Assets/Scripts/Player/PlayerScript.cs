using UnityEngine;
using Mirror;
using System;
using Audio.Managers;
using Blob;
using Objects;

public class PlayerScript : ManagedNetworkBehaviour, IMatrixRotation, IAdminInfo
{
	/// maximum distance the player needs to be to an object to interact with it
	public const float interactionDistance = 1.5f;

	public Mind mind;

	/// <summary>
	/// Current character settings for this player.
	/// </summary>
	public CharacterSettings characterSettings = new CharacterSettings();

	[HideInInspector, SyncVar(hook = nameof(SyncPlayerName))] public string playerName = " ";

	[HideInInspector, SyncVar(hook = nameof(SyncVisibleName))] public string visibleName = " ";
	public PlayerNetworkActions playerNetworkActions { get; set; }

	public WeaponNetworkActions weaponNetworkActions { get; set; }

	public Orientation CurrentDirection => playerDirectional.CurrentDirection;
	/// <summary>
	/// Will be null if player is a ghost.
	/// </summary>
	public PlayerHealth playerHealth { get; set; }

	public PlayerMove playerMove { get; set; }
	public PlayerSprites playerSprites { get; set; }

	/// <summary>
	/// Will be null if player is a ghost.
	/// </summary>
	public ObjectBehaviour pushPull { get; set; }

	public Directional playerDirectional { get; set; }

	private PlayerSync _playerSync; //Example of good on-demand reference init
	public PlayerSync PlayerSync => _playerSync ? _playerSync : (_playerSync = GetComponent<PlayerSync>());

	public Equipment Equipment { get; private set; }

	public RegisterPlayer registerTile { get; set; }

	public HasCooldowns Cooldowns { get; set; }

	public MouseInputController mouseInputController { get; set; }

	public HitIcon hitIcon { get; set; }

	public ChatIcon chatIcon { get; private set; }

	/// <summary>
	/// Serverside world position.
	/// Outputs correct world position even if you're hidden (e.g. in a locker)
	/// </summary>
	public Vector3Int AssumedWorldPos => pushPull.AssumedWorldPositionServer();

	/// <summary>
	/// Serverside world position.
	/// Returns InvalidPos if you're hidden (e.g. in a locker)
	/// </summary>
	public Vector3Int WorldPos => registerTile.WorldPositionServer;

	/// <summary>
	/// This player's item storage.
	/// </summary>
	public ItemStorage ItemStorage { get; private set; }

	private static bool verified;
	private static ulong SteamID;

	private Vector3IntEvent onTileReached = new Vector3IntEvent();
	public Vector3IntEvent OnTileReached() => onTileReached;

	public float RTT;

	[HideInInspector]
	[SyncVar]
	public bool RcsMode;
	public MatrixMove RcsMatrixMove { get; set; }

	private bool isUpdateRTT;
	private float waitTimeForRTTUpdate = 0f;

	public override void OnStartClient()
	{
		Init();
		SyncPlayerName(playerName, playerName);
	}

	//isLocalPlayer is always called after OnStartClient
	public override void OnStartLocalPlayer()
	{
		Init();
		waitTimeForRTTUpdate = 0f;

		if (IsGhost == false)
		{
			UIManager.Internals.SetupListeners();
			UIManager.Instance.panelHudBottomController.SetupListeners();
		}
		
		isUpdateRTT = true;
	}

	//You know the drill
	public override void OnStartServer()
	{
		Init();
	}

	protected override void OnEnable()
	{
		base.OnEnable();

		EventManager.AddHandler(EVENT.PlayerRejoined, Init);
		EventManager.AddHandler(EVENT.GhostSpawned, OnPlayerBecomeGhost);
		EventManager.AddHandler(EVENT.PlayerRejoined, OnPlayerReturnedToBody);
	}

	public override void UpdateMe()
	{
		if (isUpdateRTT && !isServer)
		{
			RTTUpdate();
		}
	}

	void RTTUpdate()
	{
		waitTimeForRTTUpdate += Time.deltaTime;
		if (waitTimeForRTTUpdate > 0.5f)
		{
			waitTimeForRTTUpdate = 0f;
			RTT = (float)NetworkTime.rtt;
			if (playerHealth != null)
			{
				playerHealth.RTT = RTT;
			}
			CmdUpdateRTT(RTT);
		}
	}

	[Command]
	void CmdUpdateRTT(float rtt)
	{
		RTT = rtt;
		if (playerHealth != null)
		{
			playerHealth.RTT = rtt;
		}
	}

	/// <summary>
	/// This function enable fov and lighting
	/// </summary>
	/// <param name="enable"></param>
	private void EnableLighting(bool enable)
	{
		// Get the lighting system
		var lighting = Camera.main.GetComponent<LightingSystem>();
		if (!lighting)
		{
			Logger.LogWarning("Local Player can't find lighting system on Camera.main", Category.Lighting);
			return;
		}

		lighting.enabled = enable;
	}

	private void OnPlayerReturnedToBody()
	{
		Logger.Log("Local player become Ghost", Category.DebugConsole);
		EnableLighting(true);
	}

	private void OnPlayerBecomeGhost()
	{
		Logger.Log("Local player returned to the body", Category.DebugConsole);
		EnableLighting(false);
	}

	protected override void OnDisable()
	{
		base.OnDisable();

		EventManager.RemoveHandler(EVENT.PlayerRejoined, Init);
		EventManager.RemoveHandler(EVENT.GhostSpawned, OnPlayerBecomeGhost);
		EventManager.RemoveHandler(EVENT.PlayerRejoined, OnPlayerReturnedToBody);
	}

	private void Awake()
	{
		playerSprites = GetComponent<PlayerSprites>();
		playerNetworkActions = GetComponent<PlayerNetworkActions>();
		registerTile = GetComponent<RegisterPlayer>();
		playerHealth = GetComponent<PlayerHealth>();
		pushPull = GetComponent<ObjectBehaviour>();
		weaponNetworkActions = GetComponent<WeaponNetworkActions>();
		mouseInputController = GetComponent<MouseInputController>();
		hitIcon = GetComponentInChildren<HitIcon>(true);
		chatIcon = GetComponentInChildren<ChatIcon>(true);
		playerMove = GetComponent<PlayerMove>();
		playerDirectional = GetComponent<Directional>();
		ItemStorage = GetComponent<ItemStorage>();
		Equipment = GetComponent<Equipment>();
		Cooldowns = GetComponent<HasCooldowns>();

		if (GetComponent<BlobPlayer>() != null)
		{
			IsPlayerSemiGhost = true;
		}
	}

	public void Init()
	{
		if (isLocalPlayer)
		{
			EnableLighting(true);
			UIManager.ResetAllUI();
			GetComponent<MouseInputController>().enabled = true;

			if (!UIManager.Instance.playerListUIControl.window.activeInHierarchy)
			{
				UIManager.Instance.playerListUIControl.window.SetActive(true);
			}

			PlayerManager.SetPlayerForControl(gameObject, PlayerSync);

			if (IsGhost && !IsPlayerSemiGhost)
			{
				//stop the crit notification and change overlay to ghost mode
				SoundManager.Stop("Critstate");
				UIManager.PlayerHealthUI.heartMonitor.overlayCrits.SetState(OverlayState.death);
				//show ghosts
				var mask = Camera2DFollow.followControl.cam.cullingMask;
				mask |= 1 << LayerMask.NameToLayer("Ghosts");
				Camera2DFollow.followControl.cam.cullingMask = mask;

			}
			else if(!IsPlayerSemiGhost)
			{
				UIManager.LinkUISlots();
				//play the spawn sound
				SoundAmbientManager.PlayAudio("ambigen8");
				//Hide ghosts
				var mask = Camera2DFollow.followControl.cam.cullingMask;
				mask &= ~(1 << LayerMask.NameToLayer("Ghosts"));
				Camera2DFollow.followControl.cam.cullingMask = mask;
			}
			else
			{
				//stop the crit notification and change overlay to ghost mode
				SoundManager.Stop("Critstate");
				UIManager.PlayerHealthUI.heartMonitor.overlayCrits.SetState(OverlayState.death);
				//show ghosts
				var mask = Camera2DFollow.followControl.cam.cullingMask;
				mask &= ~(1 << LayerMask.NameToLayer("Ghosts"));
				Camera2DFollow.followControl.cam.cullingMask = mask;
			}

			EventManager.Broadcast(EVENT.UpdateChatChannels);
		}
	}

	public void SyncPlayerName(string oldValue, string value)
	{
		playerName = value;
		gameObject.name = value;
		RefreshVisibleName();
	}

	public bool IsHidden => !PlayerSync.ClientState.Active;

	/// <summary>
	/// True if this player is a ghost, meaning they exist in the ghost layer
	/// </summary>
	public bool IsGhost => PlayerUtils.IsGhost(gameObject);

	/// <summary>
	/// Same as is ghost, but also true when player inside his dead body
	/// </summary>
	public bool IsDeadOrGhost
	{
		get
		{
			var isDeadOrGhost = IsGhost;
			if (playerHealth != null)
			{
				isDeadOrGhost = playerHealth.IsDead;
			}
			return isDeadOrGhost;
		}
	}

	[HideInInspector]
	//If the player acts like a ghost but is still playing ingame, used for blobs and in the future maybe AI.
	public bool IsPlayerSemiGhost;

	public object Chat { get; internal set; }

	public bool IsGameObjectReachable(GameObject go, bool isServer, float interactDist = interactionDistance, GameObject context=null)
	{
		var rt = go.RegisterTile();
		if (rt)
		{
			return IsRegisterTileReachable(rt, isServer, interactDist, context: context);
		}
		else
		{
			return IsPositionReachable(go.transform.position, isServer, interactDist, context: context);
		}
	}

	/// The smart way:
	///  <inheritdoc cref="IsPositionReachable(Vector3, bool, float, GameObject)"/>
	public bool IsRegisterTileReachable(RegisterTile otherObject, bool isServer, float interactDist = interactionDistance, GameObject context=null)
	{
		return Validations.IsReachableByRegisterTiles(registerTile, otherObject, isServer, interactDist, context: context);
	}
	///     Checks if the player is within reach of something
	/// <param name="otherPosition">The position of whatever we are trying to reach</param>
	/// <param name="isServer">True if being executed on server, false otherwise</param>
	/// <param name="interactDist">Maximum distance of interaction between the player and other objects</param>
	/// <param name="context">If not null, will ignore collisions caused by this gameobject</param>
	public bool IsPositionReachable(Vector3 otherPosition, bool isServer, float interactDist = interactionDistance, GameObject context = null)
	{
		return Validations.IsReachableByPositions(isServer ? registerTile.WorldPositionServer : registerTile.WorldPositionClient, otherPosition, isServer, interactDist, context: context);
	}

	/// <summary>
	/// Sets the IC name for this player and refreshes the visible name. Name will be kept if respawned.
	/// </summary>
	/// <param name="newName">The new name to give to the player.</param>
	public void SetPermanentName(string newName)
	{
		characterSettings.Name = newName;
		playerName = newName;
	}

	public ChatChannel GetAvailableChannelsMask(bool transmitOnly = true)
	{
		if (IsDeadOrGhost && !IsPlayerSemiGhost)
		{
			ChatChannel ghostTransmitChannels = ChatChannel.Ghost | ChatChannel.OOC;
			ChatChannel ghostReceiveChannels = ChatChannel.Examine | ChatChannel.System | ChatChannel.Combat |
				ChatChannel.Binary | ChatChannel.Command | ChatChannel.Common | ChatChannel.Engineering |
				ChatChannel.Medical | ChatChannel.Science | ChatChannel.Security | ChatChannel.Service
				| ChatChannel.Supply | ChatChannel.Syndicate;
			if (transmitOnly)
			{
				return ghostTransmitChannels;
			}
			return ghostTransmitChannels | ghostReceiveChannels;
		}

		if (IsPlayerSemiGhost)
		{
			ChatChannel blobTransmitChannels = ChatChannel.Blob | ChatChannel.OOC;
			ChatChannel blobReceiveChannels = ChatChannel.Examine | ChatChannel.System | ChatChannel.Combat;

			if (transmitOnly)
			{
				return blobTransmitChannels;
			}

			return blobTransmitChannels | blobReceiveChannels;
		}

		//TODO: Checks if player can speak (is not gagged, unconcious, has no mouth)
		ChatChannel transmitChannels = ChatChannel.OOC | ChatChannel.Local;
		if (CustomNetworkManager.Instance._isServer)
		{
			var playerStorage = gameObject.GetComponent<ItemStorage>();
			if (playerStorage && !playerStorage.GetNamedItemSlot(NamedSlot.ear).IsEmpty)
			{
				Headset headset = playerStorage.GetNamedItemSlot(NamedSlot.ear)?.Item?.GetComponent<Headset>();
				if (headset)
				{
					EncryptionKeyType key = headset.EncryptionKey;
					transmitChannels = transmitChannels | EncryptionKey.Permissions[key];
				}
			}
		}
		else
		{
			GameObject earSlotItem = gameObject.GetComponent<ItemStorage>().GetNamedItemSlot(NamedSlot.ear).ItemObject;
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

	//Syncvisiblename
	public void SyncVisibleName(string oldValue, string value)
	{
		visibleName = value;
	}

	//Update visible name.
	public void RefreshVisibleName()
	{
		string newVisibleName;

		if (IsGhost || Equipment.IsIdentityObscured() == false)
		{
			newVisibleName = playerName; // can see face so real identity is known
		}
		else
		{
			// Returns Unknown if identity could not be found via equipment (ID, PDA)
			newVisibleName = Equipment.GetPlayerNameByEquipment();
		}

		SyncVisibleName(newVisibleName, newVisibleName);
	}

	//Tooltips inspector bar
	public void OnMouseEnter()
	{
		if (gameObject.IsAtHiddenPos()) return;
		UIManager.SetToolTip = visibleName;
	}

	public void OnMouseExit()
	{
		UIManager.SetToolTip = "";
	}

	public void OnMatrixRotate(MatrixRotationInfo rotationInfo)
	{
		//We need to handle lighting stuff for matrix rotations for local player:
		if (PlayerManager.LocalPlayer == gameObject && rotationInfo.IsClientside)
		{
			if (rotationInfo.IsStarting)
			{
				Camera2DFollow.followControl.lightingSystem.matrixRotationMode = true;
			}
			else if (rotationInfo.IsEnding)
			{
				Camera2DFollow.followControl.lightingSystem.matrixRotationMode = false;
			}
		}
	}

	public string AdminInfoString()
	{
		if (PlayerList.Instance.IsAntag(gameObject))
		{
			return $"<color=yellow>Name: {characterSettings.Name}\n" +
				   $"Acc: {characterSettings.Username}\n" +
				   $"Antag: True \n" +
				   "Objectives : "+ mind.GetAntag().GetObjectiveSummary() + "</color>";

		}

		return $"Name: {characterSettings.Name}\n" +
			   $"Acc: {characterSettings.Username}\n" +
			   $"Antag: False";
	}
}
