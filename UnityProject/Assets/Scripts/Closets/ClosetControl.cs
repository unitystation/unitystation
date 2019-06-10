using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(RightClickAppearance))]
public class ClosetControl : InputTrigger
{
	[Header("Contents that will spawn inside every locker of type")]
	public List<GameObject> DefaultContents;

	//Inventory
	private IEnumerable<ObjectBehaviour> heldItems = new List<ObjectBehaviour>();
	protected List<ObjectBehaviour> heldPlayers = new List<ObjectBehaviour>();

	[SyncVar(hook = nameof(SyncIsClosed))] public bool IsClosed;
	[SyncVar(hook = nameof(LockUnlock))] public bool IsLocked;
	public LockLightController lockLight;
	public int playerLimit = 3;

	private RegisterCloset registerTile;
	private PushPull pushPull;
	private Matrix matrix => registerTile.Matrix;
	private ObjectBehaviour objectBehaviour;

	private Sprite doorClosed;
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
		IsClosed = true;
		HandleItems();
	}

	public override void OnStartLocalPlayer()
	{
		SyncIsClosed(IsClosed);
		LockUnlock(IsLocked);
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
			if (lockLight != null && lockLight.IsLocked())
			{
				IsLocked = false;
				return;
			}
			IsClosed = false;
			HandleItems();
		}
		else
		{
			IsClosed = true;
			HandleItems();
		}
	}

	private void SyncIsClosed(bool value)
	{
		IsClosed = value;
		ChangeSprite();
	}

	private void LockUnlock(bool value)
	{
		IsLocked = value;
		if (lockLight == null && !IsLocked)
		{
			lockLight.Unlock();
		}
	}

	public virtual void ChangeSprite()
	{
		registerTile.IsClosed = IsClosed;
		if(IsClosed)
		{
			spriteRenderer.sprite = doorClosed;
			if (lockLight != null)
			{
				lockLight.Show();
			}
		}
		else
		{
			spriteRenderer.sprite = doorOpened;
			if (lockLight != null)
			{
				lockLight.Hide();
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

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!CanUse(originator, hand, position, false))
		{
			return false;
		}
		if (!isServer)
		{
			//ask server to perform the interaction
			InteractMessage.Send(gameObject, position, hand);
			return true;
		}
		if (!IsClosed)
		{
			PlayerNetworkActions pna = originator.GetComponent<PlayerNetworkActions>();
			GameObject handObj = pna.Inventory[hand].Item;
			if (handObj != null)
			{
				pna.CmdPlaceItem(hand, position, null, false);
				return true;
			}
		}
		ToggleLocker();
		return true;
	}

	private void HandleItems()
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
			mobsIndex++;
			if(mobsIndex >= playerLimit)
			{
				return;
			}
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
}