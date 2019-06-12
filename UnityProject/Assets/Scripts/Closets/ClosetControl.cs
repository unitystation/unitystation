using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Allows closet to be opened / closed / locked
/// </summary>
[RequireComponent(typeof(RightClickAppearance))]
public class ClosetControl : NetworkBehaviour, IInteractable<HandApply>, IRightClickable
{
	[Header("Contents that will spawn inside every locker of type")]
	public List<GameObject> DefaultContents;

	//Inventory
	private IEnumerable<ObjectBehaviour> heldItems = new List<ObjectBehaviour>();
	protected List<ObjectBehaviour> heldPlayers = new List<ObjectBehaviour>();

	public bool IsClosed;
	[SyncVar(hook = nameof(SetIsLocked))] public bool IsLocked;
	public LockLightController lockLight;
	public int playerLimit = 3;

	private RegisterCloset registerTile;
	private PushPull pushPull;
	private Matrix matrix => registerTile.Matrix;
	private ObjectBehaviour objectBehaviour;

	[SyncVar(hook = nameof(SyncStatus))] public ClosetStatus statusSync;
	protected Sprite doorClosed;
	public Sprite doorOpened;
	public SpriteRenderer spriteRenderer;

	private void Awake()
	{
		doorClosed = spriteRenderer != null ? spriteRenderer.sprite : null;
		registerTile = GetComponent<RegisterCloset>();
		pushPull = GetComponent<PushPull>();
		objectBehaviour = GetComponent<ObjectBehaviour>();
	}

	public override void OnStartServer()
	{
		StartCoroutine(WaitForServerReg());
		base.OnStartServer();
		foreach (GameObject itemPrefab in DefaultContents)
		{
			PoolManager.PoolNetworkInstantiate(itemPrefab, transform.position, parent: transform.parent);
		}
	}

	private IEnumerator WaitForServerReg()
	{
		yield return WaitFor.Seconds(1f);
		SetIsClosed(true);
	}

	public override void OnStartLocalPlayer()
	{
		SyncSprite(statusSync);
		SetIsLocked(IsLocked);
		base.OnStartLocalPlayer();
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
		SoundManager.PlayNetworkedAtPos("OpenClose", registerTile.WorldPositionServer, 1f);
		if (IsClosed)
		{
			SetIsClosed(false);
		}
		else
		{
			SetIsClosed(true);
		}
	}

	private void SetIsClosed(bool value)
	{
		IsClosed = value;
		HandleItems();
		ChangeSprite();
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

	private void ChangeSprite()
	{
		if(IsClosed)
		{
			if(heldPlayers.Count > 0)
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

	public override bool CanUse(GameObject originator, string hand, Vector3 position, bool allowSoftCrit = false)
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

	public InteractionControl Interact(HandApply interaction)
	{
		return InteractInternal(interaction.UsedObject != null) ? InteractionControl.STOP_PROCESSING : InteractionControl.CONTINUE_PROCESSING;
	}

	private bool InteractInternal(bool placeItem = true)
	{
		//this better be rewritten to use IF2 Interactable: following code is executed on clientside
		PlayerScript localPlayer = PlayerManager.LocalPlayerScript;

		bool isInReach = localPlayer.IsInReach(registerTile, false);
		if (isInReach || localPlayer.IsHidden)
		{
			PlayerNetworkActions pna = originator.GetComponent<PlayerNetworkActions>();
			GameObject handObj = pna.Inventory[hand].Item;
			if (handObj != null)
			{
				pna.CmdPlaceItem(hand, position, null, false);
				return true;
			}
		}
		else
		{
			if(IsLocked)
			{
				return true;
			}
		}
		ToggleLocker();
		return true;
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
			item.parentContainer = null;
			if (pushPull && pushPull.Pushable.IsMovingServer)
			{
				netTransform.InertiaDrop(pos, pushPull.Pushable.SpeedServer,
					pushPull.InheritedImpulse.To2Int());
			}
			else
			{
				netTransform.AppearAtPositionServer(pos);
			}
			item.visibleState = true;
		}

		heldItems = Enumerable.Empty<ObjectBehaviour>();
	}

	private void CloseItemHandling()
	{
		heldItems = matrix.Get<ObjectBehaviour>(registerTile.PositionServer, ObjectType.Item, true);
		foreach (ObjectBehaviour item in heldItems)
		{
			CustomNetTransform netTransform = item.GetComponent<CustomNetTransform>();
			item.parentContainer = objectBehaviour;
			netTransform.DisappearFromWorldServer();
			item.visibleState = false;
		}
	}

	private void OpenPlayerHandling()
	{
		foreach (ObjectBehaviour player in heldPlayers)
		{
			var playerScript = player.GetComponent<PlayerScript>();
			var playerSync = playerScript.PlayerSync;

			playerSync.AppearAtPositionServer(registerTile.WorldPositionServer);
			player.parentContainer = null;
			if (pushPull && pushPull.Pushable.IsMovingServer)
			{
				playerScript.pushPull.TryPush(pushPull.InheritedImpulse.To2Int(),
					pushPull.Pushable.SpeedServer);
			}
			player.visibleState = true;
		}
		heldPlayers = new List<ObjectBehaviour>();
	}

	private void ClosePlayerHandling()
	{
		var mobsFound = matrix.Get<ObjectBehaviour>(registerTile.PositionServer, ObjectType.Player, true);
		int mobsIndex = 0;
		foreach (ObjectBehaviour player in mobsFound)
		{
			if(mobsIndex >= playerLimit)
			{
				return;
			}
			mobsIndex++;
			heldPlayers.Add(player);
			var playerScript = player.GetComponent<PlayerScript>();
			var playerSync = playerScript.PlayerSync;

			player.visibleState = false;
			player.parentContainer = objectBehaviour;
			playerSync.DisappearFromWorldServer();
			//Make sure a ClosetPlayerHandler is created on the client to monitor
			//the players input inside the storage. The handler also controls the camera follow targets:
			if (!playerScript.IsGhost)
			{
				ClosetHandlerMessage.Send(player.gameObject, gameObject);
			}
		}
	}

	/// <summary>
	/// Invoked when the parent net ID of this closet's RegisterCloset changes. Updates the parent net ID of the player / items
	/// in the closet, passing the update on to their RegisterTile behaviors.
	/// </summary>
	/// <param name="parentNetId">new parent net ID</param>
	public void OnParentChangeComplete(NetworkInstanceId parentNetId)
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
		//TODO: Code duplication (of validation logic) with Interact. Eliminate this duplication once this is refactored to IF2.
		var result = RightClickableResult.Create();
		PlayerScript localPlayer = PlayerManager.LocalPlayerScript;
		if (localPlayer.canNotInteract())
		{
			return result;
		}

		bool isInReach = localPlayer.IsInReach(registerTile, false);
		if (isInReach || localPlayer.IsHidden)
		{
			result.AddElement("OpenClose", RightClickInteract);
		}

		return result;
	}

	private void RightClickInteract()
	{
		Interact(PlayerManager.LocalPlayer,  registerTile.WorldPositionClient,
			UIManager.Hands.CurrentSlot.eventName);
	}
}