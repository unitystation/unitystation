using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Detective;
using Systems.Ai;
using UnityEngine;
using Mirror;
using HealthV2;
using Player;
using Player.Movement;
using UI.Action;
using Items;
using Objects.Construction;
using Player.Language;
using ScriptableObjects;
using Systems.StatusesAndEffects;
using Tiles;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine.Serialization;

public class PlayerScript : NetworkBehaviour, IMatrixRotation, IAdminInfo, IActionGUI, IHoverTooltip
{
	/// maximum distance the player needs to be to an object to interact with it
	public const float interactionDistance = 1.5f;

	public Mind mind  { private set; get; }
	public PlayerInfo PlayerInfo;

	[FormerlySerializedAs("playerStateSettings")] [SerializeField]
	private PlayerTypeSettings playerTypeSettings = null;
	public PlayerTypeSettings PlayerTypeSettings => playerTypeSettings;
	public PlayerTypes PlayerType => playerTypeSettings.PlayerType;

	/// <summary>
	/// Current character settings for this player.
	/// </summary>
	public CharacterSheet characterSettings = new CharacterSheet();

	[HideInInspector, SyncVar(hook = nameof(SyncPlayerName))]
	public string playerName = " ";

	[HideInInspector, SyncVar(hook = nameof(SyncVisibleName))]
	public string visibleName = " ";

	public PlayerNetworkActions playerNetworkActions { get; set; }

	public WeaponNetworkActions weaponNetworkActions { get; set; }

	public OrientationEnum CurrentDirection => playerDirectional.CurrentDirection;

	/// <summary>
	/// Will be null if player is a ghost.
	/// </summary>
	public PlayerHealthV2 playerHealth { get; set; }

	public MovementSynchronisation playerMove { get; set; }
	public PlayerSprites playerSprites { get; set; }

	/// <summary>
	/// Will be null if player is a ghost.
	/// </summary>
	public UniversalObjectPhysics objectPhysics { get; set; }

	public Rotatable playerDirectional { get; set; }

	public MovementSynchronisation PlayerSync;

	public Equipment Equipment { get; private set; }

	public RegisterPlayer RegisterPlayer { get; private set; }

	private PlayerCrafting playerCrafting;

	public PlayerCrafting PlayerCrafting => playerCrafting;

	public PlayerOnlySyncValues PlayerOnlySyncValues { get; private set; }

	public HasCooldowns Cooldowns { get; set; }

	public MobLanguages MobLanguages { get; private set; }

	public MouseInputController mouseInputController { get; set; }

	public ChatIcon chatIcon { get; private set; }

	public StatusEffectManager statusEffectManager { get; private set; }

	/// <summary>
	/// Serverside world position.
	/// Outputs correct world position even if you're hidden (e.g. in a locker)
	/// </summary>
	public Vector3Int AssumedWorldPos => objectPhysics.registerTile.WorldPosition;

	[SyncVar] public Vector3Int SyncedWorldPos = new Vector3Int(0, 0, 0);

	/// <summary>
	/// World position of the player.
	/// Returns InvalidPos if you're hidden (e.g. in a locker)
	/// </summary>If the
	public Vector3Int WorldPos => RegisterPlayer.WorldPosition;

	/// <summary>
	/// This player's item storage.
	/// </summary>
	public DynamicItemStorage DynamicItemStorage { get; private set; }

	private static bool verified;
	private static ulong SteamID;

	public float RTT;

	[HideInInspector] public bool RcsMode;
	[HideInInspector] public MatrixMove RcsMatrixMove;

	private bool isUpdateRTT;
	private float waitTimeForRTTUpdate = 0f;

	/// <summary>
	/// Whether a player is connected in the game object this script is on, valid serverside only
	/// </summary>
	public bool HasSoul => connectionToClient != null;

	[SerializeField] private ActionData actionData = null;
	public ActionData ActionData => actionData;

	//The object the player will receive chat and send chat from.
	//E.g. usually same object as this script but for Ai it will be their core object
	//Serverside only
	[SerializeField]
	private GameObject playerChatLocation = null;
	public GameObject PlayerChatLocation => playerChatLocation;

	[SerializeField]
	//TODO move this to somewhere else?
	private bool canVentCrawl = false;
	public bool CanVentCrawl => canVentCrawl;

	#region Lifecycle

