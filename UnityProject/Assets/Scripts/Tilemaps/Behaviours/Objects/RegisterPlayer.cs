using System.Collections;
using UnityEngine;


[ExecuteInEditMode]
public class RegisterPlayer : RegisterTile
{
	/// <summary>
	/// True when the player is laying down
	/// </summary>
	public bool IsDownClient { get; private set; }
	public bool IsDownServer { get; set; }
	public bool IsStunnedClient { get; set; }
	public bool IsStunnedServer { get; private set; }


	private UserControlledSprites playerSprites;
	private PlayerScript playerScript;
	private MetaDataLayer metaDataLayer;

	public bool IsBlockingClient => !playerScript.IsGhost && !IsDownClient;
	public bool IsBlockingServer => !playerScript.IsGhost && !IsDownServer && !IsStunnedServer;
	private Coroutine unstunHandle;


	private void Awake()
	{
		playerSprites = GetComponent<UserControlledSprites>();
		playerScript = GetComponent<PlayerScript>();
		//initially we are upright and don't rotate with the matrix
		rotateWithMatrix = false;
		metaDataLayer = transform.GetComponentInParent<MetaDataLayer>();
	}

	public override bool IsPassable(bool isServer)
	{
		return isServer ? !IsBlockingServer : !IsBlockingClient;
	}

	public override bool IsPassable(Vector3Int from, bool isServer)
	{
		return IsPassable(isServer);
	}

	protected override void OnRotationStart(RotationOffset fromCurrent, bool isInitialRotation)
	{
		base.OnRotationStart(fromCurrent, isInitialRotation);
		if (!isInitialRotation)
		{
			UpdateManager.Instance.Add(RemainUpright);
		}
	}

	void RemainUpright()
	{
		//stay upright until rotation stops (RegisterTile only updates our rotation at the end of rotation),
		//but players need to stay upright constantly unless they are downed
		foreach (SpriteRenderer renderer in spriteRenderers)
		{
			renderer.transform.rotation = IsDownClient ? Quaternion.Euler(0, 0, -90) : Quaternion.identity;
		}
	}

	protected override void OnRotationEnd(RotationOffset fromCurrent, bool isInitialRotation)
	{
		base.OnRotationEnd(fromCurrent, isInitialRotation);

		if (!isInitialRotation)
		{
			//stop reorienting to face upright
			UpdateManager.Instance.Remove(RemainUpright);
		}

		//add extra rotation to ensure we are sideways
		if (IsDownClient)
		{
			foreach (SpriteRenderer spriteRenderer in spriteRenderers)
			{
				spriteRenderer.transform.Rotate(0, 0, -90);
			}
		}
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
			if (playerScript.playerMove.IsRestrained)
			{
				return;
			}
		}

		if (!IsDownClient)
		{
			IsDownClient = true;
			//make sure sprite is in sync with server regardless of local prediction
			playerSprites.SyncWithServer();
			//rotate the sprites and change their layer
			foreach (SpriteRenderer spriteRenderer in spriteRenderers)
			{
				spriteRenderer.transform.rotation = Quaternion.identity;
				spriteRenderer.transform.Rotate(0, 0, -90);
				spriteRenderer.sortingLayerName = "Blood";
			}
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
			//make sure sprite is in sync with server regardless of local prediction
			playerSprites.SyncWithServer();
			//change sprites to be upright
			foreach (SpriteRenderer spriteRenderer in spriteRenderers)
			{
				spriteRenderer.transform.rotation = Quaternion.identity;
				spriteRenderer.sortingLayerName = "Players";
			}
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
		if ( IsStunnedServer
			|| !slipWhileWalking && playerScript.PlayerSync.SpeedServer <= playerScript.playerMove.WalkSpeed
		    || playerScript.playerHealth.IsCrit
		    || playerScript.playerHealth.IsSoftCrit
		    || playerScript.playerHealth.IsDead)
		{
			return;
		}
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
		if ( IsStunnedServer )
		{
			return;
		}
		IsStunnedServer = true;
		PlayerUprightMessage.SendToAll(gameObject, !IsStunnedServer, IsStunnedServer);
		if (dropItem)
		{
			playerScript.playerNetworkActions.DropItem("leftHand");
			playerScript.playerNetworkActions.DropItem("rightHand");
		}
		playerScript.playerMove.allowInput = false;

		this.RestartCoroutine(StunTimer(stunDuration), ref unstunHandle);
	}
	private IEnumerator StunTimer(float stunTime)
	{
		yield return new WaitForSeconds(stunTime);
		RemoveStun();
	}

	public void RemoveStun()
	{
		IsStunnedServer = false;

		if (playerScript.playerHealth.IsCrit
		 || playerScript.playerHealth.IsSoftCrit
		 || playerScript.playerHealth.IsDead )
		{
			return;
		}

		PlayerUprightMessage.SendToAll(gameObject, !IsStunnedServer, IsStunnedServer);
		playerScript.playerMove.allowInput = true;
	}
}