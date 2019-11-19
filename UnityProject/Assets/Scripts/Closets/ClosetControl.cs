using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using Enum = Google.Protobuf.WellKnownTypes.Enum;

/// <summary>
/// Allows closet to be opened / closed / locked
/// </summary>
[RequireComponent(typeof(RightClickAppearance))]
public class ClosetControl : NetworkBehaviour, ICheckedInteractable<HandApply> , IRightClickable,
	IServerSpawn

{
	[Tooltip("Contents that will spawn inside every instance of this locker when the" +
	         " locker spawns.")]
	[SerializeField]
	private SpawnableList initialContents;

	//Inventory
	private IEnumerable<ObjectBehaviour> heldItems = new List<ObjectBehaviour>();
	protected List<ObjectBehaviour> heldPlayers = new List<ObjectBehaviour>();

	public bool IsClosed;
	[SyncVar(hook = nameof(SetIsLocked))] public bool IsLocked;
	public LockLightController lockLight;
	public int playerLimit = 3;
	public int metalDroppedOnDestroy = 2;
	public string soundOnOpen = "OpenClose";

	private RegisterCloset registerTile;
	private PushPull pushPull;

	public PushPull PushPull
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
	private Matrix matrix => registerTile.Matrix;

	[SyncVar(hook = nameof(SyncStatus))] public ClosetStatus statusSync;

	protected Sprite doorClosed;
	public Sprite doorOpened;
	public SpriteRenderer spriteRenderer;

	private void Awake()
	{
		doorClosed = spriteRenderer != null ? spriteRenderer.sprite : null;
		registerTile = GetComponent<RegisterCloset>();
		pushPull = GetComponent<PushPull>();
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
	}

	private void OnWillDestroyServer(DestructionInfo arg0)
	{
		//force it open
		SetIsLocked(false);
		SetIsClosed(false);

		if (metalDroppedOnDestroy > 0)
		{
			Spawn.ServerPrefab("Metal", gameObject.TileWorldPosition().To3Int(), transform.parent, count: metalDroppedOnDestroy,
				scatterRadius: Spawn.DefaultScatterRadius, cancelIfImpassable: true);
		}
	}

	public override void OnStartServer()
	{
		StartCoroutine(WaitForServerReg());
	}

	private IEnumerator WaitForServerReg()
	{
		yield return WaitFor.Seconds(1f);
		SetIsClosed(registerTile.IsClosed);
	}

	public override void OnStartClient()
	{
		SyncStatus( statusSync );
		SetIsLocked(IsLocked);
	}

	public void OnSpawnServer(SpawnInfo info)
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
					AddItem(objBehavior);
				}
			}
		}
	}

	public bool Contains(GameObject gameObject)
	{
		if (!IsClosed)
		{
			return false;
		}
		foreach (var player in heldPlayers)
		{
			if (player.gameObject == gameObject)
			{
				return true;
			}
		}
		foreach (var item in heldItems)
		{
			if (item.gameObject == gameObject)
			{
				return true;
			}
		}
		return false;
	}

	public void ToggleLocker()
	{
		if (IsClosed)
		{
			ToggleLocker(false);
		}
		else
		{
			ToggleLocker(true);
		}
	}

	public void ToggleLocker(bool isClosed)
	{
		SoundManager.PlayNetworkedAtPos(soundOnOpen, registerTile.WorldPositionServer, 1f);
		SetIsClosed(isClosed);
	}

	private void SetIsClosed(bool value)
	{
		IsClosed = value;
		HandleItems();
		StartCoroutine(ChangeSpriteDelayed());
	}

	private void SetIsLocked(bool value)
	{
		IsLocked = value;
		if (lockLight)
		{
			if(IsLocked)
			{
				lockLight.Lock();
			}
			else
			{
				lockLight.Unlock();
			}
		}
	}

	public IEnumerator ChangeSpriteDelayed()
	{
		yield return WaitFor.EndOfFrame;
		ChangeSprite();
	}

	public void ChangeSprite()
	{
		if(IsClosed)
		{
			if(heldPlayers.Count > 0 && registerTile.closetType == ClosetType.SCANNER)
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

	public enum ClosetStatus
	{
		Closed,
		ClosedWithOccupant,
		Open
	}

	private void SyncStatus(ClosetStatus value)
	{
		statusSync = value;
		if(value == ClosetStatus.Open)
		{
			registerTile.IsClosed = false;
		}
		else
		{
			registerTile.IsClosed = true;
		}
		SyncSprite(value);
	}

	public virtual void SyncSprite(ClosetStatus value)
	{
		if (value == ClosetStatus.Open)
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

	public bool CanUse(GameObject originator, string hand, Vector3 position, bool allowSoftCrit = false)
	{
		var playerScript = originator.GetComponent<PlayerScript>();

		if (playerScript.canNotInteract() && (!playerScript.playerHealth.IsSoftCrit || !allowSoftCrit))
		{
			return false;
		}

		if (!playerScript.IsInReach(position, false))
		{
			if(isServer && !Contains(originator))
			{
				return false;
			}
		}

		return true;
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
		// Is the player trying to put something in the closet
		if (interaction.HandObject != null && !IsClosed)
		{
			Vector3 targetPosition = interaction.TargetObject.WorldPosServer().RoundToInt();
			Inventory.ServerDrop(interaction.HandSlot, targetPosition);
		}
		else if (!IsLocked)
		{
			ToggleLocker();
		}
	}

	public virtual void HandleItems()
	{
		if (IsClosed)
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

	private void OpenItemHandling()
	{
		foreach (ObjectBehaviour item in heldItems)
		{
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

		heldItems = Enumerable.Empty<ObjectBehaviour>();
	}

	public void AddItem(ObjectBehaviour toAdd)
	{
		if (toAdd == null) return;
		heldItems = heldItems.Concat(new [] {toAdd});
		toAdd.parentContainer = pushPull;
		toAdd.VisibleState = false;
	}

	private void CloseItemHandling()
	{
		var itemsOnCloset = matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer, ObjectType.Item, true);
		if (heldItems != null)
		{
			heldItems = heldItems.Concat(itemsOnCloset);
		}
		else
		{
			heldItems = itemsOnCloset;
		}
		foreach (ObjectBehaviour item in heldItems)
		{
			item.parentContainer = PushPull;
			item.VisibleState = false;
		}
	}


	private void OpenPlayerHandling()
	{
		foreach (ObjectBehaviour player in heldPlayers)
		{
			player.VisibleState = true;
			if (PushPull && PushPull.Pushable.IsMovingServer)
			{
				player.TryPush(PushPull.InheritedImpulse.To2Int(),PushPull.Pushable.SpeedServer);
			}
			player.parentContainer = null;

			//Stop tracking closet
			FollowCameraMessage.Send(player.gameObject, null);
		}
		heldPlayers = new List<ObjectBehaviour>();
	}

	private void ClosePlayerHandling()
	{
		var mobsFound = matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer, ObjectType.Player, true);
		int mobsIndex = 0;
		foreach (ObjectBehaviour player in mobsFound)
		{
			if(mobsIndex >= playerLimit)
			{
				return;
			}
			mobsIndex++;
			StorePlayer(player);
		}
	}

	public void StorePlayer(ObjectBehaviour player)
	{
		heldPlayers.Add(player);
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
		foreach (ObjectBehaviour objectBehaviour in heldItems)
		{
			objectBehaviour.registerTile.ParentNetId = parentNetId;
		}

		foreach (ObjectBehaviour objectBehaviour in heldPlayers)
		{
			objectBehaviour.registerTile.ParentNetId = parentNetId;
		}
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		var result = RightClickableResult.Create();

		if (WillInteract(HandApply.ByLocalPlayer(gameObject), NetworkSide.Client))
		{
			result.AddElement("OpenClose", RightClickInteract);
		}


		return result;
	}

	private void RightClickInteract()
	{
		InteractionUtils.RequestInteract(HandApply.ByLocalPlayer(gameObject), this);
	}

	public bool IsEmpty()
	{
		return heldItems.Count() + heldPlayers.Count == 0;
	}


}