	private void Awake()
	{
		playerSprites = GetComponent<PlayerSprites>();
		playerNetworkActions = GetComponent<PlayerNetworkActions>();
		RegisterPlayer = GetComponent<RegisterPlayer>();
		playerHealth = GetComponent<PlayerHealthV2>();
		objectPhysics = GetComponent<UniversalObjectPhysics>();
		weaponNetworkActions = GetComponent<WeaponNetworkActions>();
		mouseInputController = GetComponent<MouseInputController>();
		chatIcon = GetComponentInChildren<ChatIcon>(true);
		playerMove = GetComponent<MovementSynchronisation>();
		playerDirectional = GetComponent<Rotatable>();
		DynamicItemStorage = GetComponent<DynamicItemStorage>();
		Equipment = GetComponent<Equipment>();
		Cooldowns = GetComponent<HasCooldowns>();
		PlayerOnlySyncValues = GetComponent<PlayerOnlySyncValues>();
		playerCrafting = GetComponent<PlayerCrafting>();
		PlayerSync = GetComponent<MovementSynchronisation>();
		statusEffectManager = GetComponent<StatusEffectManager>();
		MobLanguages = GetComponent<MobLanguages>();
	}

	public override void OnStartClient()
	{
		Init();
		SyncPlayerName(playerName, playerName);
	}

	// isLocalPlayer is always called after OnStartClient
	public override void OnStartLocalPlayer()
	{
		Init();
		waitTimeForRTTUpdate = 0f;

		if (IsNormal)
		{
			UIManager.Internals.SetupListeners();
			UIManager.Instance.panelHudBottomController.SetupListeners();
		}

		isUpdateRTT = true;
	}

	// You know the drill
	public override void OnStartServer()
	{
		Init();
	}

	private void OnEnable()
	{
		EventManager.AddHandler(Event.PlayerRejoined, Init);
		EventManager.AddHandler(Event.GhostSpawned, OnPlayerBecomeGhost);
		EventManager.AddHandler(Event.PlayerRejoined, OnPlayerReturnedToBody);

		//Client and Local host only
		if (CustomNetworkManager.IsHeadless) return;
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		EventManager.RemoveHandler(Event.PlayerRejoined, Init);
		EventManager.RemoveHandler(Event.GhostSpawned, OnPlayerBecomeGhost);
		EventManager.RemoveHandler(Event.PlayerRejoined, OnPlayerReturnedToBody);

		if (CustomNetworkManager.IsHeadless) return;
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}


	public void Init()
	{
		if (isLocalPlayer)
		{
			EnableLighting(true);
			UIManager.ResetAllUI();
			GetComponent<MouseInputController>().enabled = true;

			if (UIManager.Instance.statsTab.window.activeInHierarchy == false)
			{
				UIManager.Instance.statsTab.window.SetActive(true);
			}

			IPlayerControllable input = GetComponent<IPlayerControllable>();

			if (TryGetComponent<AiMouseInputController>(out var aiMouseInputController))
			{
				input = aiMouseInputController;
			}

			PlayerManager.SetPlayerForControl(gameObject, input);

			if (PlayerType == PlayerTypes.Ghost)
			{
				if (PlayerList.Instance.IsClientAdmin)
				{
					UIManager.LinkUISlots(ItemStorageLinkOrigin.adminGhost);
				}

				// stop the crit notification and change overlay to ghost mode
				SoundManager.Stop("Critstate");
				UIManager.PlayerHealthUI.heartMonitor.overlayCrits.SetState(OverlayState.death);
				// show ghosts
				var mask = Camera2DFollow.followControl.cam.cullingMask;
				mask |= 1 << LayerMask.NameToLayer("Ghosts");
				Camera2DFollow.followControl.cam.cullingMask = mask;
			}
			//Normal players
			else if (IsPlayerSemiGhost == false)
			{
				UIManager.LinkUISlots(ItemStorageLinkOrigin.localPlayer);
				// Hide ghosts
				var mask = Camera2DFollow.followControl.cam.cullingMask;
				mask &= ~(1 << LayerMask.NameToLayer("Ghosts"));
				Camera2DFollow.followControl.cam.cullingMask = mask;
			}
			//Players like blob or Ai
			else
			{
				// stop the crit notification and change overlay to ghost mode
				SoundManager.Stop("Critstate");
				UIManager.PlayerHealthUI.heartMonitor.overlayCrits.SetState(OverlayState.death);
				// hide ghosts
				var mask = Camera2DFollow.followControl.cam.cullingMask;
				mask &= ~(1 << LayerMask.NameToLayer("Ghosts"));
				Camera2DFollow.followControl.cam.cullingMask = mask;
			}

			EventManager.Broadcast(Event.UpdateChatChannels);
			UpdateStatusTabUI();
		}
	}

