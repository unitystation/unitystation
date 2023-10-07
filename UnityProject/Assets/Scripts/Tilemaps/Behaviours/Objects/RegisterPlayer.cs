using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using Systems.Teleport;
using Messages.Server.SoundMessages;
using Player;
using Player.Movement;
using Systems.Explosions;

[RequireComponent(typeof(Rotatable))]
[RequireComponent(typeof(UprightSprites))]
[RequireComponent(typeof(LayDown))]
[ExecuteInEditMode]
public class RegisterPlayer : RegisterTile, IServerSpawn, RegisterPlayer.IControlPlayerState
{

	public interface IControlPlayerState
	{
		bool AllowChange(bool rest);
	}

	const int HELP_CHANCE = 33; // Percent.

	[field: SerializeField] public LayDown LayDownBehavior { get; private set; }

	/// <summary>
	/// True when the player is laying down for any reason (shown sideways)
	/// </summary>
	public bool IsLayingDown => LayDownBehavior.IsLayingDown;

	/// <summary>
	/// True when the player is slipping
	/// </summary>
	public bool IsSlippingServer { get; private set; }

	private HashSet<IControlPlayerState> CheckableStatuses = new HashSet<IControlPlayerState>();

	/// <summary>
	/// Invoked on server when slip state is change. Provides old and new value as 1st and 2nd args
	/// </summary>
	[NonSerialized] public SlipEvent OnSlipChangeServer = new SlipEvent();

	/// <summary>
	/// Invoked on server when slip state is change. Provides old and new value as 1st and 2nd args
	/// </summary>
	[NonSerialized] public LyingDownStateEvent OnLyingDownChangeEvent = new LyingDownStateEvent();

