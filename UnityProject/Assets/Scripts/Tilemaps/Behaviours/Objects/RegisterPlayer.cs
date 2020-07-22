using System;
using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Directional))]
[RequireComponent(typeof(UprightSprites))]
[ExecuteInEditMode]
public class RegisterPlayer : RegisterTile, IServerSpawn
{

	// tracks whether player is down or upright.
	[SyncVar(hook = nameof(SyncIsLayingDown))]
	private bool isLayingDown;

	/// <summary>
	/// True when the player is laying down for any reason (shown sideways)
	/// </summary>
	public bool IsLayingDown => isLayingDown;

	/// <summary>
	/// True when the player is slipping
	/// </summary>
	public bool IsSlippingServer { get; private set; }

	/// <summary>
	/// Invoked on server when slip state is change. Provides old and new value as 1st and 2nd args
	/// </summary>
	[NonSerialized]
	public SlipEvent OnSlipChangeServer = new SlipEvent();

	private PlayerScript playerScript;
	public PlayerScript PlayerScript => playerScript;
	private Directional playerDirectional;
	private UprightSprites uprightSprites;

	/// <summary>
	/// Returns whether this player is blocking other players from occupying the space, using the
	/// correct server/client side logic based on where this is being called from.
	/// </summary>
	public bool IsBlocking => isServer ? IsBlockingServer : IsBlockingClient;
	public bool IsBlockingClient => !playerScript.IsGhost && !IsLayingDown;
	public bool IsBlockingServer => !playerScript.IsGhost && !IsLayingDown && !IsSlippingServer;
	private Coroutine unstunHandle;
	//cached spriteRenderers of this gameobject
	protected SpriteRenderer[] spriteRenderers;

	private void Awake()
	{
		base.Awake();
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (playerScript != null) return;
		playerScript = GetComponent<PlayerScript>();
		uprightSprites = GetComponent<UprightSprites>();
		playerDirectional = GetComponent<Directional>();
		playerDirectional.ChangeDirectionWithMatrix = false;
		uprightSprites.spriteMatrixRotationBehavior = SpriteMatrixRotationBehavior.RemainUpright;
		spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		EnsureInit();
		SyncIsLayingDown(isLayingDown, isLayingDown);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		SyncIsLayingDown(isLayingDown, false);
	}

	public override bool IsPassable(bool isServer)
	{
		return isServer ? !IsBlockingServer : !IsBlockingClient;
	}

	public override bool IsPassable(Vector3Int from, bool isServer)
	{
		return IsPassable(isServer);
	}

	/// <summary>
	/// Make the player appear laying down
	/// When down, they become passable.
	/// </summary>
	[Server]
	public void ServerLayDown()
	{
		SyncIsLayingDown(isLayingDown, true);
	}

	/// <summary>
	/// Make the player appear standing up.
	/// When up, they become impassable.
	/// </summary>
	[Server]
	public void ServerStandUp()
	{
		SyncIsLayingDown(isLayingDown, false);
	}

	/// <summary>
	/// Make the player appear laying down or standing up. When up, they become impassable
	/// </summary>
	/// <param name="isStanding"></param>
	[Server]
	public void ServerSetIsStanding(bool isStanding)
	{
		SyncIsLayingDown(isLayingDown, !isStanding);
	}

	private void SyncIsLayingDown(bool wasDown, bool isDown)
	{
		EnsureInit();
		this.isLayingDown = isDown;
		if (isDown)
		{
			uprightSprites.ExtraRotation = Quaternion.Euler(0, 0, -90);
			//Change sprite layer
			foreach (SpriteRenderer spriteRenderer in spriteRenderers)
			{
				spriteRenderer.sortingLayerName = "Bodies";
			}

			//lock current direction
			playerDirectional.LockDirection = true;
		}
		else
		{
			uprightSprites.ExtraRotation = Quaternion.identity;
			//back to original layer
			foreach (SpriteRenderer spriteRenderer in spriteRenderers)
			{
				if (playerScript.IsGhost)
					spriteRenderer.sortingLayerName = "Ghosts";
				else
					spriteRenderer.sortingLayerName = "Players";
			}
			playerDirectional.LockDirection = false;
		}
	}

	/// <summary>
	/// Slips and stuns the player.
	/// </summary>
	/// <param name="slipWhileWalking">Enables slipping while walking.</param>
	[Server]
	public void ServerSlip(bool slipWhileWalking = false)
	{
		if (this == null)
		{
			return;
		}
		// Don't slip while walking unless its enabled with "slipWhileWalking".
		// Don't slip while player's consious state is crit, soft crit, or dead.
		// Don't slip while the players hunger state is Strarving
		if (IsSlippingServer
			|| !slipWhileWalking && playerScript.PlayerSync.SpeedServer <= playerScript.playerMove.WalkSpeed
			|| playerScript.playerHealth.IsCrit
			|| playerScript.playerHealth.IsSoftCrit
			|| playerScript.playerHealth.IsDead
			|| playerScript.playerHealth.Metabolism.HungerState == HungerState.Starving)
		{
			return;
		}

		ServerStun();
		SoundManager.PlayNetworkedAtPos("Slip", WorldPositionServer, Random.Range(0.9f, 1.1f), sourceObj: gameObject);
		// Let go of pulled items.
		playerScript.pushPull.ServerStopPulling();
	}

	/// <summary>
	/// Stops the player from moving and interacting for a period of time.
	/// Also drops held items by default.
	/// </summary>
	/// <param name="stunDuration">Time before the stun is removed.</param>
	/// <param name="dropItem">If items in the hand slots should be dropped on stun.</param>
	[Server]
	public void ServerStun(float stunDuration = 4f, bool dropItem = true)
	{
		var oldVal = IsSlippingServer;
		IsSlippingServer = true;
		SyncIsLayingDown(isLayingDown, true);
		OnSlipChangeServer.Invoke(oldVal, IsSlippingServer);
		if (dropItem)
		{
			Inventory.ServerDrop(playerScript.ItemStorage.GetNamedItemSlot(NamedSlot.leftHand));
			Inventory.ServerDrop(playerScript.ItemStorage.GetNamedItemSlot(NamedSlot.rightHand));
		}
		playerScript.playerMove.allowInput = false;

		this.RestartCoroutine(StunTimer(stunDuration), ref unstunHandle);
	}
	private IEnumerator StunTimer(float stunTime)
	{
		yield return WaitFor.Seconds(stunTime);
		ServerRemoveStun();
	}

	private void ServerRemoveStun()
	{
		var oldVal = IsSlippingServer;
		IsSlippingServer = false;

		// Do not raise up a dead body
		if (playerScript.playerHealth.ConsciousState == ConsciousState.CONSCIOUS)
		{
			SyncIsLayingDown(isLayingDown, false);
		}

		OnSlipChangeServer.Invoke(oldVal, IsSlippingServer);

		if (playerScript.playerHealth.ConsciousState == ConsciousState.CONSCIOUS
			 || playerScript.playerHealth.ConsciousState == ConsciousState.BARELY_CONSCIOUS)
		{
			playerScript.playerMove.allowInput = true;
		}
	}
}

/// <summary>
/// Fired when slip state changes. Provides old and new value.
/// </summary>
public class SlipEvent : UnityEvent<bool, bool>
{
}