	#endregion

	//Client Side Only
	private void UpdateMe()
	{
		if (isUpdateRTT && hasAuthority)
		{
			RTTUpdate();
		}
	}

	private void RTTUpdate()
	{
		waitTimeForRTTUpdate += Time.deltaTime;
		if (waitTimeForRTTUpdate > 0.5f)
		{
			waitTimeForRTTUpdate = 0f;
			RTT = (float) NetworkTime.rtt;
			if (playerHealth != null)
			{
				playerHealth.RTT = RTT;
			}

			CmdUpdateRTT(RTT);
		}
	}

	private void UpdateStatusTabUI()
	{
		if (StatsTab.Instance == null) return;
		StatsTab.Instance.UpdateCurrentMap();
		StatsTab.Instance.UpdateGameMode();
		StatsTab.Instance.UpdateRoundTime();
		switch (GameManager.Instance.CurrentRoundState)
		{
			case (RoundState.Started):
				StatsTab.Instance.UpdateRoundStatus("Started");
				break;
			case (RoundState.PreRound):
				StatsTab.Instance.UpdateRoundStatus("Preround");
				break;
			case (RoundState.Ended):
				StatsTab.Instance.UpdateRoundStatus("Ended! Restarting soon..");
				break;
			default:
				StatsTab.Instance.UpdateRoundStatus("???");
				break;
		}
	}

	[Command]
	private void CmdUpdateRTT(float rtt)
	{
		RTT = rtt;
		if (playerHealth != null)
		{
			playerHealth.RTT = rtt;
		}
	}

	[Command(requiresAuthority = false)]
	public void UpdateLastSyncedPosition()
	{
		SetLastRecordedPosition();
	}

	[Server]
	private void SetLastRecordedPosition()
	{
		SyncedWorldPos = gameObject.AssumedWorldPosServer().CutToInt();
	}

	/// <summary>
	/// Sets the game object for where the player can receive and send chat message from
	/// </summary>
	/// <param name="newLocation"></param>
	[Server]
	public void SetPlayerChatLocation(GameObject newLocation)
	{
		playerChatLocation = newLocation;
	}


	public void SetMind(Mind InMind)
	{
		mind = InMind;
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
		Logger.Log("Local player become Ghost", Category.Ghosts);
		EnableLighting(true);
	}

	private void OnPlayerBecomeGhost()
	{
		Logger.Log("Local player returned to the body", Category.Ghosts);
		EnableLighting(false);
	}

	public void SyncPlayerName(string oldValue, string value)
	{
		playerName = value;
		gameObject.name = value;
		RefreshVisibleName();
	}

	public bool IsHidden => PlayerSync.IsVisible == false;

	/// <summary>
	/// True if this player is a ghost
	/// </summary>
	public bool IsGhost => PlayerType == PlayerTypes.Ghost;

	/// <summary>
	/// True if this player is a normal player prefab (not ghost, Ai, blob, etc)
	/// </summary>
	public bool IsNormal => PlayerType == PlayerTypes.Normal;

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

	// If the player acts like a ghost but is still playing ingame, used for blobs and in the future maybe AI.
	public bool IsPlayerSemiGhost => PlayerType == PlayerTypes.Blob || PlayerType == PlayerTypes.Ai;

	public void ReturnGhostToBody()
	{
		if(mind == null) return;

		var ghost = mind.ghost;
		if (mind.IsGhosting == false || ghost == null) return;

		ghost.playerNetworkActions.GhostEnterBody();
	}

	public object Chat { get; internal set; }

	public bool IsGameObjectReachable(GameObject go, bool isServer, float interactDist = interactionDistance,
		GameObject context = null)
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
	public bool IsRegisterTileReachable(RegisterTile otherObject, bool isServer,
		float interactDist = interactionDistance, GameObject context = null)
	{
		return Validations.IsReachableByRegisterTiles(RegisterPlayer, otherObject, isServer, interactDist,
			context: context);
	}

