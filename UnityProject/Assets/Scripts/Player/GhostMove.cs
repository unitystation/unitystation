using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Tiles;
using UnityEngine;

public class GhostMove : NetworkBehaviour, IPlayerControllable
{
	public float MoveSpeed = 8;

	public float GhostSpeedMultiplier = 8f;

	public bool Moving = false;

	private RegisterTile registerTile;

	private PlayerScript playerScript;

	private Rotatable rotatable;

	public Vector3 LocalTargetPosition;

	private bool isFaster = false;

	//TODO Change to vector move towards
	public void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
		playerScript = GetComponent<PlayerScript>();
		rotatable = GetComponent<Rotatable>();
	}

	public void OnEnable()
	{
		LocalTargetPosition = transform.localPosition;
	}

	public void Update()
	{
		var localPos = transform.localPosition;

		Moving = localPos != LocalTargetPosition;
		if (Moving)
		{
			transform.localPosition = this.MoveTowards(localPos, LocalTargetPosition,
				MoveSpeed * Time.deltaTime);
		}

		if (UIManager.IsInputFocus || PlayerManager.LocalPlayerScript.OrNull()?.IsGhost == false) return;
		if (Input.GetKeyDown(KeyCode.LeftShift) == false) return;
		isFaster = !isFaster;
		MoveSpeed = isFaster ? MoveSpeed + GhostSpeedMultiplier : MoveSpeed - GhostSpeedMultiplier;
		Chat.AddExamineMsg(gameObject, isFaster ? "You fly quickly in panic.." : "You slow down and take in the pain and sorrow..");
	}

	public Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
	{
		var magnitude = (current - target).magnitude;

		if (magnitude > 3f)
		{
			maxDistanceDelta *= (magnitude / 3);
		}

		return Vector3.MoveTowards(current, target, maxDistanceDelta);
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
			rotatable.FaceDirection(Direction);
		}
	}

	[Command]
	public void CMDSetServerPosition(Vector3 localPosition, int MatrixID, OrientationEnum Direction)
	{
		ForcePositionClient(localPosition, MatrixID, Direction, false);
	}

	[Server]
	public void ForcePositionClient(Vector3 WorldPosition)
	{
		var Matrix = MatrixManager.AtPoint(WorldPosition, isServer);
		ForcePositionClient(WorldPosition.ToLocal(Matrix), Matrix.Id, OrientationEnum.Down_By180);
	}

	[Server]
	public void ForcePositionClient(Vector3 localPosition, int MatrixID, OrientationEnum Direction, bool doOverride = true)
	{
		localPosition.z = 0;
		if (MatrixID != registerTile.Matrix.Id)
		{
			var Position = transform.position;
			registerTile.FinishNetworkedMatrixRegistration(MatrixManager.Get(MatrixID).Matrix.NetworkedMatrix);
			transform.position = Position;
		}

		var rounded = localPosition.RoundToInt();
		registerTile.ServerSetLocalPosition(rounded);
		registerTile.ClientSetLocalPosition(rounded);
		LocalTargetPosition = localPosition;
		if (Direction != OrientationEnum.Default)
		{
			rotatable.FaceDirection(Direction);
		}

		RPCUpdatePosition(localPosition, MatrixID, Direction, doOverride);

		LocalTileReached(rounded);
	}

	public void ReceivePlayerMoveAction(PlayerAction moveActions)
	{
		if (UIManager.IsInputFocus) return;
		if (moveActions.moveActions.Length == 0) return;

		if (Moving == false)
		{
			var Worlddifference = moveActions.ToPlayerMoveDirection().TVectoro().To3Int();
			var NewWorldPosition = transform.position + Worlddifference;

			var Orientation = moveActions.ToPlayerMoveDirection().TVectoro().To2Int().ToOrientationEnum();

			rotatable.FaceDirection(Orientation);

			var movetoMatrix = MatrixManager.AtPoint(NewWorldPosition, isServer).Matrix;

			if (registerTile.Matrix != movetoMatrix)
			{
				var Position = transform.position;
				registerTile.FinishNetworkedMatrixRegistration(MatrixManager.Get(movetoMatrix).Matrix.NetworkedMatrix);
				transform.position = Position;
			}

			NewWorldPosition.z = 0; //No hidden POS for us
			var LocalPosition = (NewWorldPosition).ToLocal(movetoMatrix);

			LocalTargetPosition = LocalPosition;

			registerTile.ServerSetLocalPosition(LocalPosition.RoundToInt());
			registerTile.ClientSetLocalPosition(LocalPosition.RoundToInt());
			CMDSetServerPosition(LocalPosition, movetoMatrix.Id, Orientation);
		}
	}

	public void LocalTileReached(Vector3Int localPos)
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
	}
}