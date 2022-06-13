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
using UI.Action;
using UnityEngine;
using UnityEngine.Events;

public class MovementSynchronisation : UniversalObjectPhysics, IPlayerControllable, IActionGUI ,ICooldown, IBumpableObject, ICheckedInteractable<ContextMenuApply>, IRightClickable
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

	[SyncVar] private Vector2 PushingData;

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
	public RightClickableResult GenerateRightClickOptions()
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

			var progressConfig = new StandardProgressActionConfig(StandardProgressActionType.Uncuff);
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


	private void PlayerUIHandCuffToggle(bool HideState)
	{
		if (HideState)
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

	private void SyncInput(bool OLDInput, bool NewInput)
	{
		allowInput = NewInput;
	}

	private void SyncIntent(Intent OLDIntent, Intent NEWIntent)
	{
		OLDIntent = NEWIntent;
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

		if (Intangible == false && CanNotBeWindPushed == false)
		{
			CheckWindOtherPush();
		}
		else if (PushingData.magnitude != 0)
		{
			PushingData = Vector2.zero;
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
	public void CMDPressedMovementKey(bool IsPressed)
	{
		IsPressedServer = IsPressed;
	}

	public void CheckWindOtherPush()
	{
		if (isServer)
		{
			var node = registerTile.Matrix.GetMetaDataNode(registerTile.LocalPosition);
			Vector2 Data = Vector2.zero;
			foreach (var Push in node.WindData)
			{
				Data += Push;
			}

			if (Data.magnitude > 0.1f)
			{
				if (PushingData != Data)
				{
					PushingData = Data;
				}
			}
			else
			{
				if (PushingData.magnitude != 0)
				{
					PushingData = Vector2.zero;
				}
			}
		}

		if (PushingData.magnitude > 0.1f == false)
		{
			SetIgnoreSticky = false;
			return;
		}
		SetIgnoreSticky = true;

		if (IsFlyingSliding)
		{
			NewtonianMovement = Vector2.MoveTowards(NewtonianMovement, PushingData, 0.5f);
		}
		else
		{
			NewtonianPush(PushingData, PushingData.magnitude/2f );
		}
	}


	private readonly HashSet<IMovementEffect> movementAffects = new HashSet<IMovementEffect>();

	[Server]
	public void AddModifier(IMovementEffect modifier)
	{
		movementAffects.Add(modifier);
		UpdateSpeeds();
	}

	[Server]
	public void RemoveModifier(IMovementEffect modifier)
	{
		movementAffects.Remove(modifier);
		UpdateSpeeds();
	}

	public void UpdateSpeeds()
	{
		float newRunSpeed = 0;
		float newWalkSpeed = 0;
		float newCrawlSpeed = 0;
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

	public void OnBump(GameObject bumpedBy, GameObject Client)
	{
		Pushing.Clear();
		Bumps.Clear();
		if (intent == Intent.Help)
		{
			if (bumpedBy.TryGetComponent<MovementSynchronisation>(out var move))
			{
				if (move.intent == Intent.Help)
				{
					if (MatrixManager.IsPassableAtAllMatricesV2(bumpedBy.AssumedWorldPosServer(),
						    this.gameObject.AssumedWorldPosServer(), SetMatrixCash, this, Pushing, Bumps))
					{
						var PushVector = (bumpedBy.transform.position - this.transform.position).RoundToInt().To2Int();
						ForceTilePush(PushVector, Pushing, Client, move.TileMoveSpeed);

						if (move.IsBumping)
						{
							PushVector *= -1;
							move.ForceTilePush(PushVector, Pushing, Client, move.TileMoveSpeed);
						}
					}
				}
			}
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
	public void ServerCommandValidatePosition(Vector3 ClientLocalPOS)
	{
		if ((ClientLocalPOS - transform.localPosition).magnitude > 1.5f)
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


	public void ServerCheckQueueingAndMove()
	{
		if (isLocalPlayer) return;

		if (CanInPutMove()) //TODO potential issue with messages building up
		{
			if (MoveQueue.Count > 0)
			{
				bool Fudged = false;
				Vector3 Stored = Vector3.zero;

				var Entry = MoveQueue[0];
				MoveQueue.RemoveAt(0);

				SetMatrixCash.ResetNewPosition(transform.position, registerTile);
				//Logger.LogError(" Is Animating " +  Animating + " Is floating " +  IsAnimatingFlyingSliding +" move processed at" + transform.localPosition);

				if (IsFlyingSliding)
				{
					if ((transform.position - Entry.LocalPosition.ToWorld(MatrixManager.Get(Entry.MatrixID)))
					    .magnitude <
					    0.24f) //TODO Maybe not needed if needed can be used is when Move request comes in before player has quite reached tile in space flight
					{
						Stored = transform.localPosition;
						transform.localPosition = Entry.LocalPosition;
						registerTile.ServerSetLocalPosition(Entry.LocalPosition.RoundToInt());
						registerTile.ClientSetLocalPosition(Entry.LocalPosition.RoundToInt());
						SetMatrixCash.ResetNewPosition(transform.position, registerTile);
						Fudged = true;
					}
					else
					{
						Logger.LogError(" Fail the Range floating check ");
						ResetLocationOnClients();
						MoveQueue.Clear();
						return;
					}
				}
				else
				{
					if (SetTimestampID == Entry.LastPushID || Entry.LastPushID == -1)
					{
						if ((transform.position - Entry.LocalPosition.ToWorld(MatrixManager.Get(Entry.MatrixID)))
						    .magnitude >
						    0.75f) //Resets play location if too far away
						{
							Logger.LogError("Reset from distance from actual target" +
							                (transform.position -
							                 Entry.LocalPosition.ToWorld(MatrixManager.Get(Entry.MatrixID))).magnitude +
							                " SERVER : " +
							                transform.position + " Client : " +
							                Entry.LocalPosition.ToWorld(MatrixManager.Get(Entry.MatrixID)));

							if ((transform.position - Entry.LocalPosition.ToWorld(MatrixManager.Get(Entry.MatrixID)))
							    .magnitude >
							    3f)
							{
								ResetLocationOnClients();
							}
							else
							{
								ResetLocationOnClients(true);
							}

							MoveQueue.Clear();
							return;
						}
					}
				}


				if (CanInPutMove())
				{
					if (TryMove(ref Entry, gameObject, true, out var Slip))
					{
						//Logger.LogError("Move processed");
						if (string.IsNullOrEmpty(Entry.PushedIDs) == false || Pushing.Count > 0)
						{
							var specialist = new List<uint>();
							var NetIDList = new List<uint>();
							foreach (var Push in Pushing)
							{
								specialist.Add(Push.netId);
							}

							if (string.IsNullOrEmpty(Entry.PushedIDs) == false)
							{
								NetIDList = JsonConvert.DeserializeObject<List<uint>>(Entry.PushedIDs);
							}

							var Nonmatching = new List<uint>();

							foreach (var _In in NetIDList)
							{
								if (specialist.Contains(_In) == false)
								{
									Nonmatching.Add(_In);
								}
							}

							foreach (var _In in specialist)
							{
								if (NetIDList.Contains(_In) == false)
								{
									Nonmatching.Add(_In);
								}
							}


							foreach (var NonMatch in Nonmatching)
							{
								NetworkIdentity.spawned[NonMatch].GetComponent<UniversalObjectPhysics>()
									.ResetLocationOnClient(connectionToClient);
							}
						}

						if (Entry.CausesSlip != Slip)
						{
							ResetLocationOnClients();
							MoveQueue.Clear();
						}

						Step = !Step;
						if (Step)
						{
							FootstepSounds.PlayerFootstepAtPosition(transform.position, this);
						}

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
								if ((Entry.Timestamp + (TileMoveSpeed) < NetworkTime.time))
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
						Logger.LogError("Failed TryMove");
						if (Fudged)
						{
							transform.localPosition = Stored;
							registerTile.ServerSetLocalPosition(Stored.RoundToInt());
							registerTile.ClientSetLocalPosition(Stored.RoundToInt());
							SetMatrixCash.ResetNewPosition(transform.position, registerTile);
						}

						ResetLocationOnClients();
						MoveQueue.Clear();
					}
				}
				else
				{
					Logger.LogError("Failed Can input");
					if (Fudged)
					{
						transform.localPosition = Stored;
						registerTile.ServerSetLocalPosition(Stored.RoundToInt());
						registerTile.ClientSetLocalPosition(Stored.RoundToInt());
						SetMatrixCash.ResetNewPosition(transform.position, registerTile);
					}

					ResetLocationOnClients();
					MoveQueue.Clear();
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
		SetMatrixCash.ResetNewPosition(transform.position, registerTile);

		if (CanInPutMove())
		{
			var NewMoveData = new MoveData()
			{
				LocalPosition = transform.localPosition,
				Timestamp = NetworkTime.time,
				MatrixID = registerTile.Matrix.Id,
				GlobalMoveDirection = moveActions.ToPlayerMoveDirection(),
				CausesSlip = false,
				Bump = false,
				LastPushID = SetTimestampID
			};


			if (TryMove(ref NewMoveData, gameObject, false, out _))
			{
				AfterSuccessfulTryMove(NewMoveData);
				return;
			}
			else if (NewMoveData.GlobalMoveDirection.IsDiagonal())
			{
				var Cash = NewMoveData.GlobalMoveDirection;
				NewMoveData.GlobalMoveDirection = Cash.ToNonDiagonal(true);
				NewMoveData.Bump = false;
				if (TryMove(ref NewMoveData, gameObject, false, out _))
				{
					AfterSuccessfulTryMove(NewMoveData);
					return;
				}

				NewMoveData.GlobalMoveDirection = Cash.ToNonDiagonal(false);
				NewMoveData.Bump = false;
				if (TryMove(ref NewMoveData, gameObject, false, out _))
				{
					AfterSuccessfulTryMove(NewMoveData);
					return;
				}
			}
		}
		else
		{
			if (playerScript.OrNull()?.playerHealth.OrNull()?.IsDead == true)
			{
				playerScript.playerNetworkActions.CmdSpawnPlayerGhost();
			}
			else
			{
				if (ContainedInContainer != null)
				{
					CMDTryEscapeContainer();
				}
			}
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

		var AddedLocalPosition =
			(transform.position + NewMoveData.GlobalMoveDirection.TVectoro().To3())
			.ToLocal(MatrixManager.Get(NewMoveData.MatrixID));

		NewMoveData.LocalMoveDirection = VectorToPlayerMoveDirection(
			(AddedLocalPosition - transform.position.ToLocal(MatrixManager.Get(NewMoveData.MatrixID))).To2Int());
		//Because shuttle could be rotated   enough to make Global  Direction invalid As compared to server

		if (Pushing.Count > 0)
		{
			List<uint> NetIDs = new List<uint>();
			foreach (var Push in Pushing)
			{
				NetIDs.Add(Push.GetComponent<NetworkIdentity>().netId);
			}

			NewMoveData.PushedIDs = JsonConvert.SerializeObject(NetIDs);
		}
		else
		{
			NewMoveData.PushedIDs = "";
		}

		//Logger.LogError(" Requested move > wth  Bump " + NewMoveData.Bump);
		CMDRequestMove(NewMoveData);
		return;
	}

	public bool TryMove(ref MoveData NewMoveData, GameObject ByClient, bool ServerProcessing, out bool causesSlip)
	{
		causesSlip = false;
		Bumps.Clear();
		Pushing.Clear();
		if (CanMoveTo(NewMoveData, out var CausesSlipClient, Pushing, Bumps, out var PushesOff,
			    out var SlippingOn))
		{
			if (ServerProcessing == false)
			{
				NewMoveData.CausesSlip = CausesSlipClient;
			}
			else
			{
				if (NewMoveData.Bump)
				{
					Logger.LogError("NewMoveData.Bump");
					return true;
				}

				causesSlip = CausesSlipClient;
			}


			if (PushesOff) //space walking
			{
				if (PushesOff.TryGetComponent<UniversalObjectPhysics>(out var PhysicsObject))
				{
					var move = NewMoveData.GlobalMoveDirection.TVectoro();
					move.Normalize();
					PhysicsObject.TryTilePush((move * -1).RoundToInt().To2Int(), ByClient, TileMoveSpeed);
				}
				//Pushes off object for example pushing the object the other way
			}

			UniversalObjectPhysics Toremove = null;
			if (intent == Intent.Help)
			{
				foreach (var PushPull in Pushing)
				{
					var Player = PushPull as MovementSynchronisation;
					if (Player != null)
					{
						if (Player.intent == Intent.Help)
						{
							Toremove = PushPull;
							Player.OnBump(this.gameObject, ByClient);
						}
					}
				}

				if (Toremove != null)
				{
					Pushing.Remove(Toremove);
				}
			}

			//move
			ForceTilePush(NewMoveData.GlobalMoveDirection.TVectoro().To2Int(), Pushing, ByClient,
				isWalk: true, pushedBy: this);

			SetMatrixCash.ResetNewPosition(registerTile.WorldPosition, registerTile); //Resets the cash

			if (CausesSlipClient)
			{
				NewtonianPush(NewMoveData.GlobalMoveDirection.TVectoro().To2Int(), TileMoveSpeed, Single.NaN, 4,
					spinFactor: 35, DoNotUpdateThisClient: ByClient);

				var Player = registerTile as RegisterPlayer;
				Player.OrNull()?.ServerSlip();
			}

			if (Toremove != null)
			{
				Pushing.Add(Toremove);
			}

			return true;
		}
		else
		{
			IsBumping = true;
			bool BumpedSomething = false;
			if (Cooldowns.TryStart(playerScript, this, NetworkSide.Server))
			{
				foreach (var Bump in Bumps)
				{
					Bump.OnBump(this.gameObject, ByClient);
					BumpedSomething = true;
				}
			}

			IsBumping = false;
			if (ServerProcessing == false)
			{
				NewMoveData.Bump = BumpedSomething;
			}

			return BumpedSomething;
		}
	}

	public bool CanInPutMove(bool Queueing = false)
		//False for in machine/Buckled, No gravity/Nothing to push off, Is slipping, Is being thrown, Is incapacitated
	{
		if (Queueing == false)
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

	public bool CanMoveTo(MoveData moveAction, out bool CausesSlipClient, List<UniversalObjectPhysics> WillPushObjects,
			List<IBumpableObject> Bumps,
			out RegisterTile PushesOff,
			out ItemAttributesV2 slippedOn) //Stuff like shuttles and machines handled in their own IPlayerControllable,
		//Space movement, normal movement ( Calling running and walking part of this )

	{
		if (BuckledToObject == null)
		{
			bool Obstruction = true;
			bool Floating = true;
			if (IsNotFloating(moveAction, out PushesOff))
			{
				Floating = false;
				if (CanMoveThroughObstructions)
				{
					CausesSlipClient = false;
					slippedOn = null;
					return true;
				}

				//Need to check for Obstructions
				if (IsNotObstructed(moveAction, WillPushObjects, Bumps))
				{
					CausesSlipClient = DoesSlip(moveAction, out slippedOn);
					return true;
				}
				else
				{
					//if (isServer) Logger.LogError("failed is obstructed");
				}
			}
			else
			{
				//if (isServer) Logger.LogError("failed is floating");
			}
		}

		slippedOn = null;
		CausesSlipClient = false;
		WillPushObjects.Clear();
		PushesOff = null;
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

		var ToMatrix = SetMatrixCash.GetforDirection(moveAction.GlobalMoveDirection.TVectoro().To3Int()).Matrix;
		var LocalTo = (registerTile.WorldPosition + moveAction.GlobalMoveDirection.TVectoro().To3Int())
			.ToLocal(ToMatrix)
			.RoundToInt();
		if (ToMatrix.MetaDataLayer.IsSlipperyAt(LocalTo))
		{
			return true;
		}

		var crossedItems = ToMatrix.Get<ItemAttributesV2>(LocalTo, isServer);
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

	public bool IsNotObstructed(MoveData moveAction, List<UniversalObjectPhysics> Pushing, List<IBumpableObject> Bumps)
	{
		var transform1 = transform.position;
		return MatrixManager.IsPassableAtAllMatricesV2(transform1,
			transform1 + moveAction.GlobalMoveDirection.TVectoro().To3Int(), SetMatrixCash, this,
			Pushing, Bumps);
	}


	public bool IsNotFloating(MoveData? moveAction,
		out RegisterTile CanPushOff) //Sets bool For floating
	{
		if (stickyMovement)
		{
			if (NewtonianMovement.magnitude > maximumStickSpeed)
			{
				IsCurrentlyFloating = true;
				CanPushOff = null;
				return false;
			}
		}

		if (IsNotFloatingTileMap())
		{
			IsCurrentlyFloating = false;
			NewtonianMovement *= 0;
			CanPushOff = null;
			return true;
		}

		if (IsNotFloatingObjects(moveAction, out CanPushOff))
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
			SetMatrixCash, true) == false;
	}

	public bool IsNotFloatingObjects(MoveData? moveAction, out RegisterTile CanPushOff)
	{
		if (moveAction == null)
		{
			//Then just check around the area for something that Grounds
			CanPushOff = null;
			if (MatrixManager.IsFloatingAtV2Objects(ContextGameObjects, transform.position.RoundToInt(),
				    CustomNetworkManager.IsServer, SetMatrixCash) == false)
			{
				CanPushOff = null;
				return true;
			}
			else
			{
				CanPushOff = null;
				return false;
			}
		}
		else
		{
			//Looks around, observes object it can push off, it is not floating and CanPushOff
			//Looks around observes nothing it can push off, but is connected to object , is not floating but not Push it off
			if (MatrixManager.IsNotFloatingAtV2Objects(moveAction.Value, ContextGameObjects,
				    transform.position.RoundToInt(),
				    CustomNetworkManager.IsServer, SetMatrixCash, out CanPushOff))
			{
				return true;
			}
			else
			{
				CanPushOff = null;
				return false;
			}
		}

		CanPushOff = null;
		return false;
	}

	[Command]
	public void CMDRequestMove(MoveData InMoveData)
	{
		if (CanInPutMove(true))
		{
			var Age = NetworkTime.time - InMoveData.Timestamp;
			if (Age > MoveMaxDelayQueue)
			{
				Logger.LogError(
					$" Move message rejected because it is too old, Consider tweaking if ping is too high or Is being exploited Age {Age}");
				ResetLocationOnClients();
				MoveQueue.Clear();
				return;
			}


			// NewMoveData.LocalMoveDirection =
			// VectorToPlayerMoveDirection((LocalTargetPosition - transform.localPosition).RoundToInt().To2Int());

			//TODO Might be funny with changing to diagonal not too sure though
			var AddedGlobalPosition =
				(transform.position.ToLocal(MatrixManager.Get(InMoveData.MatrixID)) +
				 InMoveData.LocalMoveDirection.TVectoro().To3()).ToWorld(MatrixManager.Get(InMoveData.MatrixID));

			InMoveData.GlobalMoveDirection =
				VectorToPlayerMoveDirection((AddedGlobalPosition - transform.position).To2Int());
			//Logger.LogError(" Received move at  " + InMoveData.LocalPosition.ToString() + "  Currently at " + transform.localPosition );
			MoveQueue.Add(InMoveData);
		}
	}
}

/// <summary>
/// Cuff state changed, provides old state and new state as 1st and 2nd args
/// </summary>
public class CuffEvent : UnityEvent<bool, bool>
{
}