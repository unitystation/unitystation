using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using Systems.Teleport;
using Messages.Server.SoundMessages;

[RequireComponent(typeof(Directional))]
[RequireComponent(typeof(UprightSprites))]
[ExecuteInEditMode]
public class RegisterPlayer : RegisterTile, IServerSpawn, RegisterPlayer.IControlPlayerState
{

	public interface IControlPlayerState
	{
		bool AllowChange(bool rest);
	}

	const int HELP_CHANCE = 33; // Percent.

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

	private HashSet<IControlPlayerState> CheckableStatuses = new HashSet<IControlPlayerState>();

	/// <summary>
	/// Invoked on server when slip state is change. Provides old and new value as 1st and 2nd args
	/// </summary>
	[NonSerialized]
	public SlipEvent OnSlipChangeServer = new SlipEvent();

	private PlayerScript playerScript;
	public PlayerScript PlayerScript => playerScript;
	private Directional playerDirectional;
	private UprightSprites uprightSprites;
	[SerializeField] private Util.NetworkedLeanTween networkedLean;

	/// <summary>
	/// Returns whether this player is blocking other players from occupying the space, using the
	/// correct server/client side logic based on where this is being called from.
	/// </summary>
	public bool IsBlocking => isServer ? IsBlockingServer : IsBlockingClient;
	public bool IsBlockingClient => !playerScript.IsGhost && !IsLayingDown;
	public bool IsBlockingServer => !playerScript.IsGhost && !IsLayingDown && !IsSlippingServer;
	private Coroutine unstunHandle;


	protected override void Awake()
	{
		base.Awake();
		AddStatus(this);
		playerScript = GetComponent<PlayerScript>();
		uprightSprites = GetComponent<UprightSprites>();
		playerDirectional = GetComponent<Directional>();
		playerDirectional.ChangeDirectionWithMatrix = false;
		uprightSprites.spriteMatrixRotationBehavior = SpriteMatrixRotationBehavior.RemainUpright;
	}

	public void AddStatus(IControlPlayerState iThisControlPlayerState)
	{
		CheckableStatuses.Add(iThisControlPlayerState);
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		ServerCheckStandingChange( isLayingDown);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		ServerCheckStandingChange(false);
	}

	public override bool IsPassable(bool isServer, GameObject context = null)
	{
		return isServer ? !IsBlockingServer : !IsBlockingClient;
	}

