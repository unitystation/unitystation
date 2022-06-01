using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GhostMove : NetworkBehaviour, IPlayerControllable
{
	public float MoveSpeed = 8;

	public bool Moving = false;

	public RegisterTile registerTile;

	public Rotatable Rotate;

	public Vector3 LocalTargetPosition;

	//TODO Change to vector move towards
	public void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
		Rotate = GetComponent<Rotatable>();
	}

	public void OnEnable()
	{
		LocalTargetPosition = transform.localPosition;
	}

	public void Update()
	{
		var LocalPOS = transform.localPosition;

		Moving = LocalPOS != LocalTargetPosition;
		if (Moving)
		{
			transform.localPosition = this.MoveTowards(LocalPOS, LocalTargetPosition,
				MoveSpeed * Time.deltaTime);
		}
	}

	public Vector3 MoveTowards(
		Vector3 current,
		Vector3 target,
		float maxDistanceDelta)
	{
		var magnitude = (current - target).magnitude;
		if (magnitude > 7f)
		{
			maxDistanceDelta *= 40;
		}
		else if (magnitude > 3f)
		{
			maxDistanceDelta *= 10;
		}

		return Vector3.MoveTowards(current, target,
			maxDistanceDelta);
	}

	[ClientRpc]
	public void RPCUpdatePosition(Vector3 NewPosition, int MatrixID, OrientationEnum Direction, bool Override)
	{
		if (isLocalPlayer && Override == false || isServer)  return;
		if (MatrixID != registerTile.Matrix.Id)
		{
			var Position = transform.position;
			registerTile.FinishNetworkedMatrixRegistration(MatrixManager.Get(MatrixID).Matrix.NetworkedMatrix);
			transform.position = Position;
		}

		registerTile.ServerSetLocalPosition(NewPosition.RoundToInt());
		registerTile.ClientSetLocalPosition(NewPosition.RoundToInt());
		LocalTargetPosition = NewPosition;
		if (Direction != OrientationEnum.Default)
		{
			Rotate.FaceDirection(Direction);
		}
	}

	[Command]
	public void CMDSetServerPosition(Vector3 localPosition, int MatrixID, OrientationEnum Direction)
	{
		localPosition.z = 0;
		if (MatrixID != registerTile.Matrix.Id)
		{
			var Position = transform.position;
			registerTile.FinishNetworkedMatrixRegistration(MatrixManager.Get(MatrixID).Matrix.NetworkedMatrix);
			transform.position = Position;
		}
		registerTile.ServerSetLocalPosition(localPosition.RoundToInt());
		registerTile.ClientSetLocalPosition(localPosition.RoundToInt());
		LocalTargetPosition = localPosition;
		Rotate.FaceDirection(Direction);
		RPCUpdatePosition(localPosition, MatrixID, Direction, false);
	}


	public void ForcePositionClient(Vector3 localPosition, int MatrixID, OrientationEnum Direction)
	{
		localPosition.z = 0;
		if (MatrixID != registerTile.Matrix.Id)
		{
			var Position = transform.position;
			registerTile.FinishNetworkedMatrixRegistration(MatrixManager.Get(MatrixID).Matrix.NetworkedMatrix);
			transform.position = Position;
		}
		registerTile.ServerSetLocalPosition(localPosition.RoundToInt());
		registerTile.ClientSetLocalPosition(localPosition.RoundToInt());
		LocalTargetPosition = localPosition;
		if (Direction != OrientationEnum.Default)
		{
			Rotate.FaceDirection(Direction);
		}

		RPCUpdatePosition(localPosition, MatrixID, Direction, true);
	}

	public void ReceivePlayerMoveAction(PlayerAction moveActions)
	{
		if (moveActions.moveActions.Length == 0) return;

		if (Moving == false)
		{
			var NewWorldPosition = transform.position + moveActions.ToPlayerMoveDirection().TVectoro().To3Int();

			var Orientation = moveActions.ToPlayerMoveDirection().TVectoro().To2Int().ToOrientationEnum();

			Rotate.FaceDirection(Orientation);

			var movetoMatrix = MatrixManager.AtPoint(NewWorldPosition, isServer).Matrix;

			if (registerTile.Matrix != movetoMatrix)
			{
				var Position = transform.position;
				registerTile.FinishNetworkedMatrixRegistration(MatrixManager.Get(movetoMatrix).Matrix.NetworkedMatrix);
				transform.position = Position;
			}

			LocalTargetPosition = NewWorldPosition;

			var LocalPosition = (NewWorldPosition).ToLocal(movetoMatrix);

			registerTile.ServerSetLocalPosition(LocalPosition.RoundToInt());
			registerTile.ClientSetLocalPosition(LocalPosition.RoundToInt());
			CMDSetServerPosition(LocalPosition, movetoMatrix.Id, Orientation);
		}
	}
}