using System;
using System.Collections.Generic;
using System.Linq;
using Core.Editor.Attributes;
using Core.Utils;
using Items;
using Logs;
using Managers;
using Messages.Client.Interaction;
using Mirror;
using Newtonsoft.Json;
using Objects;
using Player.Movement;
using ScriptableObjects;
using ScriptableObjects.Audio;
using Systems.Character;
using Systems.Teleport;
using Tiles;
using UI;
using UI.Core.Action;
using UnityEngine;
using UnityEngine.Events;

public class MovementSynchronisation : UniversalObjectPhysics, IPlayerControllable, IActionGUI, ICooldown,
	IBumpableObject, ICheckedInteractable<ContextMenuApply>
{
	public bool IsPressedCashed;
	public bool IsPressedServer;
	public bool HardcodedSpeed = false;
	public double LastUpdatedFlyingPosition = 0;
	public PlayerScript playerScript;

	public List<MoveData> MoveQueue = new List<MoveData>();

	private const float MOVE_MAX_DELAY_QUEUE = 4f; //Only matters when low FPS mode

	public float DefaultTime { get; } = 0.5f;

	public bool Step = false;

	[SyncVar(hook = nameof(SyncInput))] [NonSerialized]
	private bool allowInput = true;

	public bool AllowInput => allowInput;

	public readonly MultiInterestBool ServerAllowInput = new MultiInterestBool(true, MultiInterestBool.RegisterBehaviour.RegisterFalse, MultiInterestBool.BoolBehaviour.ReturnOnFalse  );

	[SyncVar(hook = nameof(SyncIntent))] [NonSerialized]
	public Intent intent; //TODO Cleanup in mind rework

	/// <summary>
	/// Invoked on server side when the cuffed state is changed
	/// </summary>
	[NonSerialized] public CuffEvent OnCuffChangeServer = new CuffEvent();

	[field: SyncVar(hook = nameof(SyncCuffed))]
	public bool IsCuffed { get; private set; }

	public bool IsTrapped => IsCuffed || ContainedInObjectContainer != null;

	  public bool CanMoveThroughObstructions = false;

	//Sync vars commented out as only the current speed is sync'd
	//[SyncVar(hook = nameof(SyncRunSpeed))]
	public float RunSpeed;

	//[SyncVar(hook = nameof(SyncWalkSpeed))]
	public float WalkSpeed;

	//[SyncVar(hook = nameof(SyncCrawlingSpeed))]
	public float CrawlSpeed;

	private PassableExclusionHolder holder;

	[SerializeField] private PassableExclusionTrait needsWalking = null;
	[SerializeField] private PassableExclusionTrait needsRunning = null;

	[SyncVar(hook = nameof(SyncMovementType))]
	private MovementType currentMovementType;

	public MovementType CurrentMovementType
	{
		set
		{
			currentMovementType = value;

			if (isServer)
			{
				UpdateMovementSpeed();
			}

			UpdatePassables();
		}
		get => currentMovementType;
	}

	public ActionData actionData;
	ActionData IActionGUI.ActionData => actionData;

	public bool IsBumping = false;

	private CooldownInstance moveCooldown = new CooldownInstance(0.1f);

	private const float MINIMUM_MOVEMENT_SPEED = 0.6f;

	/// <summary>
	/// Event which fires when movement type changes (run/walk)
	/// </summary>
	[NonSerialized] public MovementStateEvent MovementStateEventServer = new MovementStateEvent();

	public void CallActionClient()
	{
		CmdUnbuckle();
	}

	[Command]
	public void CmdUnbuckle()
	{
		if (BuckledToObject == null) return;
		var buckleInteract = BuckledToObject.GetComponent<BuckleInteract>();
		if (buckleInteract == null)
		{
			Loggy.LogError($"{BuckledToObject.gameObject.ExpensiveName()} has no BuckleInteract!");
			return;
		}

		buckleInteract.TryUnbuckle(playerScript);
	}

	public override void BuckleToChange(UniversalObjectPhysics newBuckledTo)
	{
		if (isServer)
		{
			UIActionManager.ToggleServer(gameObject, this, newBuckledTo != null);
		}
	}


	[Server]
	public void ServerTryEscapeContainer()
	{
		if (ContainedInObjectContainer != null)
		{
			GameObject parentContainer = ContainedInObjectContainer.gameObject;

			foreach (var escapable in parentContainer.GetComponents<IEscapable>())
			{
				escapable.EntityTryEscape(gameObject, null, MoveAction.NoMove);
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

			var progressConfig =
				new StandardProgressActionConfig(StandardProgressActionType.Uncuff, allowTurning: true);
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
	//(Max) TODO: Move cuffing logic into their own separate component.
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
		holder = GetComponent<PassableExclusionHolder>();

		ServerAllowInput.OnBoolChange.AddListener(BoolServerAllowInputChange);

		base.Awake();
	}

	private void BoolServerAllowInputChange(bool NewValue)
	{
		SyncInput(allowInput, NewValue);
	}

	public void Update()
	{
		if (isServer)
		{
			ServerCheckQueueingAndMove();
		}

		if (hasAuthority == false) return;
		bool inputDetected = KeyboardInputManager.IsMovementPressed(KeyboardInputManager.KeyEventType.Hold);
		if (inputDetected != IsPressedCashed)
		{
			IsPressedCashed = inputDetected;
			CMDPressedMovementKey(inputDetected);
		}
	}

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

		if (legs.Count == 0 && TryGetComponent<RegisterPlayer>(out var registerPlayer))
		{
			registerPlayer.ServerCheckStandingChange(true);
		}

		UpdateSpeeds();
	}

	public void UpdateSpeeds()
	{
		if (HardcodedSpeed)
		{
			UpdateMovementSpeed();
			return;
		}

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

		RunSpeed = Mathf.Clamp(newRunSpeed, MINIMUM_MOVEMENT_SPEED, float.MaxValue);
		WalkSpeed = Mathf.Clamp(newWalkSpeed, MINIMUM_MOVEMENT_SPEED, float.MaxValue);
		CrawlSpeed = Mathf.Clamp(newCrawlSpeed, MINIMUM_MOVEMENT_SPEED, float.MaxValue);
		UpdateMovementSpeed();
	}

	public void UpdateMovementSpeed()
	{
		switch (CurrentMovementType)
		{
			case MovementType.Running:
				SetMovementSpeed(RunSpeed);
				break;
			case MovementType.Walking:
				SetMovementSpeed(WalkSpeed);
				break;
			case MovementType.Crawling:
				SetMovementSpeed(CrawlSpeed);
				break;
		}
	}

	[Command]
	public void CmdChangeCurrentWalkMode(bool isRunning)
	{
		if (CurrentMovementType == MovementType.Crawling) return;
		CurrentMovementType = isRunning ? MovementType.Running : MovementType.Walking;
		MovementStateEventServer?.Invoke(isRunning);
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

		if (CanSwap(bumpedBy, out var move))
		{
			if (MatrixManager.IsPassableAtAllMatricesV2(bumpedBy.AssumedWorldPosServer(), this.gameObject.AssumedWorldPosServer(), SetMatrixCache, this, Pushing, Bumps) == false) return;
			var pushVector = (move.transform.position - this.transform.position).RoundToInt().To2Int();
			if (Mathf.Abs(pushVector.x) > 1 || Mathf.Abs(pushVector.y) > 1) return;
			Pushing.Clear();
			ForceTilePush(pushVector, Pushing, client, move.CurrentTileMoveSpeed, SendWorld: false);
		}
	}

	public bool CanSwap(GameObject bumpedBy, out MovementSynchronisation move)
	{
		move = null;
		if (intent != Intent.Help) return false;
		if (bumpedBy.TryGetComponent<MovementSynchronisation>(out move))
		{
			if (move.intent != Intent.Help || move.CurrentMovementType == MovementType.Crawling ||
			    move.Pulling.HasComponent != false) return false;
			return true;

		}

		return false;
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

		public bool SwappedOnMove;

		public string SwappedWithIDs;

		public bool IsNotMove;
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
		if (hasAuthority == false || IsFloating() == false) return;
		if (NetworkTime.time - LastUpdatedFlyingPosition > 2)
		{
			LastUpdatedFlyingPosition = NetworkTime.time;
			ServerCommandValidatePosition(transform.localPosition);
		}
	}


	[TargetRpc]
	public void TargetRPCClientTilePush(NetworkConnection target, Vector2Int worldDirection, float speed,
		uint causedByClient, bool overridePull,
		int timestampID, bool forced)
	{

	}

	private void ClientResetOnLastID(MoveData entry, Dictionary<uint, NetworkIdentity> spawned)
	{
		if (entry.Pulling != NetId.Empty)
		{
			if (ComponentManager.TryGetUniversalObjectPhysics(spawned[entry.Pulling].gameObject,
				    out var SupposedlyPulling))
			{
				SupposedlyPulling.ResetLocationOnClient(connectionToClient);
			}
		}

		if (string.IsNullOrEmpty(entry.PushedIDs) != false) return;
		foreach (var nonMatch in JsonConvert.DeserializeObject<List<uint>>(entry.PushedIDs))
		{
			spawned[nonMatch].GetComponent<UniversalObjectPhysics>()
				.ResetLocationOnClient(connectionToClient);
		}
	}

	private void ServerCheckClientLocation(ref MoveData entry, ref bool fudged, ref Vector3 stored, out bool reset, out bool resetSmooth)
	{
		reset = false;
		resetSmooth = false;
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
			if (SetTimestampID != entry.LastPushID && entry.LastPushID != -1) return;
			if ((transform.position - entry.LocalPosition.ToWorld(MatrixManager.Get(entry.MatrixID)))
			    .magnitude >
			    0.75f) //Resets play location if too far away
			{
				//TODO Force tile push if close enough??
				if ((transform.position - entry.LocalPosition.ToWorld(MatrixManager.Get(entry.MatrixID)))
				    .magnitude >
				    3f)
				{
					reset = true;
				}
				else
				{
					resetSmooth = true;
					reset = true;
				}
			}
		}
	}

	private void ServerCheckQueueingPulling(ref Dictionary<uint, NetworkIdentity> spawned, ref MoveData entry)
	{
		switch (Pulling.HasComponent)
		{
			case false when entry.Pulling != NetId.Empty:
			{
				PullSet(null, false);
				if (ComponentManager.TryGetUniversalObjectPhysics(spawned[entry.Pulling].gameObject,
					    out var supposedlyPulling))
				{
					supposedlyPulling.ResetLocationOnClient(connectionToClient);
				}

				break;
			}
			case true when Pulling.Component.netId != entry.Pulling:
			{
				PullSet(null, false);
				if (ComponentManager.TryGetUniversalObjectPhysics(spawned[entry.Pulling].gameObject,
					    out var supposedlyPulling))
				{
					supposedlyPulling.ResetLocationOnClient(connectionToClient);
				}

				break;
			}
		}
	}

	public void ServerCheckQueueingAndMove()
	{
		if (hasAuthority) return;
		if (MoveQueue.Count > 0)
		{
			if (CanInPutMove()) //TODO potential issue with messages building up
			{

				bool fudged = false;
				Vector3 stored = Vector3.zero;
				Dictionary<uint, NetworkIdentity> spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;
				MoveData entry = MoveQueue[0];
				MoveQueue.RemoveAt(0);

				if (entry.LastResetID != SetLastResetID) //Client hasn't been reset yet
				{
					ClientResetOnLastID(entry, spawned);
					return;
				}

				SetMatrixCache.ResetNewPosition(transform.position, registerTile);

				if (DEBUG)
				{
					Loggy.LogError(" Is Animating:" + Animating
					                                 + "\n Is floating: " + IsFloating()
					                                 + "\n move processed at" + transform.localPosition
					                                 + "\n is flying:" + IsFlyingSliding);
				}

				ServerCheckQueueingPulling(ref spawned, ref entry);
				ServerCheckClientLocation(ref entry, ref fudged, ref stored, out bool reset, out bool smooth );

				if (CanInPutMove())
				{
					var Newmove = new MoveData()
					{
						LocalPosition = entry.LocalPosition,
						Timestamp = entry.Timestamp,
						MatrixID = registerTile.Matrix.Id,
						GlobalMoveDirection = entry.GlobalMoveDirection,
						CausesSlip = false,
						Bump = false,
						LastPushID = SetTimestampID,
						Pulling = Pulling.Component.OrNull()?.netId ?? NetId.Empty,
						LastResetID = entry.LastResetID,
						IsNotMove =  entry.IsNotMove
					};

					if (TryMove(ref Newmove, gameObject, true, out var slip))
					{
						if (reset)
						{
							ResetLocationOnClients(smooth);
						}

						if (Newmove.SwappedOnMove && entry.SwappedOnMove) //Both agree
						{
							//TODO  some time it could There could be a Scenario with desynchronised If someone is changing the intenses
						}
						else if (Newmove.SwappedOnMove && entry.SwappedOnMove == false) //Client didn't predict a swap
						{
							var ServerSwapped = JsonConvert.DeserializeObject<List<uint>>(Newmove.SwappedWithIDs);

							//Send push to The person who was swapped,  Possible from here
							foreach (var Body in ServerSwapped)
							{
								var MS = Body.NetIdToGameObject().GetComponent<MovementSynchronisation>();
								MS.TargetRPCClientTilePush(this.netIdentity.connectionToClient,
									entry.GlobalMoveDirection.ToVector() * -1, CurrentTileMoveSpeed, NetId.Empty, false,
									SetTimestampID, true);
							}
						}
						else if (Newmove.SwappedOnMove == false && entry.SwappedOnMove)
						{
							//Reset Swapped thing
							//TODO
							Loggy.LogError("TODO Reset location of swapped Stuff on client ");
						}

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
									.ResetLocationOnClient(connectionToClient, true);
							}
						}

						if (entry.CausesSlip != slip)
						{
							ResetLocationOnClients();
						}

						HandleFootstepLogic();


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
								if ((entry.Timestamp + (CurrentTileMoveSpeed) < NetworkTime.time))
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

						if (Newmove.IsNotMove) //they didn't on their end so
						{
							return;
						}

						if (this.connectionToClient != null) //IDK How this could happen, since it came from a client may be for disconnected after a move , better safe than sorry
						{
							foreach (var Hit in Hits)
							{
								Hit.ResetLocationOnClient(this.connectionToClient);
							}
						}



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
					if (DEBUG)
					{
						Loggy.LogError("Failed Can input", Category.Movement);
					}
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
			var dummyMind = PlayerSpawn.NewSpawnCharacterV2(OccupationList.Instance.Occupations.PickRandom(),
				CharacterSheet.GenerateRandomCharacter());
			if (dummyMind == null || dummyMind.Body == null ||
			    dummyMind.Body.TryGetComponent<UniversalObjectPhysics>(out var physics) == false)
			{
				Loggy.LogError("Something went wrong while spawning a dummy player.");
			}
			else
			{
				physics.AppearAtWorldPositionServer(transform.position);
			}
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
			playerScript.Mind.OrNull()?.CmdSpawnPlayerGhost();
			return;
		}

		//Check to see if in container
		if (ContainedInObjectContainer != null)
		{
			if (Cooldowns.TryStartClient(playerScript, moveCooldown) == false) return;

			CMDTryEscapeContainer(PlayerAction.GetMoveAction(moveActions.Direction()));
		}
	}

	[Command]
	public void CMDTryEscapeContainer(MoveAction moveAction)
	{
		if (allowInput == false) return;
		if (ContainedInObjectContainer == null) return;

		if (Cooldowns.TryStartServer(playerScript, moveCooldown) == false) return;

		foreach (var Escape in ContainedInObjectContainer.IEscapables)
		{
			Escape.EntityTryEscape(gameObject, null, moveAction);
		}
	}

	public void HandleFootstepLogic()
	{
		if (DMMath.Prob(0.1f))
		{
			_ = Spawn.ServerPrefab(CommonPrefabs.Instance.DirtyFloorDecal,transform.position.RoundToInt());
		}


		Step = !Step;
		if (Step)
		{
			FootstepSounds.PlayerFootstepAtPosition(transform.position, this);
		}
	}


	public void AfterSuccessfulTryMove(MoveData newMoveData)
	{
		if (isServer)
		{
			if (hasAuthority && this.playerScript.OrNull()?.Equipment.OrNull()?.ItemStorage != null)
			{
				HandleFootstepLogic();
			}
		}

		var addedLocalPosition =
			(transform.position + newMoveData.GlobalMoveDirection.ToVector().To3())
			.ToLocal(MatrixManager.Get(newMoveData.MatrixID));

		newMoveData.LocalMoveDirection = VectorToPlayerMoveDirection(
			(addedLocalPosition - transform.position.ToLocal(MatrixManager.Get(newMoveData.MatrixID))).RoundTo2Int());
		//Because shuttle could be rotated   enough to make Global  Direction invalid As compared to server

		if (Pushing.Count > 0)
		{
			List<uint> netIDs = new List<uint>();
			foreach (var push in Pushing)
			{
				netIDs.Add(push.GetComponent<NetworkIdentity>().netId);
			}

			newMoveData.PushedIDs = JsonConvert.SerializeObject(netIDs);
		}
		else
		{
			newMoveData.PushedIDs = "";
		}

		//Logger.LogError(" Requested move > wth  Bump " + NewMoveData.Bump);
		CmdRequestMove(newMoveData);
	}

	private List<uint> SwappedWith = new List<uint>();


	//Not multithread Save
	public bool TryMove(ref MoveData newMoveData, GameObject byClient, bool serverProcessing, out bool causesSlip)
	{
		causesSlip = false;
		Bumps.Clear();
		Pushing.Clear();
		SwappedWith.Clear();
		Hits.Clear();
		if (CanMoveTo(newMoveData, out var causesSlipClient, Pushing, Bumps, out var pushesOff,
			    out var slippingOn))
		{
			if (serverProcessing)
			{
				if (newMoveData.IsNotMove)
				{
					return false;
				}
			}
			else
			{
				newMoveData.IsNotMove = false;
			}

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
					objectPhysics.TryTilePush(move * -1, byClient, CurrentTileMoveSpeed);
				}
				//Pushes off object for example pushing the object the other way
			}

			UniversalObjectPhysics toRemove = null;
			if (intent == Intent.Help && CurrentMovementType != MovementType.Crawling && Pulling.HasComponent == false)
			{
				var count = Pushing.Count;
				for (int i = count - 1; i >= 0; i--)
				{
					if (i >= Pushing.Count) continue;

					var toPush = Pushing[i];

					if (toPush.Intangible) continue;

					if (toPush is MovementSynchronisation player)
					{
						if (player.intent == Intent.Help)
						{
							toRemove = toPush;
							player.OnBump(this.gameObject, byClient);
							newMoveData.SwappedOnMove = true;
							SwappedWith.Add(player.netId);
						}
					}
				}

				if (toRemove != null)
				{
					Pushing.Remove(toRemove);
				}
			}

			newMoveData.SwappedWithIDs = JsonConvert.SerializeObject(SwappedWith);


			//move
			ForceTilePush(newMoveData.GlobalMoveDirection.ToVector(), Pushing, byClient,
				isWalk: true, pushedBy: this, SendWorld: true);


			SetMatrixCache.ResetNewPosition(registerTile.WorldPosition, registerTile); //Resets the cash

			if (causesSlipClient)
			{
				NewtonianPush(newMoveData.GlobalMoveDirection.ToVector(), CurrentTileMoveSpeed, Single.NaN, 4,
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
				var count = Bumps.Count;
				for (int i = count - 1; i >= 0; i--)
				{
					if (i >= Bumps.Count) continue;

					Bumps[i].OnBump(this.gameObject, byClient);
					bumpedSomething = true;
				}
			}

			IsBumping = false;

			if (serverProcessing == false)
			{
				newMoveData.IsNotMove = true;
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


		if (airTime > 0)
		{
			if (IsFlyingSliding == false)
			{
				Loggy.LogError("Error somehow have air Time while not IsFlyingSliding");
				airTime = 0;
			}
			return false;
		}

		if (slideTime > 0)
		{
			if (IsFlyingSliding == false)
			{
				Loggy.LogError("Error somehow have Slide time while not IsFlyingSliding");
				slideTime = 0;
			}
			return false;
		}
		if (allowInput == false) return false;
		if (BuckledToObject) return false;
		if (hasAuthority && UIManager.IsInputFocus) return false;
		if (IsCuffed && PulledBy.HasComponent) return false;
		if (ContainedInObjectContainer != null) return false;

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
				if (IsNotObstructed(moveAction, willPushObjects, bumps, Hits))
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

	private bool DoesSlip(MoveData moveAction, out ItemAttributesV2 slippedOn)
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
		if (isServer == false && hasAuthority && UIManager.Instance.intentControl.Running == false) return false;


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
			if (crossedItem.HasTrait(CommonTraits.Instance.BluespaceActivity))
			{
				// (Max): There's better ways to do this but due to how movement code is designed
				// you can't extend functionality that easily without bloating the code more than it already is.
				// TODO: Rework movement to be open for extension and closed for modifications.
				TeleportUtils.ServerTeleportRandom(playerScript.gameObject);
			}

			if (crossedItem.HasTrait(CommonTraits.Instance.Slippery))
			{
				slippedOn = crossedItem;
				return true;
			}
		}

		return false;
	}

	public bool IsNotObstructed(MoveData moveAction, List<UniversalObjectPhysics> pushing, List<IBumpableObject> bumps, List<UniversalObjectPhysics> hits)
	{
		var transform1 = transform.position;
		return MatrixManager.IsPassableAtAllMatricesV2(transform1,
			transform1 + moveAction.GlobalMoveDirection.ToVector().To3Int(), SetMatrixCache, this,
			pushing, bumps, hits);
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
	public void CmdRequestMove(MoveData inMoveData)
	{
		if (CanInPutMove(true) == false) return;
		var Age = NetworkTime.time - inMoveData.Timestamp;
		if (Age > MOVE_MAX_DELAY_QUEUE)
		{
			ResetLocationOnClients();
			MoveQueue.Clear();
			return;
		}

		//TODO Might be funny with changing to diagonal not too sure though
		var addedGlobalPosition =
			(transform.position.ToLocal(MatrixManager.Get(inMoveData.MatrixID)) +
			 inMoveData.LocalMoveDirection.ToVector().To3()).ToWorld(MatrixManager.Get(inMoveData.MatrixID));

		inMoveData.GlobalMoveDirection =
			VectorToPlayerMoveDirection((addedGlobalPosition - transform.position).RoundTo2Int());
		MoveQueue.Add(inMoveData);
	}

	public override void ClientTileReached(Vector3Int localPos)
	{
		if (hasAuthority == false) return;

		//Client side check for invalid tabs still open
		//(Don't need to do this server side as the interactions are validated)
		ControlTabs.CheckTabClose();
	}

	public override void LocalServerTileReached(Vector3Int localPos)
	{
		if (doStepInteractions == false) return;

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

		if (hasAuthority == false) return;

		//Client side check for invalid tabs still open
		//(Don't need to do this server side as the interactions are validated)
		ControlTabs.CheckTabClose();
	}

	private void SyncMovementType(MovementType oldType, MovementType newType)
	{
		CurrentMovementType = newType;
	}

	private void UpdatePassables()
	{
		holder.passableExclusions.Remove(needsWalking);
		holder.passableExclusions.Remove(needsRunning);

		switch (CurrentMovementType)
		{
			case MovementType.Running:
				holder.passableExclusions.Add(needsRunning);
				break;
			case MovementType.Walking:
				holder.passableExclusions.Add(needsWalking);
				break;
			default:
				return;
		}
	}
}

/// <summary>
/// Cuff state changed, provides old state and new state as 1st and 2nd args
/// </summary>
public class CuffEvent : UnityEvent<bool, bool>
{
}

/// <summary>
/// Event which fires when movement type changes (run/walk)
/// </summary>
public class MovementStateEvent : UnityEvent<bool>
{
}