	public override bool IsPassableFromOutside(Vector3Int from, bool isServer, GameObject context = null)
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
		ServerCheckStandingChange(true);
	}

	/// <summary>
	/// Make the player appear standing up.
	/// When up, they become impassable.
	/// </summary>
	[Server]
	public void ServerStandUp()
	{
		ServerCheckStandingChange(false);
	}

	/// <summary>
	/// Make the player appear standing up.
	/// When up, they become impassable.
	/// </summary>
	[Server]
	public void ServerStandUp(bool DoBar = false, float Time = 0.5f)
	{
		ServerCheckStandingChange(false,DoBar, Time);
	}

	/// <summary>
	/// Make the player appear laying down or standing up. When up, they become impassable
	/// </summary>
	/// <param name="isStanding"></param>
	[Server]
	public void ServerSetIsStanding(bool isStanding)
	{
		ServerCheckStandingChange(!isStanding);
	}


	public void ServerCheckStandingChange(bool LayingDown, bool DoBar = false, float Time = 0.5f)
	{
		if (this.isLayingDown != LayingDown)
		{
			foreach (var Status in CheckableStatuses)
			{
				if (Status.AllowChange(LayingDown) == false)
				{
					return;
				}
			}

			if (DoBar)
			{
				var bar = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.SelfHeal, false, false, true), ServerStandUp);
				bar.ServerStartProgress(this, 1.5f,gameObject);
			}
			else
			{
				SyncIsLayingDown(isLayingDown, LayingDown);
			}

		}
	}

	private void SyncIsLayingDown(bool wasDown, bool isDown)
	{
		this.isLayingDown = isDown;

		if (CustomNetworkManager.IsHeadless == false)
		{
			HandleGetupAnimation(isDown == false);
		}

		if (isDown)
		{
			//uprightSprites.ExtraRotation = Quaternion.Euler(0, 0, -90);
			//Change sprite layer
			foreach (SpriteRenderer spriteRenderer in this.GetComponentsInChildren<SpriteRenderer>())
			{
				spriteRenderer.sortingLayerName = "Bodies";
			}
			playerScript.PlayerSync.SpeedServer = playerScript.playerMove.CrawlSpeed;
			//lock current direction
			playerDirectional.LockDirection = true;
		}
		else
		{
			//uprightSprites.ExtraRotation = Quaternion.identity;
			//back to original layer
			foreach (SpriteRenderer spriteRenderer in this.GetComponentsInChildren<SpriteRenderer>())
			{
				spriteRenderer.sortingLayerName = "Players";
			}
			playerDirectional.LockDirection = false;
			playerScript.PlayerSync.SpeedServer = playerScript.playerMove.RunSpeed;
		}
	}

	public void HandleGetupAnimation(bool getUp)
	{
		if (getUp == false && networkedLean.Target.rotation.z > -90)
		{
			networkedLean.RotateGameObject(new Vector3(0, 0, -90), 0.15f);
		}
		else if (getUp == true && networkedLean.Target.rotation.z < 90)
		{
			networkedLean.RotateGameObject(new Vector3(0, 0, 0), 0.19f);
		}
	}

	/// <summary>
	/// Try to help the player stand back up.
	/// </summary>
	[Server]
	public void ServerHelpUp()
	{
		if (!IsLayingDown) return;

		// Can't help a player up if they're rolling
		if (playerScript.playerNetworkActions.IsRolling) return;

		// Check if lying down because of stun. If stunned, there is a chance helping can fail.
		if (IsSlippingServer && Random.Range(0, 100) > HELP_CHANCE) return;

		ServerRemoveStun();
	}

	bool RegisterPlayer.IControlPlayerState.AllowChange(bool rest)
	{
		if (rest)
		{
			return true;
		}
		else
		{
			return !IsSlippingServer;
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
		// Don't slip if you got no legs (HealthV2)
		if (IsSlippingServer
			|| !slipWhileWalking && playerScript.PlayerSync.SpeedServer <= playerScript.playerMove.WalkSpeed
			|| playerScript.playerHealth.IsCrit
			|| playerScript.playerHealth.IsSoftCrit
			|| playerScript.playerHealth.IsDead
			|| playerScript.playerHealth.HungerState == HungerState.Starving)
		{
			return;
		}

		ServerStun();
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.9f, 1.1f));
		SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Slip, WorldPositionServer, audioSourceParameters, sourceObj: gameObject);
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
		ServerCheckStandingChange( true);
		OnSlipChangeServer.Invoke(oldVal, IsSlippingServer);
		if (dropItem)
		{
			foreach (var itemSlot in playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.leftHand))
			{
				Inventory.ServerDrop(itemSlot);
			}

			foreach (var itemSlot in playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.rightHand))
			{
				Inventory.ServerDrop(itemSlot);
			}
		}
		playerScript.playerMove.allowInput = false;

		this.RestartCoroutine(StunTimer(stunDuration), ref unstunHandle);
	}
	private IEnumerator StunTimer(float stunTime)
	{
		yield return WaitFor.Seconds(stunTime);

		// Check if player is still stunned when timer ends
		// (may have been helped up by another player, for example)
		if (IsSlippingServer)
		{
			ServerRemoveStun();
		}
	}

	private void ServerRemoveStun()
	{
		var oldVal = IsSlippingServer;
		IsSlippingServer = false;

		// Do not raise up a dead body
		if (playerScript.playerHealth.ConsciousState == ConsciousState.CONSCIOUS)
		{
			ServerCheckStandingChange( false);
		}

		OnSlipChangeServer.Invoke(oldVal, IsSlippingServer);

		if (playerScript.playerHealth.ConsciousState == ConsciousState.CONSCIOUS
			 || playerScript.playerHealth.ConsciousState == ConsciousState.BARELY_CONSCIOUS)
		{
			playerScript.playerMove.allowInput = true;
		}
	}
	// <summary>
	/// Performs bluespace activity (teleports randomly) on player if they have slipped on an object
	/// with bluespace activity or are hit by an object with bluespace acivity and
	/// has Liquid Contents.
	/// Assumes max potency if none is given.
	/// </summary>
	///
	public void ServerBluespaceActivity(int potency = 100)
	{
		int maxRange = 11;
		int potencyStrength = (int)Math.Round((potency * .01f) * maxRange, 0);
		TeleportUtils.ServerTeleportRandom(playerScript.gameObject, 0, potencyStrength, false, true);
	}
}

/// <summary>
/// Fired when slip state changes. Provides old and new value.
/// </summary>
public class SlipEvent : UnityEvent<bool, bool>
{
}
