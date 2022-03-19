using System;
using System.Collections;
using System.Collections.Generic;
using Items;
using Mirror;
using Objects;
using UnityEngine;

public class MovementSynchronisation : UniversalObjectPhysics, IPlayerControllable, ICooldown
{
	public bool IsCurrentlyFloating;
	public PlayerScript playerScript;


	public List<MoveData> MoveQueue = new List<MoveData>();

	public float MoveMaxDelayQueue = 1f;

	public float DefaultTime { get; } = 0.5f;

	public override void Awake()
	{
		playerScript = GetComponent<PlayerScript>();
		base.Awake();
	}

	public void Update()
	{
		CheckQueueingAndMove();
		if (isLocalPlayer == false) return;
		PlayerManager.SetMovementControllable(this);
	}

	public override void OnEnable()
	{
		base.OnEnable();
		if (isServer == false) return;
		UpdateManager.Add(CallbackType.UPDATE, CheckQueueingAndMove);
	}

	public override void OnDisable()
	{
		base.OnDisable();
		if (isServer == false) return;
		UpdateManager.Remove(CallbackType.UPDATE, CheckQueueingAndMove);
	}

	public struct MoveData
	{
		public Vector3Int LocalPosition; //The current location of the player (  just in case they are desynchronised )
		public int MatrixID; //( The matrix the movement is on )

		public PlayerMoveDirection GlobalMoveDirection;

		public PlayerMoveDirection
			LocalMoveDirection; //because you want the change in movement to be same across server and client

		public double Timestamp; // 	Timestamp with (800ms gap for being acceptable
		public bool CausesSlip; //
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

	public void Start()
	{
		LastProcessMoved = NetworkTime.time;
	}


	public double LastProcessMoved;

	public void CheckQueueingAndMove()
	{
		if (isLocalPlayer) return;

		if (MoveQueue.Count > 0)
		{
			var Entry = MoveQueue[0];
			MoveQueue.RemoveAt(0);
			if (LastProcessMoved > Entry.Timestamp)
			{
				Logger.LogError("Potentially Out of order message ");
				return;
			}
			SetMatrixCash.ResetNewPosition(transform.position);
			if (CanInPutMove())
			{
				if (TryMove(Entry))
				{
					//do calculation is and set targets and stuff
					//Reset client if movement failed Since its good movement only Getting sent
					//if there's enough time to do The next movement to the current time, Then process it instantly
					//Like,  it takes 1 to do movement
					//timestamp says 0 for the first, 1 For the second
					//the current server timestamp is 2
					//So that means it can do 1 and 2 Messages , in the same frame
					if (MoveQueue.Count > 0 &&
					    (Entry.Timestamp + (TileMoveSpeed) <
					     NetworkTime.time)) //yes Time.timeAsDouble Can rollover but this would only be a problem for a second
					{
						transform.localPosition = LocalTargetPosition;
						CheckQueueingAndMove();
					}
				}
				else
				{
					ForceSetPosition(transform.localPosition);
					//TODO RESET!!
				}
			}
			else
			{
				//TODO RESET!!
				ForceSetPosition(transform.localPosition);
				MoveQueue.Clear();
			}
		}
	}


	public void ReceivePlayerMoveAction(PlayerAction moveActions)
	{
		if (IsMoving) return;
		if (moveActions.moveActions.Length == 0) return;
		SetMatrixCash.ResetNewPosition(transform.position);

		if (CanInPutMove())
		{
			var NewMoveData = new MoveData()
			{
				LocalPosition = registerTile.LocalPosition,
				Timestamp = NetworkTime.time,
				MatrixID = registerTile.Matrix.Id,
				GlobalMoveDirection = moveActions.ToPlayerMoveDirection(),
				CausesSlip = false,
			};
			if (TryMove(NewMoveData))
			{
				if (isServer) return;
				CMDRequestMove(NewMoveData);
			}
		}
	}