	private PlayerScript playerScript;
	public PlayerScript PlayerScript => playerScript;
	private Rotatable playerDirectional;
	public Rotatable PlayerDirectional => playerDirectional;
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
		playerDirectional = GetComponent<Rotatable>();
		LayDownBehavior ??= GetComponent<LayDown>();
		uprightSprites.spriteMatrixRotationBehavior = SpriteMatrixRotationBehavior.RemainUpright;
	}

	public void AddStatus(IControlPlayerState iThisControlPlayerState)
	{
		CheckableStatuses.Add(iThisControlPlayerState);
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		ServerCheckStandingChange(IsLayingDown);
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

	private const float LayingDownTime = 0.5f;

	[Command]
	public void CmdSetRest(bool layingDown)
	{
		if (layingDown)
		{
			if(playerScript.PlayerTypeSettings.CanRest == false) return;

			if (ServerCheckStandingChange(true))
			{
				Chat.AddExamineMsgFromServer(gameObject, "You try to lie down.");
			}

			return;
		}

		if (playerScript.playerMove.HasALeg == false)
		{
			Chat.AddExamineMsg(gameObject,"You try standing up stand up but you have no legs!");
			return;
		}

		if (ServerCheckStandingChange(false, true, LayingDownTime))
		{
			Chat.AddExamineMsgFromServer(gameObject, "You try to stand up.");
		}
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
	public void ServerStandUp(bool doBar = false, float time = 1.5f)
	{
		ServerCheckStandingChange(false, doBar, time);
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

	public bool ServerCheckStandingChange(bool layingDown, bool doBar = false, float time = 1.5f)
	{
		if (IsLayingDown == layingDown) return false;

		foreach (var status in CheckableStatuses)
		{
			if (status.AllowChange(layingDown) == false)
			{
				return false;
			}
		}

		if (doBar)
		{
			var bar = StandardProgressAction.Create(
				new StandardProgressActionConfig(StandardProgressActionType.SelfHeal, false, false, true),
				() =>
				{
					bool cando = true;
					foreach (var status in CheckableStatuses)
					{
						if (status.AllowChange(layingDown) == false)
						{
							cando = false;
						}
					}

					if (cando)
					{
						SyncIsLayingDown(layingDown);
					}
				});

			bar.ServerStartProgress(this, time, gameObject);
		}
		else
		{
			SyncIsLayingDown(layingDown);
		}

		return true;
	}

	public override void MatrixChange(Matrix MatrixOld, Matrix MatrixNew)
	{
		if (MatrixOld != null && MatrixOld.PresentPlayers.Contains(this))
		{
			MatrixOld.PresentPlayers.Remove(this);
			MatrixNew.UpdatedPlayerFrame = Time.frameCount;
		}

		if (MatrixNew != null && MatrixNew.PresentPlayers.Contains(this) == false)
		{
			MatrixNew.PresentPlayers.Add(this);
			MatrixNew.UpdatedPlayerFrame = Time.frameCount;
		}
	}

	private void SyncIsLayingDown(bool isDown)
	{
		OnLyingDownChangeEvent?.Invoke(isDown);
		LayDownBehavior.SyncLayDownState(LayDownBehavior.IsLayingDown, isDown);
	}


	/// <summary>
	/// Try to help the player stand back up.
	/// </summary>
	[Server]
	public void ServerHelpUp()
	{
		if (IsLayingDown == false) return;

		// Can't help a player up if they're rolling
		if (playerScript.PlayerNetworkActions.IsRolling) return;

		// Check if lying down because of stun. If stunned, there is a chance helping can fail.
		if (IsSlippingServer && Random.Range(0, 100) > HELP_CHANCE) return;

		ServerRemoveStun();
	}

	bool IControlPlayerState.AllowChange(bool rest)
	{
		if (rest)
		{
			return true;
		}

		return IsSlippingServer == false;
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
        // Don't slip while crawling
		// Don't slip while player's consious state is crit, soft crit, or dead.
		// Don't slip while the players hunger state is Strarving
		// Don't slip if you got no legs (HealthV2)
		if (IsSlippingServer
			|| !slipWhileWalking && playerScript.PlayerSync.CurrentTileMoveSpeed <= playerScript.playerMove.WalkSpeed
            || IsLayingDown
			|| playerScript.playerHealth.IsCrit
			|| playerScript.playerHealth.IsSoftCrit
			|| playerScript.playerHealth.IsDead)
		{
			return;
		}

		ServerSlip();
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.9f, 1.1f));
		SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Slip, WorldPositionServer, audioSourceParameters, sourceObj: gameObject);
		// Let go of pulled items.
		playerScript.ObjectPhysics.StopPulling(false);
	}

	private void ServerSlip()
	{
		ServerCheckStandingChange(true);
		OnSlipChangeServer.Invoke(IsSlippingServer, true);
		playerScript.DynamicItemStorage.ServerDropItemsInHand();
	}

	/// <summary>
	/// Stops the player from moving and interacting for a period of time.
	/// Also drops held items by default.
	/// </summary>
	/// <param name="stunDuration">Time before the stun is removed.</param>
	/// <param name="dropItem">If items in the hand slots should be dropped on stun.</param>
	public void ServerStun(float stunDuration = 4f, bool dropItem = true, bool checkForArmor = true, bool StopMovement = true, Action stunImmunityFeedback = null)
	{
		bool CheckArmorStunImmunity()
		{
			foreach (var bodyPart in PlayerScript.playerHealth.SurfaceBodyParts)
			{
				if(bodyPart.SelfArmor.StunImmunity) continue;
				if(bodyPart.BodyPartType is not (BodyPartType.Chest or BodyPartType.Custom)) continue;
				foreach (Armor armor in bodyPart.ClothingArmors)
				{
					if (armor.StunImmunity) return true;
				}
			}
			return false;
		}

		if (checkForArmor && CheckArmorStunImmunity())
		{
			if(stunImmunityFeedback != null) stunImmunityFeedback();
			return;
		}

		var oldVal = IsSlippingServer;
		IsSlippingServer = true;
		ServerCheckStandingChange( true);
		OnSlipChangeServer.Invoke(oldVal, IsSlippingServer);
		if (dropItem)
		{
			playerScript.DynamicItemStorage.ServerDropItemsInHand();
		}

		if (StopMovement)
		{
			playerScript.playerMove.ServerAllowInput.RecordPosition(this, false);
		}


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

	public void ServerRemoveStun()
	{
		var oldVal = IsSlippingServer;
		IsSlippingServer = false;

		// Do not raise up a dead body
		if (playerScript.playerHealth.ConsciousState == ConsciousState.CONSCIOUS)
		{
			ServerCheckStandingChange(false);
		}

		OnSlipChangeServer.Invoke(oldVal, IsSlippingServer);

		if (playerScript.playerHealth.ConsciousState == ConsciousState.CONSCIOUS
			 || playerScript.playerHealth.ConsciousState == ConsciousState.BARELY_CONSCIOUS)
		{
			playerScript.playerMove.ServerAllowInput.RemovePosition(this);
		}
	}

	/// <summary>
	/// Performs bluespace activity (teleports randomly) on player if they have slipped on an object
	/// with bluespace activity or are hit by an object with bluespace activity and
	/// has Liquid Contents.
	/// Assumes max potency if none is given.
	/// </summary>
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
public class SlipEvent : UnityEvent<bool, bool> { }

/// <summary>
/// Event which fires when lying down state changes
/// </summary>
public class LyingDownStateEvent : UnityEvent<bool> { }