	///     Checks if the player is within reach of something
	/// <param name="otherPosition">The position of whatever we are trying to reach</param>
	/// <param name="isServer">True if being executed on server, false otherwise</param>
	/// <param name="interactDist">Maximum distance of interaction between the player and other objects</param>
	/// <param name="context">If not null, will ignore collisions caused by this gameobject</param>
	public bool IsPositionReachable(Vector3 otherPosition, bool isServer, float interactDist = interactionDistance,
		GameObject context = null)
	{
		return Validations.IsReachableByPositions(
			isServer ? RegisterPlayer.WorldPositionServer : RegisterPlayer.WorldPosition, otherPosition, isServer,
			interactDist, context: context);
	}

	/// <summary>
	/// Sets the IC name for this player and refreshes the visible name. Name will be kept if respawned.
	/// </summary>
	/// <param name="newName">The new name to give to the player.</param>
	public void SetPermanentName(string newName)
	{
		characterSettings.Name = newName;
		playerName = newName;
		RefreshVisibleName();
	}

	public ChatChannel GetAvailableChannelsMask(bool transmitOnly = true)
	{
		ChatChannel transmitChannels = playerTypeSettings.TransmitChannels;
		ChatChannel receiveChannels = playerTypeSettings.ReceiveChannels;

		//Can't move this to PlayerStateSettings as we need this for when in body and dead
		if (playerHealth != null && playerHealth.IsDead)
		{
			transmitChannels = ChatChannel.Ghost | ChatChannel.OOC;
			receiveChannels = ChatChannel.Examine | ChatChannel.System | ChatChannel.Combat |
			                  ChatChannel.Binary | ChatChannel.Command | ChatChannel.Common |
			                  ChatChannel.Engineering | ChatChannel.Medical | ChatChannel.Science |
			                  ChatChannel.Security | ChatChannel.Service | ChatChannel.Supply |
			                  ChatChannel.Syndicate | ChatChannel.Alien | ChatChannel.Blob;

		}

		//Ai channels limited when not allowed to use radio
		if (PlayerType == PlayerTypes.Ai)
		{
			if (GetComponent<AiPlayer>().AllowRadio == false)
			{
				transmitChannels = ChatChannel.Binary | ChatChannel.OOC | ChatChannel.Local;
				receiveChannels = ChatChannel.Binary | ChatChannel.Local | ChatChannel.Examine |
				                  ChatChannel.System | ChatChannel.Combat;
			}
		}

		//TODO: Checks if player can speak (is not gagged, unconcious, has no mouth)
		if (playerTypeSettings.CheckForRadios)
		{
			var playerStorage = gameObject.GetComponent<DynamicItemStorage>();
			if (playerStorage != null)
			{
				foreach (var earSlot in playerStorage.GetNamedItemSlots(NamedSlot.ear))
				{
					if (earSlot.IsEmpty) continue;
					if (earSlot.Item.TryGetComponent<Headset>(out var headset) == false) continue;
					if (headset.isEMPed) continue;

					EncryptionKeyType key = headset.EncryptionKey;
					transmitChannels |= EncryptionKey.Permissions[key];
				}
			}
		}

		if (transmitOnly)
		{
			return transmitChannels;
		}

		return transmitChannels | receiveChannels;
	}

	// Syncvisiblename
	public void SyncVisibleName(string oldValue, string value)
	{
		visibleName = value;
	}

