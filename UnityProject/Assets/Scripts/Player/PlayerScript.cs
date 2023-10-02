using System;
using System.Collections.Generic;
using System.Text;
using Core.Utils;
using Detective;
using Systems.Ai;
using UnityEngine;
using Mirror;
using HealthV2;
using Player;
using Items;
using Messages.Client.GhostRoles;
using Messages.Server;
using Player.Language;
using ScriptableObjects;
using Systems.Character;
using Systems.StatusesAndEffects;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Changeling;
using Logs;
using Systems.Faith;

public class PlayerScript : NetworkBehaviour, IMatrixRotation, IAdminInfo, IPlayerPossessable, IHoverTooltip, IRightClickable
{
	public GameObject GameObject => gameObject;
	public uint PossessingID => possessingID;
	public Mind PossessingMind { get; set; }
	public IPlayerPossessable PossessedBy { get; set; }
	public MindNIPossessingEvent OnPossessedBy { get; set; } = new MindNIPossessingEvent();

	[SyncVar(hook = nameof(SyncPossessingID))] private uint possessingID;

	public IPlayerPossessable Itself => this as IPlayerPossessable;

	public Mind Mind => PossessingMind;
	public PlayerInfo PlayerInfo;

	[FormerlySerializedAs("playerStateSettings")] [SerializeField]
	private PlayerTypeSettings playerTypeSettings = null;

	public PlayerTypeSettings PlayerTypeSettings => playerTypeSettings;
	public PlayerTypes PlayerType => playerTypeSettings.PlayerType;

	public ChatModifier inventorySpeechModifiers = ChatModifier.None;

	/// <summary>
	/// Current character settings for this player.
	/// </summary>
	[SyncVar] public CharacterSheet characterSettings = new CharacterSheet();

	[HideInInspector, SyncVar(hook = nameof(SyncPlayerName))]
	public string playerName = " ";

	[HideInInspector, SyncVar(hook = nameof(SyncVisibleName))]
	public string visibleName = " ";

	public PlayerNetworkActions PlayerNetworkActions { get; private set; }

	public WeaponNetworkActions WeaponNetworkActions { get; private set; }

	public OrientationEnum CurrentDirection => PlayerDirectional.CurrentDirection;

	/// <summary>
	/// Will be null if player is a ghost.
	/// </summary>
	public PlayerHealthV2 playerHealth { get; private set; }

	public MovementSynchronisation playerMove { get; private set; }
	public PlayerSprites playerSprites { get; private set; }

	/// <summary>
	/// Will be null if player is a ghost.
	/// </summary>
	public UniversalObjectPhysics ObjectPhysics { get; private set; }

	public Rotatable PlayerDirectional { get; private set; }

	public MovementSynchronisation PlayerSync;

	public Equipment Equipment { get; private set; }

	public RegisterPlayer RegisterPlayer { get; private set; }

	private PlayerCrafting playerCrafting;

	public PlayerCrafting PlayerCrafting => playerCrafting;

	public HasCooldowns Cooldowns { get; private set; }

	public MobLanguages MobLanguages { get; private set; }

	public MouseInputController MouseInputController { get; set; }

	public ChatIcon ChatIcon { get; private set; }

	public StatusEffectManager StatusEffectManager { get; private set; }

	/// <summary>
	/// Serverside world position.
	/// Outputs correct world position even if you're hidden (e.g. in a locker)
	/// </summary>
	public Vector3Int AssumedWorldPos => ObjectPhysics.registerTile.WorldPosition;

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
	private float waitTimeForRTTUpdate = 0f;

	[HideInInspector] public bool RcsMode;
	[HideInInspector] public MatrixMove RcsMatrixMove;

	//The object the player will receive chat and send chat from.
	//E.g. usually same object as this script but for Ai it will be their core object
	//Serverside only
	[SerializeField] private GameObject playerChatLocation = null;
	public GameObject PlayerChatLocation => playerChatLocation;

