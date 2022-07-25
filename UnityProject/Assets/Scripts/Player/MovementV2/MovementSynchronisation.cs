using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Editor.Attributes;
using HealthV2;
using Items;
using Managers;
using Messages.Client.Interaction;
using Mirror;
using Newtonsoft.Json;
using Objects;
using Player.Movement;
using ScriptableObjects.Audio;
using Tiles;
using UI;
using UI.Action;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

public class MovementSynchronisation : UniversalObjectPhysics, IPlayerControllable, IActionGUI, ICooldown,
	IBumpableObject, ICheckedInteractable<ContextMenuApply>
{
	public PlayerScript playerScript;

	public List<MoveData> MoveQueue = new List<MoveData>();

	private float MoveMaxDelayQueue = 4f; //Only matters when low FPS mode

	public float DefaultTime { get; } = 0.5f;

	public bool Step = false;

	[SyncVar(hook = nameof(SyncInput))] [NonSerialized]
	public bool allowInput = true; //Should be synchvar far

	[SyncVar(hook = nameof(SyncIntent))] [NonSerialized]
	public Intent intent; //TODO Cleanup in mind rework

	/// <summary>
	/// Invoked on server side when the cuffed state is changed
	/// </summary>
	[NonSerialized] public CuffEvent OnCuffChangeServer = new CuffEvent();

	[field: SyncVar(hook = nameof(SyncCuffed))]
	public bool IsCuffed { get; private set; }

	public bool IsTrapped => IsCuffed || ContainedInContainer != null;

	[PrefabModeOnly] public bool CanMoveThroughObstructions = false;

	//[SyncVar(hook = nameof(SyncRunSpeed))]
	public float RunSpeed;

	//[SyncVar(hook = nameof(SyncWalkSpeed))]
	public float WalkSpeed;

	//[SyncVar(hook = nameof(SyncCrawlingSpeed))]
	public float CrawlSpeed;

	private MovementType _currentMovementType;

	public MovementType CurrentMovementType
	{
		set
		{
			_currentMovementType = value;
			UpdateMovementSpeed();
		}
		get => _currentMovementType;
	}

	public ActionData actionData;
	ActionData IActionGUI.ActionData => actionData;

	public bool IsBumping = false;

	public void CallActionClient()
	{
		CmdUnbuckle();
	}


	[Command]
	public void CmdUnbuckle()
	{
		if (IsCuffed)
		{
			if (CanUnBuckleSelf())
			{
				Chat.AddActionMsgToChat(
					playerScript.gameObject,
					"You're trying to unbuckle yourself from the chair! (this will take some time...)",
					playerScript.name + " is trying to unbuckle themself from the chair!"
				);
				StandardProgressAction.Create(
					new StandardProgressActionConfig(StandardProgressActionType.Unbuckle),
					BuckledToObject.UnbuckleObject
				).ServerStartProgress(
					BuckledToObject.registerTile,
					BuckledToObject.GetComponent<BuckleInteract>().ResistTime,
					playerScript.gameObject
				);
			}
		}
		else
		{
			BuckledToObject.UnbuckleObject();
		}
	}

	private bool CanUnBuckleSelf()
	{
		PlayerHealthV2 playerHealth = playerScript.playerHealth;

		return !(playerHealth == null ||
		         playerHealth.ConsciousState == ConsciousState.DEAD ||
		         playerHealth.ConsciousState == ConsciousState.UNCONSCIOUS ||
		         playerHealth.ConsciousState == ConsciousState.BARELY_CONSCIOUS);
	}

	public override void BuckleToChange(UniversalObjectPhysics newBuckledTo)
	{
		if (PlayerManager.LocalPlayerObject == gameObject)
		{
			UIActionManager.ToggleLocal(this, newBuckledTo != null);
		}
	}


	[Server]
	public void ServerTryEscapeContainer()
	{
		if (ContainedInContainer != null)
		{
			GameObject parentContainer = ContainedInContainer.gameObject;

			foreach (var escapable in parentContainer.GetComponents<IEscapable>())
			{
				escapable.EntityTryEscape(gameObject, null);
			}
		}
		else if (BuckledToObject != null)
		{
			CmdUnbuckle();
		}
	}

	#region Cuffing

	/// <summary>
	/// Anything with PlayerMove can be cuffed and uncuffed. Might make sense to seperate that into its own behaviour
	/// </summary>
	/// <returns>The menu including the uncuff action if applicable, otherwise null</returns>
	public override RightClickableResult GenerateRightClickOptions()
	{
		var result = base.GenerateRightClickOptions();

		if (!WillInteract(ContextMenuApply.ByLocalPlayer(gameObject, "Uncuff"), NetworkSide.Client)) return result;

		return result.AddElement("Uncuff", OnUncuffClicked);
	}

	/// <summary>
	/// Used for the right click action, sends a message requesting uncuffing
	/// </summary>
	public void OnUncuffClicked()
	{
		RequestInteractMessage.Send(ContextMenuApply.ByLocalPlayer(gameObject, "Uncuff"), this);
	}

	/// <summary>
	/// Determines if the interaction request for uncuffing is valid clientside and if true, then serverside
	/// </summary>
	public bool WillInteract(ContextMenuApply interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side) && IsCuffed;
	}

	/// <summary>
	/// Handles the interaction request for uncuffing serverside
	/// </summary>
	public void ServerPerformInteraction(ContextMenuApply interaction)
	{
		TryUnCuff(interaction.TargetObject, interaction.Performer);
	}

	public void TryUnCuff(GameObject targetObject, GameObject performer)
	{
		var handcuffSlots = targetObject.GetComponent<DynamicItemStorage>().OrNull()
			?.GetNamedItemSlots(NamedSlot.handcuffs)
			.Where(x => x.IsEmpty == false).ToList();

		if (handcuffSlots == null) return;

		//Somehow has no cuffs but has cuffed effect, force uncuff
		if (handcuffSlots.Count == 0)
		{
			Uncuff();
			return;
		}

		foreach (var handcuffSlot in handcuffSlots)
		{
			var restraint = handcuffSlot.Item.GetComponent<Restraint>();
			if (restraint == null) continue;

			var progressConfig = new StandardProgressActionConfig(StandardProgressActionType.Uncuff, allowTurning: true);
			StandardProgressAction.Create(progressConfig, Uncuff)
				.ServerStartProgress(targetObject.RegisterTile(),
					restraint.RemoveTime * (handcuffSlots.Count / 2f), performer);

			//Only need to do it once
			break;
		}
	}

	/// <summary>
	/// Request a ContextMenuApply interaction if you have not done your own validation.
	/// Calling this clientside will break your client.
	/// </summary>
	[Server]
	public void Uncuff()
	{
		SyncCuffed(IsCuffed, false);
		foreach (var itemSlot in playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.handcuffs))
		{
			Inventory.ServerDrop(itemSlot);
		}
	}


	[Server]
	public void Cuff(HandApply interaction)
	{
		SyncCuffed(IsCuffed, true);

		var targetStorage = interaction.TargetObject.GetComponent<DynamicItemStorage>();

		//transfer cuffs to the special cuff slot

		foreach (var handcuffSlot in targetStorage.GetNamedItemSlots(NamedSlot.handcuffs))
		{
			Inventory.ServerTransfer(interaction.HandSlot, handcuffSlot);
			break;
		}

		//drop hand items
		foreach (var itemSlot in targetStorage.GetNamedItemSlots(NamedSlot.leftHand))
		{
			Inventory.ServerDrop(itemSlot);
		}

		foreach (var itemSlot in targetStorage.GetNamedItemSlots(NamedSlot.rightHand))
		{
			Inventory.ServerDrop(itemSlot);
		}
	}


	private void PlayerUIHandCuffToggle(bool hideState)
	{
		if (hideState)
		{
			HandsController.Instance.HideHands(HiddenHandValue.bothHands);
		}
		else
		{
			HandsController.Instance.HideHands(HiddenHandValue.none);
		}
	}

	private void SyncCuffed(bool wasCuffed, bool cuffed)
	{
		var oldCuffed = this.IsCuffed;
		this.IsCuffed = cuffed;

		if (isServer)
		{
			OnCuffChangeServer.Invoke(oldCuffed, this.IsCuffed);
		}

		if (this.gameObject == PlayerManager.LocalPlayerObject)
		{
			PlayerUIHandCuffToggle(this.IsCuffed);
		}
	}

	#endregion Cuffing

	private void SyncInput(bool oldInput, bool newInput)
	{
		allowInput = newInput;
	}

	private void SyncIntent(Intent oldIntent, Intent newIntent)
	{
		oldIntent = newIntent;
	}

	public override void Awake()
	{
		playerScript = GetComponent<PlayerScript>();
		OnThrowEnd.AddListener(ThrowEnding);
		base.Awake();
	}

	private void ThrowEnding(UniversalObjectPhysics thing)
	{
		if (playerScript.registerTile.IsLayingDown)
		{
			transform.localRotation = Quaternion.Euler(0, 0, 90);
		}
		else
		{
			transform.localRotation = Quaternion.Euler(0, 0, 0);
		}
	}

	public void Update()
	{
		if (isServer)
		{
			ServerCheckQueueingAndMove();
		}

		if (isLocalPlayer == false) return;
		bool inputDetected = KeyboardInputManager.IsMovementPressed(KeyboardInputManager.KeyEventType.Hold);
		if (inputDetected != IsPressedCashed)
		{
			IsPressedCashed = inputDetected;
			CMDPressedMovementKey(inputDetected);
		}
	}

	public bool IsPressedCashed;
	public bool IsPressedServer;

	[Command]
	public void CMDPressedMovementKey(bool isPressed)
	{
		IsPressedServer = isPressed;
	}

	private readonly HashSet<IMovementEffect> movementAffects = new HashSet<IMovementEffect>();
	private readonly HashSet<IMovementEffect> legs = new HashSet<IMovementEffect>();
	public bool HasALeg => legs.Count != 0;

	[Server]
	public void AddModifier(IMovementEffect modifier)
	{
		movementAffects.Add(modifier);
		UpdateSpeeds();
	}

	[Server]
	public void AddLeg(IMovementEffect newLeg)
	{
		legs.Add(newLeg);
		UpdateSpeeds();
	}

	[Server]
	public void RemoveModifier(IMovementEffect modifier)
	{
		movementAffects.Remove(modifier);
		UpdateSpeeds();
	}

	[Server]
	public void RemoveLeg(IMovementEffect oldLeg)
	{
		legs.Remove(oldLeg);
		if (legs.Count == 0)
		{
			RequestRest.Send(true);
		}
		UpdateSpeeds();
	}

	public void UpdateSpeeds()
	{
		float newRunSpeed = 0;
		float newWalkSpeed = 0;
		float newCrawlSpeed = 0;
		if (legs.Count == 0)
		{
			RunSpeed = 0;
			WalkSpeed = 0;
			foreach (var movementAffect in movementAffects)
			{
				newCrawlSpeed += movementAffect.CrawlingSpeedModifier;
			}
			CrawlSpeed = newCrawlSpeed;
			UpdateMovementSpeed();
			return;
		}
		foreach (var movementAffect in movementAffects)
		{
			newRunSpeed += movementAffect.RunningSpeedModifier;
			newWalkSpeed += movementAffect.WalkingSpeedModifier;
			newCrawlSpeed += movementAffect.CrawlingSpeedModifier;
		}

		RunSpeed = Mathf.Clamp(newRunSpeed, 0, float.MaxValue);
		WalkSpeed = Mathf.Clamp(newWalkSpeed, 0, float.MaxValue);
		CrawlSpeed = Mathf.Clamp(newCrawlSpeed, 0, float.MaxValue);
		UpdateMovementSpeed();
	}

	public void UpdateMovementSpeed()
	{
		switch (CurrentMovementType)
		{
			case MovementType.Running:
				SyncMovementSpeed(TileMoveSpeed, RunSpeed);
				break;
			case MovementType.Walking:
				SyncMovementSpeed(TileMoveSpeed, WalkSpeed);
				break;
			case MovementType.Crawling:
				SyncMovementSpeed(TileMoveSpeed, CrawlSpeed);
				break;
		}
	}

	[Command]
	public void CmdChangeCurrentWalkMode(bool isRunning)
	{
		if (CurrentMovementType == MovementType.Crawling) return;
		CurrentMovementType = isRunning ? MovementType.Running : MovementType.Walking;
	}

	public override void OnEnable()
	{
		base.OnEnable();
		if (isServer == false)
		{
			UpdateManager.Add(CallbackType.UPDATE, ClientCheckLocationFlight);
			return;
		}

		UpdateManager.Add(CallbackType.UPDATE, ServerCheckQueueingAndMove);
	}

	public override void OnDisable()
	{
		base.OnDisable();
		if (isServer == false)
		{
			UpdateManager.Remove(CallbackType.UPDATE, ClientCheckLocationFlight);
			return;
		}

		UpdateManager.Remove(CallbackType.UPDATE, ServerCheckQueueingAndMove);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		UpdateManager.Remove(CallbackType.UPDATE, ClientCheckLocationFlight);
	}

	public void OnBump(GameObject bumpedBy, GameObject client)
	{
		Pushing.Clear();
		Bumps.Clear();
		if(intent != Intent.Help) return;
		if (bumpedBy.TryGetComponent<MovementSynchronisation>(out var move))
		{
			if (move.CurrentMovementType == MovementType.Crawling) return;
			if (MatrixManager.IsPassableAtAllMatricesV2(bumpedBy.AssumedWorldPosServer(),
				    this.gameObject.AssumedWorldPosServer(), SetMatrixCache, this, Pushing, Bumps) == false) return;
			var pushVector = (bumpedBy.transform.position - this.transform.position).RoundToInt().To2Int();
			ForceTilePush(pushVector, Pushing, client, move.TileMoveSpeed);

			if (move.IsBumping == false) return;
			pushVector *= -1;
			move.ForceTilePush(pushVector, Pushing, client, move.TileMoveSpeed);
		}
	}


	public struct MoveData
	{
		public Vector3 LocalPosition;

		//The current location of the player (  just in case they are desynchronised )
		public int MatrixID;

		//( The matrix the movement is on )
		public PlayerMoveDirection GlobalMoveDirection;

		public PlayerMoveDirection LocalMoveDirection;

		//because you want the change in movement to be same across server and client
		public double Timestamp;

		//Timestamp with (800ms gap for being acceptable
		public bool CausesSlip;

		//Pushed objects
		public string PushedIDs;

		public bool Bump;

		public int LastPushID;

		//Object that it is pulling
		public uint Pulling;

		//LastReset ID
		public int LastResetID;
	}

	public enum PlayerMoveDirection
	{
		Up,
		Up_Right,
		Right,

		/* you are */
		Down_Right, //Dastardly
		Down,
		Down_Left,
		Left,
		Up_Left
	}

	public static Vector2Int Up_Right => new Vector2Int(1, 1);
	public static Vector2Int Down_Right => new Vector2Int(1, -1);
	public static Vector2Int Left_Down => new Vector2Int(-1, -1);
	public static Vector2Int Up_Left => new Vector2Int(-1, 1);

	public static PlayerMoveDirection VectorToPlayerMoveDirection(Vector2Int direction)
	{
		if (direction == Vector2Int.up)
		{
			return PlayerMoveDirection.Up;
		}
		else if (direction == Vector2Int.down)
		{
			return PlayerMoveDirection.Down;
		}
		else if (direction == Vector2Int.left)
		{
			return PlayerMoveDirection.Left;
		}
		else if (direction == Vector2Int.right)
		{
			return PlayerMoveDirection.Right;
		}
		else if (direction == Up_Right)
		{
			return PlayerMoveDirection.Up_Right;
		}
		else if (direction == Down_Right)
		{
			return PlayerMoveDirection.Down_Right;
		}
		else if (direction == Left_Down)
		{
			return PlayerMoveDirection.Down_Left;
		}
		else if (direction == Up_Left)
		{
			return PlayerMoveDirection.Up_Left;
		}

		return PlayerMoveDirection.Up;
	}

	[Command]
	public void ServerCommandValidatePosition(Vector3 clientLocalPOS)
	{
		if ((clientLocalPOS - transform.localPosition).magnitude > 1.5f)
		{
			ResetLocationOnClients(false);
		}
	}

	public void ClientCheckLocationFlight()
	{
		if (isLocalPlayer == false) return;
		if (IsFloating())
		{
			if (NetworkTime.time - LastUpdatedFlyingPosition > 2)
			{
				LastUpdatedFlyingPosition = NetworkTime.time;
				ServerCommandValidatePosition(transform.localPosition);
			}
		}
	}

	public double LastUpdatedFlyingPosition = 0;

	public double DEBUGLastMoveMessageProcessed = 0;

	public System.Random RNG = new System.Random();

	public void ServerCheckQueueingAndMove()
	{
		if (isLocalPlayer) return;

		if (CanInPutMove()) //TODO potential issue with messages building up
		{
			if (MoveQueue.Count > 0)
			{
				bool fudged = false;
				Vector3 stored = Vector3.zero;
				var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;

				var entry = MoveQueue[0];
				MoveQueue.RemoveAt(0);

				if (entry.LastResetID != SetLastResetID) //Client hasn't been reset yet
				{
					if (entry.Pulling != NetId.Empty)
					{

						if (ComponentManager.TryGetUniversalObjectPhysics(spawned[entry.Pulling].gameObject, out var SupposedlyPulling))
						{
							SupposedlyPulling.ResetLocationOnClient(connectionToClient);
						}
					}

					if (string.IsNullOrEmpty(entry.PushedIDs) == false)
					{
						foreach (var nonMatch in JsonConvert.DeserializeObject<List<uint>>(entry.PushedIDs))
						{
							spawned[nonMatch].GetComponent<UniversalObjectPhysics>()
								.ResetLocationOnClient(connectionToClient);
						}
					}
					return;

				}

				SetMatrixCache.ResetNewPosition(transform.position, registerTile);
				//Logger.LogError(" Is Animating " +  Animating + " Is floating " +  IsAnimatingFlyingSliding +" move processed at" + transform.localPosition);

				if (Pulling.HasComponent == false && entry.Pulling != NetId.Empty)
				{
					PullSet(null, false);
					if (ComponentManager.TryGetUniversalObjectPhysics(spawned[entry.Pulling].gameObject, out var supposedlyPulling))
					{
						supposedlyPulling.ResetLocationOnClient(connectionToClient);
					}
				}
				else if ( Pulling.HasComponent && Pulling.Component.netId != entry.Pulling)
				{
					PullSet(null, false);
					if (ComponentManager.TryGetUniversalObjectPhysics(spawned[entry.Pulling].gameObject, out var supposedlyPulling))
					{
						supposedlyPulling.ResetLocationOnClient(connectionToClient);
					}
				}


				if (IsFlyingSliding)
				{
					if ((transform.position - entry.LocalPosition.ToWorld(MatrixManager.Get(entry.MatrixID)))
					    .magnitude <
					    0.24f) //TODO Maybe not needed if needed can be used is when Move request comes in before player has quite reached tile in space flight
					{
						stored = transform.localPosition;
						transform.localPosition = entry.LocalPosition;
						registerTile.ServerSetLocalPosition(entry.LocalPosition.RoundToInt());
						registerTile.ClientSetLocalPosition(entry.LocalPosition.RoundToInt());
						SetMatrixCache.ResetNewPosition(transform.position, registerTile);
						fudged = true;
					}
					else
					{
						//Logger.LogError(" Fail the Range floating check ");
						ResetLocationOnClients();
						MoveQueue.Clear();
						return;
					}
				}
				else
				{
					if (SetTimestampID == entry.LastPushID || entry.LastPushID == -1)
					{
						if ((transform.position - entry.LocalPosition.ToWorld(MatrixManager.Get(entry.MatrixID)))
						    .magnitude >
						    0.75f) //Resets play location if too far away
						{
							// Logger.LogError("Reset from distance from actual target" +
							//                 (transform.position -
							//                  Entry.LocalPosition.ToWorld(MatrixManager.Get(Entry.MatrixID))).magnitude +
							//                 " SERVER : " +
							//                 transform.position + " Client : " +
							//                 Entry.LocalPosition.ToWorld(MatrixManager.Get(Entry.MatrixID)));

							if ((transform.position - entry.LocalPosition.ToWorld(MatrixManager.Get(entry.MatrixID)))
							    .magnitude >
							    3f)
							{
								ResetLocationOnClients();
							}
							else
							{
								ResetLocationOnClients(true);
							}
							return;
						}
					}
				}


				if (CanInPutMove())
				{
					if (TryMove(ref entry, gameObject, true, out var slip))
					{
						//Logger.LogError("Move processed");
						if (string.IsNullOrEmpty(entry.PushedIDs) == false || Pushing.Count > 0)
						{
							var specialist = new List<uint>();
							var netIDList = new List<uint>();
							foreach (var push in Pushing)
							{
								specialist.Add(push.netId);
							}

							if (string.IsNullOrEmpty(entry.PushedIDs) == false)
							{
								netIDList = JsonConvert.DeserializeObject<List<uint>>(entry.PushedIDs);
							}

							var nonMatching = new List<uint>();

							foreach (var @in in netIDList)
							{
								if (specialist.Contains(@in) == false)
								{
									nonMatching.Add(@in);
								}
							}

							foreach (var @in in specialist)
							{
								if (netIDList.Contains(@in) == false)
								{
									nonMatching.Add(@in);
								}
							}

							foreach (var nonMatch in nonMatching)
							{
								spawned[nonMatch].GetComponent<UniversalObjectPhysics>()
									.ResetLocationOnClient(connectionToClient);
							}
						}

						if (entry.CausesSlip != slip)
						{
							ResetLocationOnClients();
						}

						Step = !Step;
						if (Step)
						{
							FootstepSounds.PlayerFootstepAtPosition(transform.position, this);
						}


						// if (RNG.Next(0, 100) > 50)
						// {
						// 	var Node = registerTile.Matrix.MetaDataLayer.Get(registerTile.LocalPosition);
						// 	var  feets =  playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.feet).PickRandom();
						//
						//
						// 	Node.AppliedDetail.AddDetail(new Detail()
						// 	{
						//
						// 	})
						// }




						//TODO this is good but need to clean up movement a bit more Logger.LogError("Delta magnitude " + (transform.position - Entry.LocalPosition.ToWorld(MatrixManager.Get(Entry.MatrixID).Matrix)).magnitude );
						//do calculation is and set targets and stuff
						//Reset client if movement failed Since its good movement only Getting sent
						//if there's enough time to do The next movement to the current time, Then process it instantly
						//Like,  it takes 1 to do movement
						//timestamp says 0 for the first, 1 For the second
						//the current server timestamp is 2
						//So that means it can do 1 and 2 Messages , in the same frame

						if (MoveQueue.Count > 0)
							//yes Time.timeAsDouble Can rollover but this would only be a problem for a second
						{
							if (FPSMonitor.Instance.Average < 10)
							{
								if ((entry.Timestamp + (TileMoveSpeed) < NetworkTime.time))
								{
									transform.localPosition = LocalTargetPosition;
									registerTile.ServerSetLocalPosition(LocalTargetPosition.RoundToInt());
									registerTile.ClientSetLocalPosition(LocalTargetPosition.RoundToInt());
									ServerCheckQueueingAndMove();
								}
							}
						}
					}
					else
					{
						//Logger.LogError("Failed TryMove");
						if (fudged)
						{
							transform.localPosition = stored;
							registerTile.ServerSetLocalPosition(stored.RoundToInt());
							registerTile.ClientSetLocalPosition(stored.RoundToInt());
							SetMatrixCache.ResetNewPosition(transform.position, registerTile);
						}

						ResetLocationOnClients();
					}
				}
				else
				{
					//Logger.LogError("Failed Can input");
					if (fudged)
					{
						transform.localPosition = stored;
						registerTile.ServerSetLocalPosition(stored.RoundToInt());
						registerTile.ClientSetLocalPosition(stored.RoundToInt());
						SetMatrixCache.ResetNewPosition(transform.position, registerTile);
					}

					ResetLocationOnClients();
				}
			}
		}
	}


	public void ReceivePlayerMoveAction(PlayerAction moveActions)
	{
		if (UIManager.IsInputFocus) return;
		if (CommonInput.GetKeyDown(KeyCode.F7) && gameObject == PlayerManager.LocalPlayerObject)
		{
			PlayerSpawn.ServerSpawnDummy(gameObject.transform);
		}


		if (moveActions.moveActions.Length == 0) return;

		if (KeyboardInputManager.IsControlPressed())
		{
			rotatable.SetFaceDirectionLocalVector(moveActions.Direction());
			return;
		}

		SetMatrixCache.ResetNewPosition(transform.position, registerTile);

		if (CanInPutMove())
		{
			var newMoveData = new MoveData()
			{
				LocalPosition = transform.localPosition,
				Timestamp = NetworkTime.time,
				MatrixID = registerTile.Matrix.Id,
				GlobalMoveDirection = moveActions.ToPlayerMoveDirection(),
				CausesSlip = false,
				Bump = false,
				LastPushID = SetTimestampID,
				Pulling = Pulling.Component.OrNull()?.netId ?? NetId.Empty,
				LastResetID = SetLastResetID
			};


			if (TryMove(ref newMoveData, gameObject, false, out _))
			{
				AfterSuccessfulTryMove(newMoveData);
				return;
			}

			if (newMoveData.GlobalMoveDirection.IsDiagonal())
			{
				var cache = newMoveData.GlobalMoveDirection;
				newMoveData.GlobalMoveDirection = cache.ToNonDiagonal(true);
				newMoveData.Bump = false;
				if (TryMove(ref newMoveData, gameObject, false, out _))
				{
					AfterSuccessfulTryMove(newMoveData);
					return;
				}

				newMoveData.GlobalMoveDirection = cache.ToNonDiagonal(false);
				newMoveData.Bump = false;
				if (TryMove(ref newMoveData, gameObject, false, out _))
				{
					AfterSuccessfulTryMove(newMoveData);
					return;
				}
			}

			return;
		}

		//Can't do normal move, so check to see if dead
		if (playerScript.OrNull()?.playerHealth.OrNull()?.IsDead == true)
		{
			playerScript.playerNetworkActions.CmdSpawnPlayerGhost();
			return;
		}

		//Check to see if in container
		if (ContainedInContainer != null)
		{
			CMDTryEscapeContainer();
		}
	}

	[Command]
	public void CMDTryEscapeContainer()
	{
		if (allowInput == false) return;
		if (ContainedInContainer == null) return;

		foreach (var Escape in ContainedInContainer.IEscapables)
		{
			Escape.EntityTryEscape(gameObject, null);
		}
	}


	public void AfterSuccessfulTryMove(MoveData NewMoveData)
	{
		if (isServer)
		{
			if (isLocalPlayer && this.playerScript.OrNull()?.Equipment.OrNull()?.ItemStorage != null)
			{
				Step = !Step;
				if (Step)
				{
					FootstepSounds.PlayerFootstepAtPosition(transform.position, this);
				}
			}
		}

		var addedLocalPosition =
			(transform.position + NewMoveData.GlobalMoveDirection.ToVector().To3())
			.ToLocal(MatrixManager.Get(NewMoveData.MatrixID));

		NewMoveData.LocalMoveDirection = VectorToPlayerMoveDirection(
			(addedLocalPosition - transform.position.ToLocal(MatrixManager.Get(NewMoveData.MatrixID))).To2Int());
		//Because shuttle could be rotated   enough to make Global  Direction invalid As compared to server

		if (Pushing.Count > 0)
		{
			List<uint> netIDs = new List<uint>();
			foreach (var push in Pushing)
			{
				netIDs.Add(push.GetComponent<NetworkIdentity>().netId);
			}

			NewMoveData.PushedIDs = JsonConvert.SerializeObject(netIDs);
		}
		else
		{
			NewMoveData.PushedIDs = "";
		}

		//Logger.LogError(" Requested move > wth  Bump " + NewMoveData.Bump);
		CMDRequestMove(NewMoveData);
	}

	public bool TryMove(ref MoveData newMoveData, GameObject byClient, bool serverProcessing, out bool causesSlip)
	{
		causesSlip = false;
		Bumps.Clear();
		Pushing.Clear();
		if (CanMoveTo(newMoveData, out var causesSlipClient, Pushing, Bumps, out var pushesOff,
			    out var slippingOn))
		{
			if (serverProcessing == false)
			{
				newMoveData.CausesSlip = causesSlipClient;
			}
			else
			{
				if (newMoveData.Bump)
				{
					// Logger.LogError("NewMoveData.Bump");
					return true;
				}

				causesSlip = causesSlipClient;
			}


			if (pushesOff) //space walking
			{
				if (pushesOff.TryGetComponent<UniversalObjectPhysics>(out var objectPhysics))
				{
					var move = newMoveData.GlobalMoveDirection.ToVector();
					move.Normalize();
					objectPhysics.TryTilePush(move * -1, byClient, TileMoveSpeed);
				}
				//Pushes off object for example pushing the object the other way
			}

			UniversalObjectPhysics toRemove = null;
			if (intent == Intent.Help)
			{
				foreach (var toPush in Pushing)
				{
					var player = toPush as MovementSynchronisation;
					if (player != null)
					{
						if (player.intent == Intent.Help)
						{
							toRemove = toPush;
							player.OnBump(this.gameObject, byClient);
						}
					}
				}

				if (toRemove != null)
				{
					Pushing.Remove(toRemove);
				}
			}

			//move
			ForceTilePush(newMoveData.GlobalMoveDirection.ToVector(), Pushing, byClient,
				isWalk: true, pushedBy: this);

			SetMatrixCache.ResetNewPosition(registerTile.WorldPosition, registerTile); //Resets the cash

			if (causesSlipClient)
			{
				NewtonianPush(newMoveData.GlobalMoveDirection.ToVector(), TileMoveSpeed, Single.NaN, 4,
					spinFactor: 35, doNotUpdateThisClient: byClient);

				var player = registerTile as RegisterPlayer;
				player.OrNull()?.ServerSlip();
			}

			if (toRemove != null)
			{
				Pushing.Add(toRemove);
			}

			return true;
		}
		else
		{
			IsBumping = true;
			bool bumpedSomething = false;
			if (Cooldowns.TryStart(playerScript, this, NetworkSide.Server))
			{
				foreach (var bump in Bumps)
				{
					bump.OnBump(this.gameObject, byClient);
					bumpedSomething = true;
				}
			}

			IsBumping = false;
			if (serverProcessing == false)
			{
				newMoveData.Bump = bumpedSomething;
			}

			return bumpedSomething;
		}
	}

	public bool CanInPutMove(bool queueing = false)
		//False for in machine/Buckled, No gravity/Nothing to push off, Is slipping, Is being thrown, Is incapacitated
	{
		if (queueing == false)
		{
			if (IsWalking) return false;
		}

		if (slideTime > 0) return false;
		if (allowInput == false) return false;
		if (BuckledToObject) return false;
		if (isLocalPlayer && UIManager.IsInputFocus) return false;
		if (IsCuffed && PulledBy.HasComponent) return false;
		if (ContainedInContainer != null) return false;

		return true;
	}

	// public bool CausesSlip

	public bool CanMoveTo(MoveData moveAction, out bool causesSlipClient, List<UniversalObjectPhysics> willPushObjects,
			List<IBumpableObject> bumps,
			out RegisterTile pushesOff,
			out ItemAttributesV2 slippedOn) //Stuff like shuttles and machines handled in their own IPlayerControllable,
		//Space movement, normal movement ( Calling running and walking part of this )

	{
		if (BuckledToObject == null)
		{
			bool obstruction = true;
			bool floating = true;
			if (IsNotFloating(moveAction, out pushesOff))
			{
				floating = false;
				if (CanMoveThroughObstructions)
				{
					causesSlipClient = false;
					slippedOn = null;
					return true;
				}

				//Need to check for Obstructions
				if (IsNotObstructed(moveAction, willPushObjects, bumps))
				{
					causesSlipClient = DoesSlip(moveAction, out slippedOn);
					return true;
				}
				else
				{
					//if (isServer) Logger.LogError("failed is obstructed");

					rotatable.SetFaceDirectionLocalVector(moveAction.GlobalMoveDirection.ToVector());
				}
			}
			else
			{
				//if (isServer) Logger.LogError("failed is floating");
			}
		}

		slippedOn = null;
		causesSlipClient = false;
		willPushObjects.Clear();
		pushesOff = null;
		return false;
	}

	public bool DoesSlip(MoveData moveAction, out ItemAttributesV2 slippedOn)
	{
		bool slipProtection = true;
		if (playerScript.DynamicItemStorage != null)
		{
			foreach (var itemSlot in playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.feet))
			{
				if (itemSlot.ItemAttributes == null ||
				    itemSlot.ItemAttributes.HasTrait(CommonTraits.Instance.NoSlip) == false)
				{
					slipProtection = false;
				}
			}
		}

		slippedOn = null;
		if (slipProtection) return false;
		if (CurrentMovementType != MovementType.Running) return false;
		if (isServer == false && isLocalPlayer && UIManager.Instance.intentControl.Running == false) return false;


		var toMatrix = SetMatrixCache.GetforDirection(moveAction.GlobalMoveDirection.ToVector().To3Int()).Matrix;
		var localTo = (registerTile.WorldPosition + moveAction.GlobalMoveDirection.ToVector().To3Int())
			.ToLocal(toMatrix)
			.RoundToInt();
		if (toMatrix.MetaDataLayer.IsSlipperyAt(localTo))
		{
			return true;
		}

		var crossedItems = toMatrix.Get<ItemAttributesV2>(localTo, isServer);
		foreach (var crossedItem in crossedItems)
		{
			if (crossedItem.HasTrait(CommonTraits.Instance.Slippery))
			{
				slippedOn = crossedItem;
				return true;
			}
		}

		return false;
	}

	public bool IsNotObstructed(MoveData moveAction, List<UniversalObjectPhysics> pushing, List<IBumpableObject> bumps)
	{
		var transform1 = transform.position;
		return MatrixManager.IsPassableAtAllMatricesV2(transform1,
			transform1 + moveAction.GlobalMoveDirection.ToVector().To3Int(), SetMatrixCache, this,
			pushing, bumps);
	}


	public bool IsNotFloating(MoveData? moveAction,
		out RegisterTile canPushOff) //Sets bool For floating
	{
		if (stickyMovement)
		{
			if (NewtonianMovement.magnitude > maximumStickSpeed)
			{
				IsCurrentlyFloating = true;
				canPushOff = null;
				return false;
			}
		}

		if (IsNotFloatingTileMap())
		{
			IsCurrentlyFloating = false;
			NewtonianMovement *= 0;
			canPushOff = null;
			return true;
		}

		if (IsNotFloatingObjects(moveAction, out canPushOff))
		{
			IsCurrentlyFloating = false;
			NewtonianMovement *= 0;
			return true;
		}

		IsCurrentlyFloating = true;
		return false;
	}


	public bool IsNotFloatingTileMap()
	{
		return MatrixManager.IsFloatingAtV2Tile(transform.position, CustomNetworkManager.IsServer,
			SetMatrixCache, true) == false;
	}

	public bool IsNotFloatingObjects(MoveData? moveAction, out RegisterTile canPushOff)
	{
		if (moveAction == null)
		{
			//Then just check around the area for something that Grounds
			canPushOff = null;
			if (MatrixManager.IsFloatingAtV2Objects(ContextGameObjects, transform.position.RoundToInt(),
				    CustomNetworkManager.IsServer, SetMatrixCache) == false)
			{
				canPushOff = null;
				return true;
			}
			else
			{
				canPushOff = null;
				return false;
			}
		}
		else
		{
			//Looks around, observes object it can push off, it is not floating and CanPushOff
			//Looks around observes nothing it can push off, but is connected to object , is not floating but not Push it off
			if (MatrixManager.IsNotFloatingAtV2Objects(moveAction.Value, ContextGameObjects,
				    transform.position.RoundToInt(),
				    CustomNetworkManager.IsServer, SetMatrixCache, out canPushOff))
			{
				return true;
			}
			else
			{
				canPushOff = null;
				return false;
			}
		}

		canPushOff = null;
		return false;
	}

	[Command]
	public void CMDRequestMove(MoveData inMoveData)
	{
		if (CanInPutMove(true))
		{
			var Age = NetworkTime.time - inMoveData.Timestamp;
			if (Age > MoveMaxDelayQueue)
			{
				// Logger.LogError(
					// $" Move message rejected because it is too old, Consider tweaking if ping is too high or Is being exploited Age {Age}");
				ResetLocationOnClients();
				MoveQueue.Clear();
				return;
			}


			// NewMoveData.LocalMoveDirection =
			// VectorToPlayerMoveDirection((LocalTargetPosition - transform.localPosition).RoundToInt().To2Int());

			//TODO Might be funny with changing to diagonal not too sure though
			var addedGlobalPosition =
				(transform.position.ToLocal(MatrixManager.Get(inMoveData.MatrixID)) +
				 inMoveData.LocalMoveDirection.ToVector().To3()).ToWorld(MatrixManager.Get(inMoveData.MatrixID));

			inMoveData.GlobalMoveDirection =
				VectorToPlayerMoveDirection((addedGlobalPosition - transform.position).To2Int());
			//Logger.LogError(" Received move at  " + InMoveData.LocalPosition.ToString() + "  Currently at " + transform.localPosition );
			MoveQueue.Add(inMoveData);
		}
	}

	public override void LocalTileReached(Vector3Int localPos)
	{
		var tile = registerTile.Matrix.MetaTileMap.GetTile(localPos, LayerType.Base);
		if (tile != null && tile is BasicTile c)
		{
			foreach (var interaction in c.TileStepInteractions)
			{
				if (interaction.WillAffectPlayer(playerScript) == false) continue;
				interaction.OnPlayerStep(playerScript);
			}
		}

		//Check for tiles before objects because of this list
		if (registerTile.Matrix.MetaTileMap.ObjectLayer.EnterTileBaseList == null) return;
		var loopto = registerTile.Matrix.MetaTileMap.ObjectLayer.EnterTileBaseList.Get(localPos);
		foreach (var enterTileBase in loopto)
		{
			if (enterTileBase.WillAffectPlayer(playerScript) == false) continue;
			enterTileBase.OnPlayerStep(playerScript);
		}

		if (isLocalPlayer == false) return;

		//Client side check for invalid tabs still open
		//(Don't need to do this server side as the interactions are validated)
		ControlTabs.CheckTabClose();
	}
}

/// <summary>
/// Cuff state changed, provides old state and new state as 1st and 2nd args
/// </summary>
public class CuffEvent : UnityEvent<bool, bool>
{
}