	public bool TryMove(MoveData NewMoveData)
	{
		var PushPulls = new List<PushPull>();
		var Bumps = new List<IBumpableObject>();
		if (CanMoveTo(NewMoveData, out var CausesSlipClient, PushPulls, Bumps, out var PushesOff,
			    out var SlippingOn))
		{
			NewMoveData.CausesSlip = CausesSlipClient;

			if (PushesOff) //space walking
			{
				if (PushesOff.TryGetComponent<UniversalObjectPhysics>(out var PhysicsObject))
				{
					var move = NewMoveData.GlobalMoveDirection.TVectoro();
					move.Normalize();
					PhysicsObject.NewtonianPush((move * -1), TileMoveSpeed); //TODO SPEED!
				}
				//Pushes off object for example pushing the object the other way
			}

			//move
			ForceTilePush(NewMoveData.GlobalMoveDirection.TVectoro().To2Int(), PushPulls); //TODO Speed

			SetMatrixCash.ResetNewPosition(registerTile.WorldPosition); //Resets the cash

			if (CausesSlipClient)
			{
				//SlippingOn
				//slip //TODO
			}


			if (IsNotFloating(null, out _) == false || CausesSlipClient) //check if floating
			{
				var move = NewMoveData.GlobalMoveDirection.TVectoro();
				move.Normalize();
				newtonianMovement += move * TileMoveSpeed;
			}

			NewMoveData.LocalMoveDirection =
				VectorToPlayerMoveDirection((LocalTargetPosition - transform.localPosition).RoundToInt().To2Int());

			return true;
		}
		else
		{
			bool BumpedSomething = false;
			if (Cooldowns.TryStart(playerScript, this))
			{
				foreach (var Bump in Bumps)
				{
					Bump.OnBump(this.gameObject);
					BumpedSomething = true;
				}
			}


			return BumpedSomething;
		}
	}

	public bool CanInPutMove()
		//False for in machine/Buckled, No gravity/Nothing to push off, Is slipping, Is being thrown, Is incapacitated
	{
		return true;
	}

	// public bool CausesSlip

	public bool CanMoveTo(MoveData moveAction, out bool CausesSlipClient, List<PushPull> WillPushObjects,
			List<IBumpableObject> Bumps,
			out RegisterTile PushesOff,
			out ItemAttributesV2 slippedOn) //Stuff like shuttles and machines handled in their own IPlayerControllable,
		//Space movement, normal movement ( Calling running and walking part of this )

	{
		if (IsNotFloating(moveAction, out PushesOff))
		{
			//Need to check for Obstructions
			if (IsNotObstructed(moveAction, WillPushObjects, Bumps))
			{
				CausesSlipClient = DoesSlip(moveAction, out slippedOn);
				return true;
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
		foreach (var itemSlot in playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.feet))
		{
			if (itemSlot.ItemAttributes == null ||
			    itemSlot.ItemAttributes.HasTrait(CommonTraits.Instance.NoSlip) == false)
			{
				slipProtection = false;
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

	public bool IsNotObstructed(MoveData moveAction, List<PushPull> Pushing, List<IBumpableObject> Bumps)
	{
		var transform1 = transform.position;
		return MatrixManager.IsPassableAtAllMatricesV2(transform1,
			transform1 + moveAction.GlobalMoveDirection.TVectoro().To3Int(), SetMatrixCash, this.gameObject,
			Pushing, Bumps);
	}


	public bool IsNotFloating(MoveData? moveAction,
		out RegisterTile CanPushOff) //Sets bool For floating
	{
		if (stickyMovement)
		{
			if (newtonianMovement.magnitude > maximumStickSpeed)
			{
				IsCurrentlyFloating = true;
				CanPushOff = null;
				return false;
			}
		}


		if (IsNotFloatingTileMap())
		{
			if (stickyMovement) newtonianMovement *= 0;
			IsCurrentlyFloating = false;
			CanPushOff = null;
			return true;
		}

		if (IsNotFloatingObjects(moveAction, out CanPushOff))
		{
			if (stickyMovement) newtonianMovement *= 0;
			IsCurrentlyFloating = false;
			return true;
		}

		IsCurrentlyFloating = true;
		return false;
	}


	public bool IsNotFloatingTileMap()
	{
		return MatrixManager.IsFloatingAtV2Tile(transform.position.RoundToInt(), CustomNetworkManager.IsServer,
			SetMatrixCash) == false;
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
			if (MatrixManager.IsNotFloatingAtV2Objects(moveAction.Value, ContextGameObjects, transform.position.RoundToInt(),
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
		if (CanInPutMove())
		{
			var Age = NetworkTime.time - InMoveData.Timestamp;
			if (Age > MoveMaxDelayQueue)
			{
				Logger.LogError(
					$" Move message rejected because it is too old, Consider tweaking if ping is too high or Is being exploited Age {Age}");
				return;
			}

			MoveQueue.Add(InMoveData);
		}
	}
}