	[SerializeField]
	//TODO move this to somewhere else?
	private bool canVentCrawl = false;
	public bool CanVentCrawl => canVentCrawl;
	/// <summary>
	/// Whether a player is connected in the game object this script is on, valid serverside only
	/// </summary>
	public bool HasSoul => connectionToClient != null;
	private bool isUpdateRTT;

	public Action OnActionControlPlayer { get; set; }
	public Action OnActionPossess { get; set; }
	[field: SerializeField] public UnityEvent OnBodyPossesedByPlayer { get; set; }
	[field: SerializeField] public UnityEvent OnBodyUnPossesedByPlayer { get; set; }
	public Action<Intent> OnIntentChange;
	public Action OnLayDown;

	private System.Random RNG = new System.Random();
	public int ClueHandsImprintInverseChance = 55;
	public int ClueUniformImprintInverseChance = 65;
	public int ClueSpeciesImprintInverseChance = 85;

	/// maximum distance the player needs to be to an object to interact with it
	public const float INTERACTION_DISTANCE = 1.5f;
	public const float INTERACTION_DISTANCE_EXTENDED = 1.75f;

	private ChangelingMain changeling = null;
	public ChangelingMain Changeling
	{
		get
		{
			if (changeling == null)
			{
				if (CustomNetworkManager.IsServer)
				{
					if (playerHealth != null && playerHealth.brain != null && playerHealth.brain.gameObject.TryGetComponent<ChangelingMain>(out var change))
						changeling = change;
				} else
				{
					changeling = UIManager.Instance.displayControl.hudChangeling.ChangelingMain;
				}
			}
			return changeling;
		}
	}

	private Faith currentFaith = null;
	public Faith CurrentFaith
	{
		get => currentFaith;
		private set => currentFaith = value;
	}

	[field: SyncVar] public string FaithName { get; private set; } = "None";


	#region Lifecycle

	private void Awake()
	{
		playerSprites = GetComponent<PlayerSprites>();
		PlayerNetworkActions = GetComponent<PlayerNetworkActions>();
		RegisterPlayer = GetComponent<RegisterPlayer>();
		playerHealth = GetComponent<PlayerHealthV2>();
		ObjectPhysics = GetComponent<UniversalObjectPhysics>();
		WeaponNetworkActions = GetComponent<WeaponNetworkActions>();
		MouseInputController = GetComponent<MouseInputController>();
		ChatIcon = GetComponentInChildren<ChatIcon>(true);
		playerMove = GetComponent<MovementSynchronisation>();
		PlayerDirectional = GetComponent<Rotatable>();
		DynamicItemStorage = GetComponent<DynamicItemStorage>();
		Equipment = GetComponent<Equipment>();
		Cooldowns = GetComponent<HasCooldowns>();
		playerCrafting = GetComponent<PlayerCrafting>();
		PlayerSync = GetComponent<MovementSynchronisation>();
		StatusEffectManager = GetComponent<StatusEffectManager>();
		MobLanguages = GetComponent<MobLanguages>();
	}

	public override void OnStartClient()
	{
		SyncPlayerName(name, name);
	}

	private void OnEnable()
	{
		EventManager.AddHandler(Event.GhostSpawned, OnPlayerBecomeGhost);
		EventManager.AddHandler(Event.PlayerRejoined, OnPlayerReturnedToBody);

		//Client and Local host only
		if (CustomNetworkManager.IsHeadless) return;
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		EventManager.RemoveHandler(Event.GhostSpawned, OnPlayerBecomeGhost);
		EventManager.RemoveHandler(Event.PlayerRejoined, OnPlayerReturnedToBody);

		if (CustomNetworkManager.IsHeadless) return;
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}


	public void InitPossess(Mind mind)
	{
		if (mind.CurrentCharacterSettings != null)
		{
			characterSettings = mind.CurrentCharacterSettings;
		}
	}

