using System;
using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using UnityEngine;
using Mirror;
using UnityEngine.Serialization;
using Objects;

/// <summary>
/// Allows closet to be opened / closed / locked
/// </summary>
public class ClosetControl : NetworkBehaviour, ICheckedInteractable<HandApply>, IRightClickable,
	IServerLifecycle
{
	private static readonly StandardProgressActionConfig ProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.Escape);

	[Tooltip("Contents that will spawn inside every instance of this locker when the" +
			 " locker spawns.")]
	[SerializeField]
	public SpawnableList initialContents = null;

	[Tooltip("Lock light status indicator component")]
	[SerializeField]
	private LockLightController lockLight = null;

	[Tooltip("Whether the container can be locked.")]
	[SerializeField]
	public bool IsLockable = false;

	[Tooltip("Max amount of players that can fit in it at once.")]
	[SerializeField]
	private int playerLimit = 3;

	[Tooltip("Time to breakout or resist out of closet")]
	[SerializeField]
	private float breakoutTime = 120f;

	[Tooltip("Type of material to drop when destroyed")]
	public GameObject matsOnDestroy;

	[FormerlySerializedAs("metalDroppedOnDestroy")]
	[Tooltip("How much material to drop when destroyed")]
	[SerializeField]
	private int matsDroppedOnDestroy = 2;

	[FormerlySerializedAs("soundOnOpen")]
	[Tooltip("Name of sound to play when opened / closed")]
	[SerializeField]
	private AddressableAudioSource soundOnOpenOrClose = null;

	[Tooltip("Name of sound to play when emagged")]
	[SerializeField]
	private AddressableAudioSource soundOnEmag = null;

	[Tooltip("Name of sound to play when emagged")]
	[SerializeField]
	private AddressableAudioSource soundOnEscape = null;

	[Tooltip("Sprite to show when door is open.")]
	[SerializeField]
	private Sprite doorOpened = null;

	[Tooltip("Renderer for the whole locker")]
	[SerializeField]
	protected SpriteRenderer spriteRenderer;

	[Tooltip("GameObject for the lock overlay")]
	[SerializeField]
	protected GameObject lockOverlay = null;

	/// <summary>
	/// Invoked when locker becomes closed / open. Param is true
	/// if it's now closed, false if now open. Called client and server side.
	/// </summary>
	[NonSerialized]
	public readonly BoolEvent OnClosedChanged = new BoolEvent();

	/// <summary>
	/// Currently held items, only valid server side
	/// </summary>
	public List<ObjectBehaviour> ServerHeldItems => serverHeldItems;

	/// <summary>
	/// Currently held players, only valid server side
	/// </summary>
	public IEnumerable<ObjectBehaviour> ServerHeldPlayers => serverHeldPlayers;

	/// <summary>
	/// Whether locker is currently closed. Valid client / server side.
	/// </summary>
	public bool IsClosed => ClosetStatus != ClosetStatus.Open;

	[SyncVar(hook = nameof(SyncIsWelded))]
	[HideInInspector] private bool isWelded = false;
	/// <summary>
	/// Is door welded shut?
	/// </summary>
	public bool IsWelded => isWelded;

	/// <summary>
	/// Whether locker is currently locked. Valid client / server side.
	/// </summary>
	public bool IsLocked => isLocked;

	/// <summary>
	/// Whether locker is emagged.
	/// </summary>
	public bool isEmagged;

	[Tooltip("SpriteRenderer which is toggled when welded. Existence is equivalent to weldability of door.")]
	[SerializeField]
	protected SpriteRenderer weldOverlay = null;

	[SerializeField]
	private Sprite weldSprite = null;
	private static readonly float weldTime = 5.0f;

	private string closetName;
	private ObjectAttributes closetAttributes;

	/// <summary>
	/// Whether locker is weldable.
	/// </summary>
	public bool IsWeldable => (weldOverlay != null);

	/// <summary>
	/// Current status of the closet, valid client / server side.
	/// </summary>
	public ClosetStatus ClosetStatus => statusSync;

	private AccessRestrictions accessRestrictions;
	public AccessRestrictions AccessRestrictions
	{
		get
		{
			if (!accessRestrictions)
			{
				accessRestrictions = GetComponent<AccessRestrictions>();
			}
			return accessRestrictions;
		}
	}

	[SyncVar(hook = nameof(SyncStatus))]
	private ClosetStatus statusSync;

	[SyncVar(hook = nameof(SyncLocked))]
	private bool isLocked;

	//Inventory
	private List<ObjectBehaviour> serverHeldItems = new List<ObjectBehaviour>();
	private List<ObjectBehaviour> serverHeldPlayers = new List<ObjectBehaviour>();

	private RegisterCloset registerTile;
	private PushPull pushPull;
	//cached closed door sprite, initialized from whatever sprite the sprite renderer is initially set
	//to in the prefab
	private Sprite doorClosed;

	private Matrix Matrix => registerTile.Matrix;
	private PushPull PushPull
	{
		get
		{
			if (pushPull == null)
			{
				Logger.LogErrorFormat("Closet {0} has no PushPull component! All contained items will appear at HiddenPos!", Category.Transform, gameObject.ExpensiveName());
			}
			return pushPull;
		}
	}

	private void Awake()
	{
		EnsureInit();
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);

		//Fetch the items name to use in messages
		closetName = gameObject.ExpensiveName();
	}

	private void EnsureInit()
	{
		if (registerTile != null) return;
		doorClosed = spriteRenderer != null ? spriteRenderer.sprite : null;

		registerTile = GetComponent<RegisterCloset>();
		pushPull = GetComponent<PushPull>();
	}

	private void OnWillDestroyServer(DestructionInfo arg0)
	{
		// failsafe: drop all contents immediately
		ServerHandleContentsOnStatusChange(false);

		//force it open so it drops its contents
		SyncLocked(isLocked, false);
		SyncStatus(statusSync, ClosetStatus.Open);

		if (matsDroppedOnDestroy > 0)
		{
			Spawn.ServerPrefab(matsOnDestroy, gameObject.TileWorldPosition().To3Int(), transform.parent, count: matsDroppedOnDestroy,
				scatterRadius: Spawn.DefaultScatterRadius, cancelIfImpassable: true);
		}
	}

	public override void OnStartClient()
	{
		EnsureInit();

		SyncStatus(statusSync, statusSync);
		SyncLocked(isLocked, isLocked);
		SyncIsWelded(isWelded, isWelded);
	}

	public virtual void OnSpawnServer(SpawnInfo info)
	{
		if (initialContents != null)
		{
			//populate initial contents on spawn
			var result = initialContents.SpawnAt(SpawnDestination.At(gameObject));
			foreach (var spawned in result.GameObjects)
			{
				var objBehavior = spawned.GetComponent<ObjectBehaviour>();
				if (objBehavior != null)
				{
					ServerAddInternalItem(objBehavior);
				}
			}
		}

		//always spawn closed, all lockable closets locked
		SyncStatus(statusSync, ClosetStatus.Closed);
		if (IsLockable)
		{
			SyncLocked(isLocked, true);
		}
		else
		{
			SyncLocked(isLocked, false);
		}

		//if this is a mapped spawn, stick any items mapped on top of us in
		if (info.SpawnType == SpawnType.Mapped)
		{
			CloseItemHandling();
		}
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		//make sure we despawn what we are holding
		foreach (var heldItem in serverHeldItems)
		{
			Despawn.ServerSingle(heldItem.gameObject);
		}
		serverHeldItems.Clear();
	}

	/// <summary>
	/// Adds the indicated object inside this closet. No effect if
	/// closet is open.
	/// </summary>
	/// <param name="toAdd"></param>
	[Server]
	public void ServerAddInternalItem(ObjectBehaviour toAdd)
	{
		ServerAddInternalItemInternal(toAdd);
	}

	//internal because forcing is only to be used internally, because
	//we need to know what items are added to closet before it closes in
	//order to decide if player has been added.
	//Blame this mess on DNA scanner
	//TODO: FIx this mess, remove the need for a special OccupiedWithPlayer status, move that as a special
	//syncvar inside DNAScanner
	[Server]
	private void ServerAddInternalItemInternal(ObjectBehaviour toAdd, bool force = false)
	{
		if (toAdd == null || serverHeldItems.Contains(toAdd) || (!IsClosed && !force)) return;
		serverHeldItems.Add(toAdd);
		toAdd.parentContainer = pushPull;
		toAdd.VisibleState = false;
	}

	/// <summary>
	/// Is the closet empty?
	/// </summary>
	/// <returns></returns>
	[Server]
	public bool ServerIsEmpty()
	{
		return serverHeldItems.Count() + serverHeldPlayers.Count == 0;
	}

	/// <summary>
	/// Does this closet contain the indicated game object?
	/// </summary>
	/// <param name="gameObject"></param>
	/// <returns></returns>
	[Server]
	public bool ServerContains(GameObject gameObject)
	{
		if (!IsClosed)
		{
			return false;
		}
		foreach (var player in serverHeldPlayers)
		{
			if (player.gameObject == gameObject)
			{
				return true;
			}
		}
		foreach (var item in serverHeldItems)
		{
			if (item.gameObject == gameObject)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Toggle locker open / closed with sfx.
	/// </summary>
	/// <param name="nowClosed">specify open / closed. Leave null (default) to toggle based on
	/// current status.</param>
	[Server]
	public void ServerToggleClosed(bool? nowClosed = null)
	{
		SoundManager.PlayNetworkedAtPos(soundOnOpenOrClose, registerTile.WorldPositionServer, 1f, sourceObj: gameObject);
		ServerSetIsClosed(nowClosed.GetValueOrDefault(!IsClosed));
	}

	[Server]
	private void ServerSetIsClosed(bool nowClosed)
	{
		//need to call this before we actually updated the closed status o we can see if we will have any occupants
		//This is only needed because of the weird way DNA scanner was implemented, requiring us to
		//know if there are held players in order to SyncStatus(ClosedWithOccupant)
		//Blame this mess on DNA scanner
		//TODO: FIx this mess, remove the need for a special OccupiedWithPlayer status, move that as a special
		//syncvar inside DNAScanner
		ServerHandleContentsOnStatusChange(nowClosed);
		if (nowClosed)
		{
			if (serverHeldPlayers.Count > 0 && registerTile.closetType == ClosetType.SCANNER)
			{
				statusSync = ClosetStatus.ClosedWithOccupant;
			}
			else
			{
				statusSync = ClosetStatus.Closed;
			}
		}
		else
		{
			statusSync = ClosetStatus.Open;
		}
	}

	/// <summary>
	/// Break the closet lock.
	/// </summary>
	public void BreakLock()
	{
		//Disable the lock and hide its light
		if (IsLockable)
		{
			SyncLocked(isLocked, false);
			IsLockable = false;
			lockLight.Hide();
			Despawn.ClientSingle(lockOverlay);
		}
	}

	public void ServerTryWeld()
	{
		if (weldOverlay != null)
		{
			ServerWeld();
		}
	}

	public void ServerWeld()
	{
		if (this == null || gameObject == null) return; // probably destroyed by a shuttle crash

		SyncIsWelded(isWelded, !isWelded);

	}

	private void SyncIsWelded(bool _wasWelded, bool _isWelded)
	{
		isWelded = _isWelded;
		if (weldOverlay != null) // if closet is weldable
		{
			weldOverlay.sprite = isWelded ? weldSprite : null;
		}
	}

	private void SyncStatus(ClosetStatus oldValue, ClosetStatus value)
	{
		EnsureInit();
		statusSync = value;
		if (value == ClosetStatus.Open)
		{
			OnClosedChanged.Invoke(false);
		}
		else
		{
			OnClosedChanged.Invoke(true);
		}
		UpdateSpritesOnStatusChange();
	}

	/// <summary>
	/// Called when closet status changes. Update the closet sprites.
	/// </summary>
	protected virtual void UpdateSpritesOnStatusChange()
	{
		if (statusSync == ClosetStatus.Open)
		{
			spriteRenderer.sprite = doorOpened;
			if (lockLight && IsLockable)
			{
				lockLight.Hide();
			}
		}
		else
		{
			spriteRenderer.sprite = doorClosed;
			if (lockLight && IsLockable)
			{
				lockLight.Show();
			}
		}
	}

	/// <summary>
	/// Toggle locker open / closed with sfx.
	/// </summary>
	/// <param name="nowLocked">specify locked / unlocked. Leave null (default) to toggle based on
	/// current status.</param>
	[Server]
	public void ServerToggleLocked(bool? nowLocked = null)
	{
		isLocked = nowLocked.GetValueOrDefault(!IsLocked);
	}

	private void SyncLocked(bool oldValue, bool value)
	{
		//Set closet to locked or unlocked as well as update light graphic
		EnsureInit();
		isLocked = value;
		if (lockLight)
		{
			if (isLocked)
			{
				lockLight.Lock();
			}
			else
			{
				lockLight.Unlock();
			}
		}
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//only allow interactions targeting this closet
		if (interaction.TargetObject != gameObject) return false;

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		// Locking/Unlocking by alt clicking
		if (interaction.IsAltClick)
		{
			if(IsLockable && AccessRestrictions != null && ClosetStatus.Equals(ClosetStatus.Closed))
			{
				// Default CheckAccess will check for the ID slot first
				// so the default AltClick interaction will prioritize
				// the ID slot, only when that would fail the hand
				// will be checked, alternatively the user can also
				// just click the locker with the ID inhand.
				if (AccessRestrictions.CheckAccess(interaction.Performer))
				{

					if (isLocked)
					{
						SyncLocked(isLocked, false);
						Chat.AddExamineMsg(interaction.Performer, $"You unlock the {closetName}.");
					}
					else
					{
						SyncLocked(isLocked, true);
						Chat.AddExamineMsg(interaction.Performer, $"You lock the {closetName}.");
					}

				}
			}

			// Alt clicking is the locker's only alt click behaviour.
			return;
		}

		// Is the player trying to put something in the closet?
		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag)
			&& interaction.HandObject.TryGetComponent<Emag>(out var emag)
			&& emag.EmagHasCharges())
		{
			if (IsClosed && !isEmagged)
			{
				SoundManager.PlayNetworkedAtPos(soundOnEmag, registerTile.WorldPositionServer, 1f, gameObject);
				//ServerHandleContentsOnStatusChange(false);
				isEmagged = true;
				emag.UseCharge(interaction);

				//SyncStatus(statusSync, ClosetStatus.Open);
				BreakLock();
			}
		}
		else if (Validations.HasUsedActiveWelder(interaction))
		{
			// Is the player trying to weld closet?
			if (IsWeldable && interaction.Intent == Intent.Harm)
			{
				ToolUtils.ServerUseToolWithActionMessages(
						interaction, weldTime,
						$"You start {(IsWelded ? "unwelding" : "welding")} the {closetName} door...",
						$"{interaction.Performer.ExpensiveName()} starts {(IsWelded ? "unwelding" : "welding")} the {closetName} door...",
						$"You {(IsWelded ? "unweld" : "weld")} the {closetName} door.",
						$"{interaction.Performer.ExpensiveName()} {(IsWelded ? "unwelds" : "welds")} the {closetName} door.",
						ServerTryWeld);
			}
		}
		else if (interaction.HandObject != null)
		{
			// If nothing in the players hand can be used on the closet, drop it in the closet
			if (!IsClosed)
			{
				Vector3 targetPosition = interaction.TargetObject.WorldPosServer().RoundToInt();
				Vector3 performerPosition = interaction.Performer.WorldPosServer();
				Inventory.ServerDrop(interaction.HandSlot, targetPosition - performerPosition);
			}
		}
		else if (interaction.HandObject == null)
		{
			// player want to close locker?
			if (!isLocked && !isWelded)
			{
				ServerToggleClosed();
			}
			else if (isLocked || isWelded)
			{
				if (IsLockable)
				{ //This is to stop cant open msg even though you can
					if (!AccessRestrictions.CheckAccess(interaction.Performer))
					{
						Chat.AddExamineMsg(
						interaction.Performer,
						$"Can\'t open {closetName}");
					}
				}
				else
				{
					Chat.AddExamineMsg(
					interaction.Performer,
					$"Can\'t open {closetName}");
				}

			}
		}

		// player trying to unlock locker?
		if (IsLockable && AccessRestrictions != null && ClosetStatus.Equals(ClosetStatus.Closed))
		{
			// player trying to open lock by card?
			if (AccessRestrictions.CheckAccessCard(interaction.HandObject))
			{
				if (isLocked)
				{
					SyncLocked(isLocked, false);
					Chat.AddExamineMsg(interaction.Performer, $"You unlock the {closetName}.");
				}
				else
				{
					SyncLocked(isLocked, true);
					Chat.AddExamineMsg(interaction.Performer, $"You lock the {closetName}.");
				}
			}
			// player with access can unlock just by click
			else if (AccessRestrictions.CheckAccess(interaction.Performer))
			{
				if (isLocked)
				{
					SyncLocked(isLocked, false);
					Chat.AddExamineMsg(interaction.Performer, $"You unlock the {closetName}.");
				}
			}
		}
	}

	public void PlayerTryEscaping(GameObject player)
	{
		// First, try to just open the closet.
		if (!isLocked && !isWelded)
		{
			ServerToggleClosed();
		}
		else
		{
			GameObject target = this.gameObject;
			GameObject performer = player;

			void ProgressFinishAction()
			{
				//TODO: Add some sound here.
				ServerToggleClosed();
				BreakLock();

				//Remove the weld
				if (isWelded)
				{
					ServerTryWeld();
				}
				SoundManager.PlayNetworkedAtPos(soundOnEmag, registerTile.WorldPositionServer, 1f, sourceObj: gameObject);
				Chat.AddActionMsgToChat(performer, $"You successfully broke out of {target.ExpensiveName()}.",
					$"{performer.ExpensiveName()} successfully breaks out of {target.ExpensiveName()}.");
			}

			var bar = StandardProgressAction.Create(ProgressConfig, ProgressFinishAction)
				.ServerStartProgress(target.RegisterTile(), breakoutTime, performer);
			if (bar != null)
			{
				SoundManager.PlayNetworkedAtPos(soundOnEscape, registerTile.WorldPositionServer, 1f, sourceObj: gameObject);
				Chat.AddActionMsgToChat(performer,
					$"You begin breaking out of {target.ExpensiveName()}...",
					$"{performer.ExpensiveName()} begins breaking out of {target.ExpensiveName()}...");
			}
		}
	}

	[Server]
	protected virtual void ServerHandleContentsOnStatusChange(bool willClose)
	{
		if (willClose)
		{
			CloseItemHandling();
			ClosePlayerHandling();
		}
		else
		{
			OpenItemHandling();
			OpenPlayerHandling();
		}
	}

	/// <summary>
	/// Removes all items currently inside of the closet
	/// </summary>
	private void OpenItemHandling()
	{
		foreach (ObjectBehaviour item in serverHeldItems)
		{
			if (!item) continue;

			CustomNetTransform netTransform = item.GetComponent<CustomNetTransform>();
			//avoids blinking of premapped items when opening first time in another place:
			Vector3Int pos = registerTile.WorldPositionServer;
			netTransform.AppearAtPosition(pos);
			if (PushPull && PushPull.Pushable.IsMovingServer)
			{
				netTransform.InertiaDrop(pos, PushPull.Pushable.SpeedServer,
					PushPull.InheritedImpulse.To2Int());
			}
			else
			{
				item.VisibleState = true; //should act identical to line above
			}
			item.parentContainer = null;
		}
		serverHeldItems.Clear();
	}

	/// <summary>
	/// Adds all items currently sitting on this closet into the closet
	/// </summary>
	private void CloseItemHandling()
	{
		var itemsOnCloset = Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer, ObjectType.Item, true)
			.Where(ob => ob != null && ob.gameObject != gameObject)
			.Where(ob =>
			{
				return true;
			});

		foreach (var objectBehaviour in itemsOnCloset)
		{
			//force add, because we call clositemhandling before the
			//closet is actually updated as being closed.
			//Blame this mess on DNA scanner
			//TODO: Fix this mess, remove the need for a special OccupiedWithPlayer status, move that as a special
			//syncvar inside DNAScanner
			ServerAddInternalItemInternal(objectBehaviour, true);
		}
	}

	/// <summary>
	/// Removes all players currently inside of the closet
	/// </summary>
	private void OpenPlayerHandling()
	{
		foreach (ObjectBehaviour player in serverHeldPlayers)
		{
			player.VisibleState = true;
			player.GetComponent<PlayerMove>().IsTrapped = false;
			if (PushPull && PushPull.Pushable.IsMovingServer)
			{
				player.TryPush(PushPull.InheritedImpulse.To2Int(), PushPull.Pushable.SpeedServer);
			}
			player.parentContainer = null;
			//Stop tracking closet
			FollowCameraMessage.Send(player.gameObject, player.gameObject);
		}
		serverHeldPlayers = new List<ObjectBehaviour>();
	}

	/// <summary>
	/// Adds all players currently sitting on this closet into the closet
	/// </summary>
	private void ClosePlayerHandling()
	{
		var mobsFound = Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer, ObjectType.Player, true);
		int mobsIndex = 0;
		foreach (ObjectBehaviour player in mobsFound)
		{
			if (mobsIndex >= playerLimit)
			{
				return;
			}
			mobsIndex++;
			ServerStorePlayer(player);
		}
	}

	[Server]
	protected void ServerStorePlayer(ObjectBehaviour player)
	{
		serverHeldPlayers.Add(player);
		var playerScript = player.GetComponent<PlayerScript>();

		player.VisibleState = false;
		playerScript.playerMove.IsTrapped = true;
		player.parentContainer = PushPull;

		//Start tracking closet
		if (!playerScript.IsGhost)
		{
			FollowCameraMessage.Send(player.gameObject, gameObject);
		}
	}

	/// <summary>
	/// Invoked when the parent net ID of this closet's RegisterCloset changes. Updates the parent net ID of the player / items
	/// in the closet, passing the update on to their RegisterTile behaviors.
	/// </summary>
	/// <param name="parentNetId">new parent net ID</param>
	public void OnParentChangeComplete(uint parentNetId)
	{
		foreach (ObjectBehaviour objectBehaviour in serverHeldItems)
		{
			objectBehaviour.registerTile.ServerSetNetworkedMatrixNetID(parentNetId);
		}

		foreach (ObjectBehaviour objectBehaviour in serverHeldPlayers)
		{
			objectBehaviour.registerTile.ServerSetNetworkedMatrixNetID(parentNetId);
		}
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		var result = RightClickableResult.Create();

		if (WillInteract(HandApply.ByLocalPlayer(gameObject), NetworkSide.Client))
		{
			//TODO: Make this contexual if holding a welder or wrench
			var optionName = IsClosed ? "Open" : "Close";
			result.AddElement("OpenClose", RightClickInteract, nameOverride: optionName);
		}

		return result;
	}

	private void RightClickInteract()
	{
		InteractionUtils.RequestInteract(HandApply.ByLocalPlayer(gameObject), this);
	}
}

public enum ClosetStatus
{
	Closed,
	ClosedWithOccupant,
	Open
}
