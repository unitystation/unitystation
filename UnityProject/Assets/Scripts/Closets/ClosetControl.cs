using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using UnityEngine.Serialization;

/// <summary>
/// Allows closet to be opened / closed / locked
/// </summary>
[RequireComponent(typeof(RightClickAppearance))]
public class ClosetControl : NetworkBehaviour, ICheckedInteractable<HandApply> , IRightClickable,
	IServerLifecycle

{
	[Tooltip("Contents that will spawn inside every instance of this locker when the" +
	         " locker spawns.")]
	[SerializeField]
	private SpawnableList initialContents = null;

	[Tooltip("Lock light status indicator component")]
	[SerializeField]
	private LockLightController lockLight = null;

	[Tooltip("Whether the container can be locked.")]
	[SerializeField]
	private bool IsLockable = false;

	[Tooltip("Max amount of players that can fit in it at once.")]
	[SerializeField]
	private int playerLimit = 3;

	[Tooltip("Type of material to drop when destroyed")]
	public GameObject matsOnDestroy;

	[FormerlySerializedAs("metalDroppedOnDestroy")]
	[Tooltip("How much material to drop when destroyed")]
	[SerializeField]
	private int matsDroppedOnDestroy = 2;

	[FormerlySerializedAs("soundOnOpen")]
	[Tooltip("Name of sound to play when opened / closed")]
	[SerializeField]
	private string soundOnOpenOrClose = "OpenClose";

	[Tooltip("Name of sound to play when emagged")]
	[SerializeField]
	private string soundOnEmag = "grillehit";

	[Tooltip("Sprite to show when door is open.")]
	[SerializeField]
	private Sprite doorOpened = null;

	[Tooltip("Renderer for the whole locker")]
	[SerializeField]
	protected SpriteRenderer spriteRenderer;

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

	/// <summary>
	/// Whether locker is currently locked. Valid client / server side.
	/// </summary>
	public bool IsLocked => isLocked;

	/// <summary>
	/// Whether locker is emagged.
	/// </summary>
	public bool isEmagged;

	/// <summary>
	/// Current status of the closet, valid client / server side.
	/// </summary>
	public ClosetStatus ClosetStatus => statusSync;

	private AccessRestrictions accessRestrictions;
	public AccessRestrictions AccessRestrictions {
		get {
			if ( !accessRestrictions ) {
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
			if ( pushPull == null )
			{
				Logger.LogErrorFormat( "Closet {0} has no PushPull component! All contained items will appear at HiddenPos!", Category.Transform, gameObject.ExpensiveName() );
			}

			return pushPull;
		}
	}


	private void Awake()
	{
		EnsureInit();
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
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
		if(IsLockable)
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
		if(nowClosed)
		{
			if(serverHeldPlayers.Count > 0 && registerTile.closetType == ClosetType.SCANNER)
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

	private void SyncStatus(ClosetStatus oldValue, ClosetStatus value)
	{
		EnsureInit();
		statusSync = value;
		if(value == ClosetStatus.Open)
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
			if (lockLight)
			{
				lockLight.Hide();
			}
		}
		else
		{
			spriteRenderer.sprite = doorClosed;
			if (lockLight)
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
		EnsureInit();
		isLocked = value;
		if (lockLight)
		{
			if(isLocked)
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
		// Is the player trying to put something in the closet?
		if (interaction.HandObject != null)
		{
			if (!IsClosed)
			{
				Vector3 targetPosition = interaction.TargetObject.WorldPosServer().RoundToInt();
				Vector3 performerPosition = interaction.Performer.WorldPosServer();
				Inventory.ServerDrop(interaction.HandSlot, targetPosition - performerPosition);
			}
			else if (IsClosed && !isEmagged && Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag))
			{
				SoundManager.PlayNetworkedAtPos(soundOnEmag, registerTile.WorldPositionServer, 1f, sourceObj: gameObject);
				ServerHandleContentsOnStatusChange(false);
				isEmagged = true;
				SyncLocked(isLocked, false);
				SyncStatus(statusSync, ClosetStatus.Open);
			}
		}
		else
		{
			// player want to close locker?
			if (!isLocked && !isEmagged)
			{
				ServerToggleClosed();
			}
		}


		// player trying to unlock locker?
		if (IsLockable && AccessRestrictions != null)
		{
			// player trying to open lock by card?
			if (AccessRestrictions.CheckAccessCard(interaction.HandObject))
			{
				if (isLocked)
				{
					SyncLocked(isLocked, false);
				}
				else
				{
					SyncLocked(isLocked, true);
				}
			}
			// player with access can unlock just by click
			else if (AccessRestrictions.CheckAccess(interaction.Performer))
			{
				if (isLocked)
				{
					SyncLocked(isLocked, false);
				}
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
//				netTransform.AppearAtPositionServer(pos);
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
			//TODO: FIx this mess, remove the need for a special OccupiedWithPlayer status, move that as a special
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
			if (PushPull && PushPull.Pushable.IsMovingServer)
			{
				player.TryPush(PushPull.InheritedImpulse.To2Int(),PushPull.Pushable.SpeedServer);
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
			if(mobsIndex >= playerLimit)
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