	public void Init(Mind mind)
	{
		if (isServer)
		{
			if (mind.CurrentCharacterSettings != null)
			{
				SyncPlayerName(mind.name, mind.CurrentCharacterSettings.Name);
			}
			else
			{
				SyncPlayerName(mind.name, mind.name);
			}
		}


		if (hasAuthority)
		{
			EnableLighting(true);
			UIManager.ResetAllUI();
			GetComponent<MouseInputController>().enabled = true;

			if (UIManager.Instance.statsTab.window.activeInHierarchy == false)
			{
				UIManager.Instance.statsTab.window.SetActive(true);
			}

			if (PlayerType == PlayerTypes.Ghost)
			{
				if (PlayerList.Instance.IsClientAdmin)
				{
					UIManager.LinkUISlots(ItemStorageLinkOrigin.adminGhost);
				}

				// stop the crit notification and change overlay to ghost mode
				SoundManager.Stop("Critstate");
				OverlayCrits.Instance.SetState(OverlayState.death);
				// show ghosts
				var mask = Camera2DFollow.followControl.cam.cullingMask;
				mask |= 1 << LayerMask.NameToLayer("Ghosts");
				Camera2DFollow.followControl.cam.cullingMask = mask;
				UIManager.Display.RejoinedEvent();
				RequestAvailableGhostRolesMessage.SendMessage();
			}
			//Normal players
			else if (IsPlayerSemiGhost == false)
			{

				UIManager.LinkUISlots(ItemStorageLinkOrigin.localPlayer);
				// Hide ghosts
				var mask = Camera2DFollow.followControl.cam.cullingMask;
				mask &= ~(1 << LayerMask.NameToLayer("Ghosts"));
				Camera2DFollow.followControl.cam.cullingMask = mask;
				UIManager.Display.RejoinedEvent();
			}
			//Players like blob or Ai
			else
			{
				// stop the crit notification and change overlay to ghost mode
				SoundManager.Stop("Critstate");
				OverlayCrits.Instance.SetState(OverlayState.death);
				// hide ghosts
				var mask = Camera2DFollow.followControl.cam.cullingMask;
				mask &= ~(1 << LayerMask.NameToLayer("Ghosts"));
				Camera2DFollow.followControl.cam.cullingMask = mask;
				UIManager.Display.RejoinedEvent();
			}

			EventManager.Broadcast(Event.UpdateChatChannels);
			UpdateStatusTabUI();

			AmbientSoundArea.TriggerRefresh();

			waitTimeForRTTUpdate = 0f;

			isUpdateRTT = true;
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

	/// <summary>
	/// Sets the game object for where the player can receive and send chat message from
	/// </summary>
	/// <param name="newLocation"></param>
	[Server]
	public void SetPlayerChatLocation(GameObject newLocation)
	{
		playerChatLocation = newLocation;
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
			Loggy.LogWarning("Local Player can't find lighting system on Camera.main", Category.Lighting);
			return;
		}

		lighting.enabled = enable;
	}

	private void OnPlayerReturnedToBody()
	{
		Loggy.Log("Local player become Ghost", Category.Ghosts);
		EnableLighting(true);
	}

	private void OnPlayerBecomeGhost()
	{
		Loggy.Log("Local player returned to the body", Category.Ghosts);
		EnableLighting(false);
		OnBodyUnPossesedByPlayer?.Invoke();
	}

	public void SyncPlayerName(string oldValue, string value)
	{
		playerName = value;
		gameObject.name = value;
		RefreshVisibleName();
	}

	[RightClickMethod]
	public void Possess()
	{
		PlayerManager.LocalMindScript.SetPossessingObject(this.gameObject);
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

	public object Chat { get; internal set; }

	public bool IsGameObjectReachable(GameObject go, bool isServer, float interactDist = INTERACTION_DISTANCE,
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
		float interactDist = INTERACTION_DISTANCE, GameObject context = null)
	{
		return Validations.IsReachableByRegisterTiles(RegisterPlayer, otherObject, isServer, interactDist,
			context: context);
	}

	///     Checks if the player is within reach of something
	/// <param name="otherPosition">The position of whatever we are trying to reach</param>
	/// <param name="isServer">True if being executed on server, false otherwise</param>
	/// <param name="interactDist">Maximum distance of interaction between the player and other objects</param>
	/// <param name="context">If not null, will ignore collisions caused by this gameobject</param>
	public bool IsPositionReachable(Vector3 otherPosition, bool isServer, float interactDist = INTERACTION_DISTANCE,
		GameObject context = null)
	{
		return Validations.IsReachableByPositions(
			isServer ? RegisterPlayer.WorldPositionServer : RegisterPlayer.WorldPosition, otherPosition, isServer,
			interactDist, context: context);
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

		var CombinedRadioAccess = gameObject.GetComponent<CombinedRadioAccess>();
		if (CombinedRadioAccess != null)
		{
			transmitChannels |= CombinedRadioAccess.GetChannels();
		}


		var BodyPartRadioAccess = gameObject.GetComponent<BodyPartRadioAccess>(); //TODO interface?
		if (BodyPartRadioAccess != null)
		{
			transmitChannels |= BodyPartRadioAccess.AvailableChannels;
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

	public void OnInteract(TargetedInteraction interaction, Component interactable)
	{
		if (interaction == null) return;
		if (IsNormal == false) return;
		if (ComponentManager.TryGetUniversalObjectPhysics(interactable.gameObject, out var uop) == false) return;

		if ((this.gameObject.AssumedWorldPosServer() - uop.OfficialPosition ).magnitude >
		    PlayerScript.INTERACTION_DISTANCE_EXTENDED) //If telekinesis was used play effect I assume TODO test , also return maybe because you can't Put fingerprint on something far away
		{
			PlayEffect.SendToAll(interactable.gameObject, "TelekinesisEffect");
		}


		if (uop.attributes.HasComponent == false) return;

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
				if (slot != null)
				{
					details.AddDetail(new Detail
					{
						CausedByInstanceID = slot.ItemStorage.gameObject.GetInstanceID(),
						Description = $" A fingerprint ",
						DetailType = DetailType.Fingerprints
					});
				}
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

		if (Mind != null && Mind.IsAntag)
		{
			stringBuilder.Insert(0, "<color=yellow>");
			stringBuilder.AppendLine($"Antag: {Mind.GetAntag().Antagonist.AntagJobType}");
			stringBuilder.AppendLine($"Objectives : {Mind.GetAntag().GetObjectiveSummary()}</color>");
		}

		return stringBuilder.ToString();
	}

	//Used for Admins to VV function to toggle vent crawl as for some reason in build VV variable isnt working
	public void ToggleVentCrawl()
	{
		canVentCrawl = !canVentCrawl;
	}

	public void OnDestroy()
	{
		Itself.PreImplementedOnDestroy();
	}

	public void OnPossessPlayer(Mind mind, IPlayerPossessable parent)
	{
		if (mind == null) return;
		if (IsNormal && parent == null &&  playerTypeSettings.PlayerType != PlayerTypes.Ghost)//Can't be possessed directly
		{
			mind.SetPossessingObject(playerHealth.OrNull()?.brain.OrNull()?.gameObject);
			mind.StopGhosting();
			return;
		}
		else
		{
			InitPossess(mind);
		}
		OnBodyPossesedByPlayer?.Invoke();
	}

	public void OnControlPlayer(Mind mind)
	{
		if (mind == null) return;
		Init(mind);
	}

	public void SyncPossessingID(uint previouslyPossessing, uint currentlyPossessing)
	{
		possessingID = currentlyPossessing;
		Itself.PreImplementedSyncPossessingID(previouslyPossessing, currentlyPossessing);
	}

	#region FAITH

	[Server]
	public void JoinReligion(Faith newFaith)
	{
		LeaveReligion();
		currentFaith = newFaith;
		FaithName = currentFaith.FaithName;
		foreach (var prop in currentFaith.FaithProperties)
		{
			prop.OnJoinFaith(this);
		}
		if (FaithManager.Instance.CurrentFaith == newFaith) FaithManager.Instance.FaithMembers.Add(this);
	}

	[Server]
	public void JoinReligion(string newFaith)
	{
		JoinReligion(FaithManager.Instance.AllFaiths.Find(x => x.Faith.FaithName == newFaith).Faith);
	}

	[Server]
	public void LeaveReligion()
	{
		if (currentFaith == null) return;
		foreach (var prop in currentFaith.FaithProperties)
		{
			prop.OnLeaveFaith(this);
		}
		if (FaithManager.Instance.CurrentFaith == currentFaith) FaithManager.Instance.FaithMembers.Remove(this);
		currentFaith = null;
		FaithName = "None";
	}

	#endregion


	#region TOOLTIPDATA

	private string ToleranceCheckForReligion()
	{
		//This is client trickery, anything we want to check on the client itself is from PlayerManager
		//while things on the other player is done directly from within this class
		if(PlayerManager.LocalPlayerScript.currentFaith == null) return "";
		string finalText = "";
		if (FaithName == "None")
		{
			finalText = "This person does not appear to be a part of any faith.";
		}
		else
		{
			switch (PlayerManager.LocalPlayerScript.currentFaith.ToleranceToOtherFaiths)
			{
				case ToleranceToOtherFaiths.Accepting:
					finalText = "";
					break;
				case ToleranceToOtherFaiths.Neutral:
					if (PlayerManager.LocalPlayerScript.FaithName != FaithName)
					{
						finalText = $"This person appears to have faith in {FaithName}.";
					}
					else
					{
						finalText = $"<color=green>This person appears to share the same faith as me!</color>";
					}
					break;
				case ToleranceToOtherFaiths.Rejecting:
					if (PlayerManager.LocalPlayerScript.FaithName != FaithName)
					{
						finalText = $"<color=red>This person appears to have faith in {FaithName} which goes against what I believe.</color>";
					}
					else
					{
						finalText = $"<color=green>This person appears to share the same faith as me!</color>";
					}
					break;
				case ToleranceToOtherFaiths.Violent:
					if (PlayerManager.LocalPlayerScript.FaithName != FaithName)
					{
						finalText = $"<color=red>This person appears to not share the same beliefs as me, and I don't like that.</color>";
					}
					else
					{
						finalText = $"<color=green>This person appears to share the same faith as me!</color>";
					}
					break;
				default:
					finalText = "";
					break;
			}
		}
		return finalText;
	}

	public string HoverTip()
	{
		StringBuilder finalText = new StringBuilder();
		if (characterSettings == null) return finalText.ToString();
		finalText.Append($"A {characterSettings.Species}.");
		finalText.Append($" {characterSettings.TheyPronoun(this)}/{characterSettings.TheirPronoun(this)}.");
		finalText.AppendLine($"\n{ToleranceCheckForReligion()}");
		return finalText.ToString();
	}

	public string CustomTitle()
	{
		return visibleName;
	}

	public Sprite CustomIcon()
	{
		// (Max): I tried making the custom icon use the player's face but there is no way to properly grab their face sprites
		// Because the character customisation stuff does not have an methods to grab this data easily and when you do eventually grab it
		// by looping through all sprites in PlayerSprties; you just get an empty sprite. Also all bodyPart sprites don't have their bodyPartType enum
		// set for some odd reason so you can't just do an enum check and have to use regex for name detection (gameObject.name).
		// Do you see why I keep begging you, Bod, to look at this? Because character sprites are a mess to work with
		// and trying to create anything with it is near impossible and you're the only one who actually knows how to work with this.
		return null;
	}

	public List<Sprite> IconIndicators()
	{
		//TODO: add indicators for players.
		return null;
	}

	public List<TextColor> InteractionsStrings()
	{
		TextColor inspectText = new TextColor
		{
			Text = "Shift + Left Click: Inspect",
			Color = Color.white
		};

		List<TextColor> interactions = new List<TextColor>();
		interactions.Add(inspectText);
		return interactions;
	}

	#endregion

	public RightClickableResult GenerateRightClickOptions()
	{
		RightClickableResult result = new RightClickableResult();
		if (FaithName != "None" && PlayerManager.LocalPlayerScript.FaithName == "None")
		{
			result.AddElement("Join Faith", () => PlayerManager.LocalPlayerScript.JoinReligion(FaithName));
		}
		return result;
	}
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