using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Directional))]
[RequireComponent(typeof(SpriteMatrixRotation))]
[ExecuteInEditMode]
public class RegisterPlayer : RegisterTile
{
	/// <summary>
	/// True when the player is laying down. Gets the correct value
	/// based on whether this is called on client or server side
	/// </summary>
	public bool IsDown => isServer ? IsDownServer : IsDownClient;
	public bool IsDownClient { get; private set; }
	public bool IsDownServer { get; set; }

	public bool IsStunnedClient => false;

	/// <summary>
	/// True when the player is slipping
	/// </summary>
	public bool IsSlippingServer { get; private set; }

	private PlayerScript playerScript;
	private Directional playerDirectional;
	private SpriteMatrixRotation spriteMatrixRotation;

	/// <summary>
	/// Returns whether this player is blocking other players from occupying the space, using the
	/// correct server/client side logic based on where this is being called from.
	/// </summary>
	public bool IsBlocking => isServer ? IsBlockingServer : IsBlockingClient;
	public bool IsBlockingClient => !playerScript.IsGhost && !IsDownClient;
	public bool IsBlockingServer => !playerScript.IsGhost && !IsDownServer && !IsSlippingServer;
	private Coroutine unstunHandle;
	//cached spriteRenderers of this gameobject
	protected SpriteRenderer[] spriteRenderers;

	private void Awake()
	{
		playerScript = GetComponent<PlayerScript>();
		spriteMatrixRotation = GetComponent<SpriteMatrixRotation>();
		playerDirectional = GetComponent<Directional>();
		playerDirectional.ChangeDirectionWithMatrix = false;
		spriteMatrixRotation.spriteMatrixRotationBehavior = SpriteMatrixRotationBehavior.RemainUpright;
		spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
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
	/// Cause the player sprite to appear as laying down, which also causes them to become
	/// passable.
	///
	/// No effect if player is restrained.
	/// </summary>
	/// <param name="force">Allows forcing of the player to lay down without validation.
	///
	/// This is used so server can force client to lay the player down in the event
	/// that they are restrained but the syncvar Playermove.restrained has not yet finished being
	/// propagated.</param>
	public void LayDown(bool force=false)
	{
		if (!force)
		{
			if (playerScript.playerMove.IsBuckled)
			{
				return;
			}
		}

		if (!IsDownClient)
		{
			IsDownClient = true;
			spriteMatrixRotation.ExtraRotation = Quaternion.Euler(0, 0, -90);
			//Change sprite layer
			foreach (SpriteRenderer spriteRenderer in spriteRenderers)
			{
				spriteRenderer.sortingLayerName = "Bodies";
			}
			//lock current direction
			playerDirectional.LockDirection = true;
		}
	}

	/// <summary>
	/// Causes the player sprite to stand upright, causing them to become
	/// blocking again.
	/// </summary>
	public void GetUp()
	{
		if (IsDownClient)
		{
			IsDownClient = false;
			spriteMatrixRotation.ExtraRotation = Quaternion.identity;
			//back to original layer
			foreach (SpriteRenderer spriteRenderer in spriteRenderers)
			{
				spriteRenderer.sortingLayerName = "Players";
			}
			playerDirectional.LockDirection = false;

		}
	}
	/// <summary>
	/// Slips and stuns the player.
	/// </summary>
	/// <param name="slipWhileWalking">Enables slipping while walking.</param>
	public void Slip(bool slipWhileWalking = false)
	{
		// Don't slip while walking unless its enabled with "slipWhileWalking".
		// Don't slip while player's consious state is crit, soft crit, or dead.
		if ( IsSlippingServer
			|| !slipWhileWalking && playerScript.PlayerSync.SpeedServer <= playerScript.playerMove.WalkSpeed
		    || playerScript.playerHealth.IsCrit
		    || playerScript.playerHealth.IsSoftCrit
		    || playerScript.playerHealth.IsDead)
		{
			return;
		}

		IsSlippingServer = true;
		Stun();
		SoundManager.PlayNetworkedAtPos("Slip", WorldPositionServer, Random.Range(0.9f, 1.1f));
		// Let go of pulled items.
		playerScript.pushPull.CmdStopPulling();
	}

	/// <summary>
	/// Stops the player from moving and interacting for a period of time.
	/// Also drops held items by default.
	/// </summary>
	/// <param name="stunDuration">Time before the stun is removed.</param>
	/// <param name="dropItem">If items in the hand slots should be dropped on stun.</param>
	public void Stun(float stunDuration = 4f, bool dropItem = true)
	{
		PlayerUprightMessage.SendToAll(gameObject, false, false);
		if (dropItem)
		{
			playerScript.playerNetworkActions.DropItem(EquipSlot.leftHand);
			playerScript.playerNetworkActions.DropItem(EquipSlot.rightHand);
		}
		playerScript.playerMove.allowInput = false;

		this.RestartCoroutine(StunTimer(stunDuration), ref unstunHandle);
	}
	private IEnumerator StunTimer(float stunTime)
	{
		yield return WaitFor.Seconds(stunTime);
		RemoveStun();
	}

	public void RemoveStun()
	{
		PlayerUprightMessage.SendToAll(gameObject, true, false);
		playerScript.playerMove.allowInput = true;
	}
}