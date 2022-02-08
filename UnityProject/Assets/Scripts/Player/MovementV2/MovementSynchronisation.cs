using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MovementSynchronisation : MonoBehaviour, IPlayerControllable //IPushable,
{
	public RegisterTile registerTile;

	public SpaceflightData SetSpaceflightData;

	public bool IsCurrentlyFloating;

	public MatrixCash SetMatrixCash = new MatrixCash();


	public GameObject[] ContextGameObjects = new GameObject[2];

	public void Awake()
	{
		ContextGameObjects[0] = gameObject;
	}

	public struct SpaceflightData
	{
	}


	public struct MoveData
	{
		public Vector3Int LocalPosition; //The current location of the player (  just in case they are desynchronised )
		public int MatrixID; //( The matrix the movement is on )

		public PlayerMoveDirection
			GlobalMoveDirection; //The move direction  Global (  this is for when you're on Rotated shuttles, Because you might be going down in terms of the local x,y , but on the global you're going up , So the globally is the one that is the reliable)

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


	public void Start()
	{
		double now = NetworkTime.time;
	}


	public void ClientUpdate() // Only toggled on when floating or slipping
	{
	}

	public void ReceivePlayerMoveAction(PlayerAction moveActions)
	{
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

			if (CanMoveTo(NewMoveData, out var CausesSlipClient, out var WillPush, out var PushesOff))
			{
				NewMoveData.CausesSlip = CausesSlipClient;

				if (PushesOff)
				{
					//Pushes off object for example pushing the object the other way
				}

				if (WillPush)
				{
					//Push Object
				}

				//move

				SetMatrixCash.ResetNewPosition(registerTile.WorldPosition); //Resets the cash

				if (CausesSlipClient)
				{
					//slip
				}



				IsNotFloating(null, out var NotFloating, out var CanPushOff);
				if ((NotFloating == false) || CausesSlipClient) //check if floating
				{
					//SpaceflightData Setup

					UpdateManager.Add(CallbackType.UPDATE, ClientUpdate); //If floating or slipping initiate Update Loop
				}
			}
		}
	}

	public bool
		CanInPutMoveClient(
			PlayerAction moveActions) //False for in machine/Buckled, No gravity/Nothing to push off, Is slipping, Is being thrown, Is incapacitated
	{
		return true;
	}

	public bool CanMoveTo(MoveData moveAction, out bool CausesSlipClient, out RegisterTile WillPushObject,
			out RegisterTile PushesOff) //Stuff like shuttles and machines handled in their own IPlayerControllable,
		//Space movement, normal movement ( Calling running and walking part of this )

	{
		IsNotFloating(moveAction, out bool NotFloating, out PushesOff);

		if (NotFloating)
		{
			//Need to check for Obstructions
			CausesSlipClient = false;
			WillPushObject = null;
			return true;
		}
		else
		{
			CausesSlipClient = false;
			WillPushObject = null;
			PushesOff = null;
			return false;
		}
	}


	public void IsNotFloating(MoveData? moveAction, out bool NotFloating,
		out RegisterTile CanPushOff) //Sets bool For floating
	{
		if (IsNotFloatingTileMap())
		{
			if (IsNotFloatingObjects(moveAction, out CanPushOff))
			{
				IsCurrentlyFloating = false;
				NotFloating = true;
			}
			else
			{
				IsCurrentlyFloating = true;
				NotFloating = false;
			}
		}

		CanPushOff = null;
		IsCurrentlyFloating = true;
		NotFloating = false;
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
	public void CMDRequestMove()
	{
	}
}