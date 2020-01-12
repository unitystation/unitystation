using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

public class PlayerScript : ManagedNetworkBehaviour, IMatrixRotation
{

	[Tooltip("Cooldown settings for this player.")]
	[SerializeField]
	private Cooldowns cooldowns;

	/// maximum distance the player needs to be to an object to interact with it
	public const float interactionDistance = 1.5f;

	public Mind mind;

	/// <summary>
	/// Current character settings for this player.
	/// </summary>
	public CharacterSettings characterSettings = new CharacterSettings();

	[SyncVar(hook = nameof(SyncPlayerName))] public string playerName = " ";

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

	public MouseInputController mouseInputController { get; set; }

	public HitIcon hitIcon { get; set; }

	public Vector3Int WorldPos => registerTile.WorldPositionServer;

	/// <summary>
	/// This player's item storage.
	/// </summary>
	public ItemStorage ItemStorage { get; private set; }

	private static bool verified;
	private static ulong SteamID;

	private Vector3IntEvent onTileReached = new Vector3IntEvent();
	public Vector3IntEvent OnTileReached() => onTileReached;

	public override void OnStartClient()
	{
		Init();
		SyncPlayerName(playerName);
		base.OnStartClient();
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

	void OnEnable()
	{
		EventManager.AddHandler(EVENT.PlayerRejoined, Init);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.PlayerRejoined, Init);
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
		playerMove = GetComponent<PlayerMove>();
		playerDirectional = GetComponent<Directional>();
		ItemStorage = GetComponent<ItemStorage>();
		Equipment = GetComponent<Equipment>();
	}

	public void Init()
	{
		if (isLocalPlayer)
		{
			UIManager.ResetAllUI();
			UIManager.DisplayManager.SetCameraFollowPos();
			GetComponent<MouseInputController>().enabled = true;

			if (!UIManager.Instance.playerListUIControl.window.activeInHierarchy)
			{
				UIManager.Instance.playerListUIControl.window.SetActive(true);
			}

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

				UIManager.LinkUISlots();
				//play the spawn sound
				SoundManager.PlayAmbience();
				//Hide ghosts
				var mask = Camera2DFollow.followControl.cam.cullingMask;
				mask &= ~(1 << LayerMask.NameToLayer("Ghosts"));
				Camera2DFollow.followControl.cam.cullingMask = mask;
			}

			//				Request sync to get all the latest transform data
			new RequestSyncMessage().Send();
			EventManager.Broadcast(EVENT.UpdateChatChannels);
		}
	}

	/// <summary>
	/// Is this player on cooldown for the particular cooldown type?
	/// </summary>
	/// <param name="cooldownType"></param>
	/// <returns></returns>
	public bool IsOnCooldown(CooldownType cooldownType)
	{
		return cooldowns.IsOnCooldown(cooldownType);
	}

	/// <summary>
	/// Begin the cooldown timer for the indicated cooldown type if not already on cooldown.
	/// </summary>
	/// <param name="cooldownType"></param>
	/// <returns>true if cooldown started, false if was already on cooldown</returns>
	public bool TryStartCooldown(CooldownType cooldownType)
	{
		return cooldowns.TryStartCooldown(cooldownType, this);
	}

	public void SyncPlayerName(string value)
	{
		playerName = value;
		gameObject.name = value;
	}

	public bool IsHidden => !PlayerSync.ClientState.Active;

	/// <summary>
	/// True if this player is a ghost, meaning they exist in the ghost layer
	/// </summary>
	public bool IsGhost => PlayerUtils.IsGhost(gameObject);

	public bool IsInReach(GameObject go, bool isServer, float interactDist = interactionDistance)
	{
		var rt = go.RegisterTile();
		if (rt)
		{
			return IsInReach(rt, isServer, interactDist);
		}
		else
		{
			return IsInReach(go.transform.position, isServer, interactDist);
		}
	}

	/// The smart way:
	///  <inheritdoc cref="IsInReach(Vector3,float)"/>
	public bool IsInReach(RegisterTile otherObject, bool isServer, float interactDist = interactionDistance)
	{
		return Validations.IsInReach(registerTile, otherObject, isServer, interactDist);
	}
	///     Checks if the player is within reach of something
	/// <param name="otherPosition">The position of whatever we are trying to reach</param>
	/// <param name="interactDist">Maximum distance of interaction between the player and other objects</param>
	public bool IsInReach(Vector3 otherPosition, bool isServer, float interactDist = interactionDistance)
	{
		return Validations.IsInReach(isServer ? registerTile.WorldPositionServer : registerTile.WorldPositionClient, otherPosition, interactDist);
	}

	public ChatChannel GetAvailableChannelsMask(bool transmitOnly = true)
	{
		var isDeadOrGhost = IsGhost;
		if (playerHealth != null)
		{
			isDeadOrGhost = playerHealth.IsDead;
		}

		if (isDeadOrGhost)
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
			var playerStorage = gameObject.GetComponent<ItemStorage>();
			if (playerStorage && !playerStorage.GetNamedItemSlot(NamedSlot.ear).IsEmpty)
			{
				Headset headset =  playerStorage.GetNamedItemSlot(NamedSlot.ear)?.Item?.GetComponent<Headset>();
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

		if (mind.occupation.JobType == JobType.CLOWN)
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
}