	// Update visible name.
	public void RefreshVisibleName()
	{
		string newVisibleName;

		if (IsNormal == false || Equipment.IsIdentityObscured() == false)
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

	// Tooltips inspector bar
	public void OnMouseEnter()
	{
		if (gameObject.IsAtHiddenPos()) return;
		UIManager.SetToolTip = visibleName;
		UIManager.SetHoverToolTip = gameObject;

	}

	public void OnMouseExit()
	{
		UIManager.SetToolTip = "";
		UIManager.SetHoverToolTip = null;
	}

	private System.Random RNG = new System.Random();

	public int ClueHandsImprintInverseChance = 55;
	public int ClueUniformImprintInverseChance = 65;
	public int ClueSpeciesImprintInverseChance = 85;


	public void OnInteract(TargetedInteraction Interaction, Component interactable)
	{
		if (Interaction == null) return;
		if (IsNormal == false) return;
		if (ComponentManager.TryGetUniversalObjectPhysics(interactable.gameObject, out var uop) == false) return;
		if(uop.attributes.HasComponent == false) return;

		var details = uop.attributes.Component.AppliedDetails;
		if (details == null) return;

		if (RNG.Next(0, 100) > ClueHandsImprintInverseChance)
		{
			bool wearingGloves = false;
			var slotGlove = DynamicItemStorage.GetNamedItemSlots(NamedSlot.hands).PickRandom();
			if (slotGlove != null && slotGlove.Item != null)
			{
				wearingGloves = true;
				details.AddDetail(new Detail
				{
					CausedByInstanceID = slotGlove.Item.gameObject.GetInstanceID(),
					Description = $" A fibre from a {slotGlove.Item.gameObject.ExpensiveName()}",
					DetailType = DetailType.Fibre
				});
			}

			if (wearingGloves == false)
			{
				var slot = DynamicItemStorage.GetActiveHandSlot();
				details.AddDetail(new Detail
				{
					CausedByInstanceID = slot.ItemStorage.gameObject.GetInstanceID(),
					Description = $" A fingerprint ",
					DetailType = DetailType.Fingerprints
				});
			}
		}


		if (RNG.Next(0, 100) > ClueUniformImprintInverseChance)
		{
			var slot = DynamicItemStorage.GetNamedItemSlots(NamedSlot.uniform).PickRandom();
			if (slot != null && slot.Item != null)
			{
				details.AddDetail(new Detail
				{
					CausedByInstanceID = slot.Item.gameObject.GetInstanceID(),
					Description = $" A fibre from a {slot.Item.gameObject.ExpensiveName()}",
					DetailType = DetailType.Fibre
				});
			}
		}

		if (RNG.Next(0, 100) > ClueSpeciesImprintInverseChance)
		{
			details.AddDetail(new Detail
			{
				CausedByInstanceID = this.gameObject.GetInstanceID(),
				Description = playerSprites.RaceBodyparts.Base.ClueString,
				DetailType = DetailType.SpeciesIdentify
			});
		}
	}

	public void OnMatrixRotate(MatrixRotationInfo rotationInfo)
	{
		//We need to handle lighting stuff for matrix rotations for local player:
		if (PlayerManager.LocalPlayerObject == gameObject && rotationInfo.IsClientside)
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
		var stringBuilder = new StringBuilder();

		stringBuilder.AppendLine($"Name: {characterSettings.Name}");
		stringBuilder.AppendLine($"Acc: {PlayerInfo?.Username}");

		if (connectionToClient == null)
		{
			stringBuilder.AppendLine("Has No Soul");
		}

		if (playerHealth != null)
		{
			stringBuilder.AppendLine($"Is Alive: {playerHealth.IsDead == false} Health: {playerHealth.OverallHealth}");
		}

		if (mind != null && mind.IsAntag)
		{
			stringBuilder.Insert(0, "<color=yellow>");
			stringBuilder.AppendLine($"Antag: {mind.GetAntag().Antagonist.AntagJobType}");
			stringBuilder.AppendLine($"Objectives : {mind.GetAntag().GetObjectiveSummary()}</color>");
		}

		return stringBuilder.ToString();
	}

	public void CallActionClient()
	{
		playerNetworkActions.CmdAskforAntagObjectives();
	}

	public void ActivateAntagAction(bool state)
	{
		UIActionManager.ToggleServer(mind , this, state);
	}

	//Used for Admins to VV function to toggle vent crawl as for some reason in build VV variable isnt working
	public void ToggleVentCrawl()
	{
		canVentCrawl = !canVentCrawl;
	}


	#region TOOLTIPDATA

	public string HoverTip()
	{
		StringBuilder finalText = new StringBuilder();
		if (characterSettings == null) return finalText.ToString();
		finalText.Append($"A {characterSettings.Species}.");
		finalText.Append($" {characterSettings.PlayerPronoun}.");
		return finalText.ToString();
	}

	public string CustomTitle()
	{
		return visibleName;
	}

	public Sprite CustomIcon()
	{
		foreach (var bodyPart in playerSprites.SurfaceSprite)
		{
			if(bodyPart.name != "head" || bodyPart.name != "Head") continue;
			return bodyPart.baseSpriteHandler.CurrentSprite;
		}

		return null;
	}

	public List<Sprite> IconIndicators()
	{
		//TODO: add indicators for players.
		return null;
	}

	public List<TextColor> InteractionsStrings()
	{
		return null;
	}

	#endregion

}

[Flags]
public enum PlayerTypes
{
	None = 0,
	Normal = 1 << 0,
	Ghost = 1 << 1,
	Blob = 1 << 2,
	Ai = 1 << 3,
	Alien = 1 << 4
}
