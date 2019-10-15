using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

public class PlayerScript : ManagedNetworkBehaviour
{
	/// maximum distance the player needs to be to an object to interact with it
	public const float interactionDistance = 1.5f;

	public Mind mind;

	/// <summary>
	/// Current character settings for this player.
	/// </summary>
	public CharacterSettings characterSettings = new CharacterSettings();

	private float pingUpdate;

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

	public RegisterPlayer registerTile { get; set; }

	public MouseInputController mouseInputController { get; set; }

	public HitIcon hitIcon { get; set; }

	public Vector3Int WorldPos => registerTile.WorldPositionServer;

	private static bool verified;
	private static ulong SteamID;

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
			int ping = (int)NetworkTime.rtt;
			UIManager.SetPingDisplay = string.Format("ping: {0,-5:D}", ping);
		}
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
		return IsInReach(registerTile, otherObject, isServer, interactDist);
	}
	///     Checks if the player is within reach of something
	/// <param name="otherPosition">The position of whatever we are trying to reach</param>
	/// <param name="interactDist">Maximum distance of interaction between the player and other objects</param>
	public bool IsInReach(Vector3 otherPosition, bool isServer, float interactDist = interactionDistance)
	{
		return IsInReach(isServer ? registerTile.WorldPositionServer : registerTile.WorldPositionClient, otherPosition, interactDist);
	}

	///Smart way to detect reach, supports high speeds in ships. Should use it more!
	public static bool IsInReach(RegisterTile from, RegisterTile to, bool isServer, float interactDist = interactionDistance)
	{
		if ( isServer )
		{
			return from.Matrix == to.Matrix && IsInReach(from.LocalPositionServer, to.LocalPositionServer, interactDist) ||
			IsInReach(from.WorldPositionServer, to.WorldPositionServer, interactDist);
		}
		else
		{
			return from.Matrix == to.Matrix && IsInReach(from.LocalPositionClient, to.LocalPositionClient, interactDist) ||
		       IsInReach(from.WorldPositionClient, to.WorldPositionClient, interactDist);
		}
	}

	public static bool IsInReach( Vector3 targetVector, float interactDist = interactionDistance )
	{
		return Mathf.Max( Mathf.Abs(targetVector.x), Mathf.Abs(targetVector.y) ) < interactDist;
	}

	public static bool IsInReach(Vector3 from, Vector3 to, float interactDist = interactionDistance)
	{
		var targetVector = from - to;
		return IsInReach( targetVector );
	}

	public ChatChannel GetAvailableChannelsMask(bool transmitOnly = true)
	{
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
			if (pna && pna.SlotNotEmpty(EquipSlot.ear))
			{
				Headset headset = pna.Inventory[EquipSlot.ear].Item.GetComponent<Headset>();
				if (headset)
				{
					EncryptionKeyType key = headset.EncryptionKey;
					transmitChannels = transmitChannels | EncryptionKey.Permissions[key];
				}
			}
		}
		else
		{
			GameObject earSlotItem = UIManager.InventorySlots[EquipSlot.ear].Item;
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

		if (mind.jobType == JobType.CLOWN)
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