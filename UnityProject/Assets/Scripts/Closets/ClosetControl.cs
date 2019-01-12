using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class ClosetControl : InputTrigger
{
	private Sprite doorClosed;
	public Sprite doorOpened;

	//Inventory
	private List<ObjectBehaviour> heldItems = new List<ObjectBehaviour>();

	private List<ObjectBehaviour> heldPlayers = new List<ObjectBehaviour>();

	[SyncVar(hook = nameof(OpenClose))] public bool IsClosed;

	[SyncVar(hook = nameof(LockUnlock))] public bool IsLocked;
	public GameObject items;
	public LockLightController lockLight;

	private RegisterCloset registerTile;
	private PushPull pushPull;
	private Matrix matrix => registerTile.Matrix;

	public SpriteRenderer spriteRenderer;

	private void Awake()
	{
		doorClosed = spriteRenderer.sprite;
	}

	private void Start()
	{
		registerTile = GetComponent<RegisterCloset>();
		pushPull = GetComponent<PushPull>();
	}

	public override void OnStartServer()
	{
		StartCoroutine(WaitForServerReg());
		base.OnStartServer();
	}

	private IEnumerator WaitForServerReg()
	{
		yield return new WaitForSeconds(1f);
		IsClosed = true;
		SetItems(!IsClosed);
	}

	public override void OnStartClient()
	{
		StartCoroutine(WaitForLoad());
		base.OnStartClient();
	}

	/// SERVERSIDE -- Does this closet contain this object? (if it's closed)
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

	private IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(3f);
		bool iC = IsClosed;
		bool iL = IsLocked;
		OpenClose(iC);
		LockUnlock(iL);
	}

	[Server]
	public void ServerToggleCupboard()
	{
		if (IsClosed)
		{
			if (lockLight != null)
			{
				if (lockLight.IsLocked())
				{
					IsLocked = false;
					return;
				}
				IsClosed = false;
				SetItems(true);
			}
			else
			{
				IsClosed = false;
				SetItems(true);
			}
		}
		else
		{
			IsClosed = true;
			SetItems(false);
		}
	}

	private void OpenClose(bool isClosed)
	{
		IsClosed = isClosed;
		if (isClosed)
		{
			Close();
		}
		else
		{
			Open();
		}
	}

	private void LockUnlock(bool lockIt)
	{
		IsLocked = lockIt;
		if (lockLight == null)
		{
			return;
		}
		if (lockIt)
		{
		}
		else
		{
			lockLight.Unlock();
		}
	}

	private void Close()
	{
		registerTile.IsClosed = true;
		SoundManager.PlayAtPosition("OpenClose", transform.position);
		spriteRenderer.sprite = doorClosed;
		if (lockLight != null)
		{
			lockLight.Show();
		}
	}

	private void Open()
	{
		registerTile.IsClosed = false;
		SoundManager.PlayAtPosition("OpenClose", transform.position);
		spriteRenderer.sprite = doorOpened;
		if (lockLight != null)
		{
			lockLight.Hide();
		}
	}
	[ContextMethod("Open/close", "hand")]
	public void GUIInteract()
	{
		//don't put your hand contents on open/close rmb action!
		InteractInternal(false);
	}
	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		return InteractInternal();
	}

	private bool InteractInternal(bool placeItem = true)
	{
		//this better be rewritten to net messages: following code is executed on clientside
		PlayerScript localPlayer = PlayerManager.LocalPlayerScript;
		if (localPlayer.canNotInteract())
		{
			return true;
		}

		bool isInReach = localPlayer.IsInReach(registerTile);
		if (isInReach || localPlayer.IsHidden)
		{
			if (IsClosed)
			{
				localPlayer.playerNetworkActions.CmdToggleCupboard(gameObject);
				return true;
			}

			GameObject item = UIManager.Hands.CurrentSlot.Item;
			if (placeItem && item != null && isInReach)
			{
				localPlayer.playerNetworkActions.CmdPlaceItem(
					UIManager.Hands.CurrentSlot.eventName, transform.position, null, false);

				item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
			}
			else
			{
				localPlayer.playerNetworkActions.CmdToggleCupboard(gameObject);
			}
			return true;
		}
		return true;
	}

	private void SetItems(bool open)
	{
		if (!open)
		{
			SetItemsAliveState(false);
			SetPlayersAliveState(false);
		}
		else
		{
			SetItemsAliveState(true);
			SetPlayersAliveState(true);
		}
	}

	private void SetItemsAliveState(bool on)
	{
		if (!on)
		{
			heldItems = matrix.Get<ObjectBehaviour>(registerTile.Position, ObjectType.Item);
		}

		for (var i = 0; i < heldItems.Count; i++)
		{
			ObjectBehaviour item = heldItems[i];
			CustomNetTransform netTransform = item.GetComponent<CustomNetTransform>();
			if (@on)
			{
				//avoids blinking of premapped items when opening first time in another place:
				Vector3Int pos = registerTile.WorldPosition;
				netTransform.AppearAtPosition(pos);
				if (pushPull && pushPull.Pushable.IsMovingServer)
				{
					netTransform.InertiaDrop(pos, pushPull.Pushable.MoveSpeedServer, pushPull.InheritedImpulse.To2Int());
				}
				else
				{
					netTransform.AppearAtPositionServer(pos);
				}
			}
			else
			{
				netTransform.DisappearFromWorldServer();
			}

			item.visibleState = @on;
		}
	}

	private void SetPlayersAliveState(bool on)
	{
		if (!on)
		{
			heldPlayers = matrix.Get<ObjectBehaviour>(registerTile.Position, ObjectType.Player);
		}

		for (var i = 0; i < heldPlayers.Count; i++)
		{
			ObjectBehaviour player = heldPlayers[i];
			var playerScript = player.GetComponent<PlayerScript>();
			var playerSync = playerScript.PlayerSync;
			if (@on)
			{
				playerSync.AppearAtPositionServer(registerTile.WorldPosition);
				if (pushPull && pushPull.Pushable.IsMovingServer)
				{
					playerScript.pushPull.TryPush(pushPull.InheritedImpulse.To2Int(), pushPull.Pushable.MoveSpeedServer);
				}
			}

			player.visibleState = @on;

			if (!@on)
			{
				playerSync.DisappearFromWorldServer();
				//Make sure a ClosetPlayerHandler is created on the client to monitor
				//the players input inside the storage. The handler also controls the camera follow targets:
				if (!playerScript.playerMove.isGhost)
				{
					ClosetHandlerMessage.Send(player.gameObject, gameObject);
				}
			}
		}
	}
}
