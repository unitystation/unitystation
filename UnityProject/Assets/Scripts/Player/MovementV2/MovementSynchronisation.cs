using System;
using System.Collections;
using System.Collections.Generic;
using Items;
using Mirror;
using Objects;
using UnityEngine;

public class MovementSynchronisation : UniversalObjectPhysics, IPlayerControllable
{
	public bool IsCurrentlyFloating;
	public PlayerScript playerScript;


	public List<MoveData> MoveQueue = new List<MoveData>();

	public float MoveMaxDelayQueue = 1f;

	public override void Awake()
	{
		playerScript = GetComponent<PlayerScript>();
		base.Awake();
	}

	public void Update()
	{
		PlayerManager.SetMovementControllable(this);
	}

	public override void OnEnable()
	{
		base.OnEnable();
		UpdateManager.Add(CallbackType.UPDATE, ClientUpdate);
	}

	public  override void OnDisable()
	{
		base.OnDisable();
		UpdateManager.Remove(CallbackType.UPDATE, ClientUpdate);
	}

	public struct MoveData
	{
		public Vector3Int LocalPosition; //The current location of the player (  just in case they are desynchronised )
		public int MatrixID; //( The matrix the movement is on )

		public PlayerMoveDirection GlobalMoveDirection; //The move direction  Global (  this is for when you're on Rotated shuttles, Because you might be going down in terms of the local x,y , but on the global you're going up , So the globally is the one that is the reliable)

		public double Timestamp; // 	Timestamp with (800ms gap for being acceptable
		public bool CausesSlip; //
	}

	public enum PlayerMoveDirection
	{
		Up,
		Up_Right,
		Right,

		/* you are */Down_Right, //Dastardly
		Down,
		Down_Left,
		Left,
		Up_Left
	}


	public void Start()
	{
		double now = NetworkTime.time;
	}


	public void ClientUpdate()
	{
	}

	public double Last;

	public void ReceivePlayerMoveAction(PlayerAction moveActions)
	{
		if (IsMoving) return;
		Last = Time.timeAsDouble;
		if (moveActions.moveActions.Length == 0) return;
		SetMatrixCash.ResetNewPosition(registerTile.WorldPosition);

		if (CanInPutMoveClient(moveActions))
		{
			var NewMoveData = new MoveData()
			{
				LocalPosition = registerTile.LocalPosition,
				Timestamp = NetworkTime.time,
				MatrixID = registerTile.Matrix.Id,
				GlobalMoveDirection = moveActions.ToPlayerMoveDirection(),
				CausesSlip = false,
			};
			var PushPulls = new List<PushPull>();
			var Bumps = new List<IBumpableObject>();
			if (CanMoveTo(NewMoveData, out var CausesSlipClient, PushPulls, Bumps, out var PushesOff, out var SlippingOn))
			{
				NewMoveData.CausesSlip = CausesSlipClient;

				if (PushesOff) //space walking
				{
					if (PushesOff.TryGetComponent<UniversalObjectPhysics>(out var PhysicsObject))
					{
						var move = NewMoveData.GlobalMoveDirection.TVectoro();
						move.Normalize();
						PhysicsObject.NewtonianPush( (move * -1), TileMoveSpeed);//TODO SPEED!
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
			}
			else
			{
				foreach (var Bump in Bumps)
				{
					Bump.OnBump(this.gameObject);
				}
			}
		}
	}

	public bool CanInPutMoveClient(PlayerAction moveActions)
		//False for in machine/Buckled, No gravity/Nothing to push off, Is slipping, Is being thrown, Is incapacitated
	{
		return true;
	}

	// public bool CausesSlip

	public bool CanMoveTo(MoveData moveAction, out bool CausesSlipClient, List<PushPull> WillPushObjects, List<IBumpableObject> Bumps,
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
		var LocalTo = (registerTile.WorldPosition + moveAction.GlobalMoveDirection.TVectoro().To3Int()).ToLocal(ToMatrix)
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
		return MatrixManager.IsPassableAtAllMatricesV2(registerTile.WorldPosition,
			registerTile.WorldPosition + moveAction.GlobalMoveDirection.TVectoro().To3Int(), SetMatrixCash, this.gameObject,
			Pushing,Bumps);
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
		return MatrixManager.IsFloatingAtV2Tile(registerTile.WorldPosition, CustomNetworkManager.IsServer,
			SetMatrixCash) == false;
	}

	public bool IsNotFloatingObjects(MoveData? moveAction, out RegisterTile CanPushOff)
	{
		if (moveAction == null)
		{
			//Then just check around the area for something that Grounds
			CanPushOff = null;
			if (MatrixManager.IsFloatingAtV2Objects(ContextGameObjects, registerTile.WorldPosition,
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
			if (MatrixManager.IsNotFloatingAtV2Objects(moveAction.Value, ContextGameObjects, registerTile.WorldPosition,
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
		var Age = Time.timeAsDouble - InMoveData.Timestamp;
		if (Age > MoveMaxDelayQueue)
		{
			Logger.LogError($" Move message rejected because it is too old, Consider tweaking if ping is too high or Is being exploited Age {Age}");
			return;

		}
		MoveQueue.Add(InMoveData);
	}
}