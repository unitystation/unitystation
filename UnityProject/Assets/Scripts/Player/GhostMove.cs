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

	[SerializeField] private RegisterTile registerTile {
		get;
		private set;
	}

	[SerializeField] private PlayerScript playerScript;

	[SerializeField] private Rotatable rotatable;

	public Vector3 LocalTargetPosition;

	private bool isFaster = false;

	//TODO Change to vector move towards

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

		if (isLocalPlayer == false) return;
		if (UIManager.IsInputFocus || PlayerManager.LocalPlayerScript.OrNull()?.IsGhost == false) return;
		if (Input.GetKeyDown(KeyCode.LeftShift) == false) return;
		isFaster = !isFaster;
		MoveSpeed = isFaster ? MoveSpeed + GhostSpeedMultiplier : MoveSpeed - GhostSpeedMultiplier;
		Chat.AddExamineMsg(gameObject,
			isFaster ? "You fly quickly in panic.." : "You slow down and take in the pain and sorrow..");
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
	public void RPCUpdatePosition(Vector3 newPosition, int matrixID, OrientationEnum direction, bool @override,
		bool Smooth)
	{
		if (isLocalPlayer && @override == false || isServer) return;
		if (matrixID != registerTile.Matrix.Id)
		{
			var position = transform.position;
			registerTile.FinishNetworkedMatrixRegistration(MatrixManager.Get(matrixID).Matrix.NetworkedMatrix);
			transform.position = position;
		}

		registerTile.ServerSetLocalPosition(newPosition.RoundToInt());
		registerTile.ClientSetLocalPosition(newPosition.RoundToInt());
		LocalTargetPosition = newPosition;
		if (Smooth == false)
		{
			transform.localPosition = LocalTargetPosition;
		}

		if (direction != OrientationEnum.Default)
		{
			rotatable.FaceDirection(direction);
		}
	}

	[Command]
	public void CMDSetServerPosition(Vector3 localPosition)
	{
		ForcePositionClient(localPosition, false);
	}

	[Command]
	public void CMDSetServerPosition(Vector3 localPosition, int matrixID, OrientationEnum direction)
	{
		ForcePositionClient(localPosition, matrixID, direction, false);
	}

	[Server]
	public void ForcePositionClient(Vector3 worldPosition, bool triggerStepInterface = true, bool Smooth = true)
	{
		var matrix = MatrixManager.AtPoint(worldPosition, isServer);
		ForcePositionClient(worldPosition.ToLocal(matrix), matrix.Id, OrientationEnum.Down_By180,
			triggerStepInterface: triggerStepInterface, Smooth: Smooth);
	}

	[Server]
	public void ForcePositionClient(Vector3 localPosition, int matrixID, OrientationEnum direction,
		bool doOverride = true,
		bool triggerStepInterface = true, bool Smooth = true)
	{
		localPosition.z = 0;
		if (matrixID != registerTile.Matrix.OrNull()?.Id)
		{
			var position = transform.position;
			registerTile.FinishNetworkedMatrixRegistration(MatrixManager.Get(matrixID).Matrix.NetworkedMatrix);
			transform.position = position;
		}

		var rounded = localPosition.RoundToInt();
		registerTile.ServerSetLocalPosition(rounded);
		registerTile.ClientSetLocalPosition(rounded);
		LocalTargetPosition = localPosition;
		if (Smooth == false)
		{
			transform.localPosition = LocalTargetPosition;
		}

		if (direction != OrientationEnum.Default)
		{
			rotatable.FaceDirection(direction);
		}

		RPCUpdatePosition(localPosition, matrixID, direction, doOverride, Smooth);

		if (triggerStepInterface == false) return;

		LocalTileReached(rounded);
	}

	public void ReceivePlayerMoveAction(PlayerAction moveActions)
	{
		if (UIManager.IsInputFocus) return;
		if (moveActions.moveActions.Length == 0) return;

		if (Moving == false)
		{
			var worldDifference = moveActions.ToPlayerMoveDirection().ToVector().To3Int();
			var newWorldPosition = transform.position + worldDifference;

			var orientation = moveActions.ToPlayerMoveDirection().ToVector().ToOrientationEnum();

			rotatable.FaceDirection(orientation);

			var movetoMatrix = MatrixManager.AtPoint(newWorldPosition, isServer).Matrix;

			if (registerTile.Matrix != movetoMatrix)
			{
				var position = transform.position;
				registerTile.FinishNetworkedMatrixRegistration(MatrixManager.Get(movetoMatrix).Matrix.NetworkedMatrix);
				transform.position = position;
			}

			newWorldPosition.z = 0; //No hidden POS for us
			var localPosition = (newWorldPosition).ToLocal(movetoMatrix);

			LocalTargetPosition = localPosition;

			registerTile.ServerSetLocalPosition(localPosition.RoundToInt());
			registerTile.ClientSetLocalPosition(localPosition.RoundToInt());
			CMDSetServerPosition(localPosition, movetoMatrix.Id, orientation);
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
		var loopTo = registerTile.Matrix.MetaTileMap.ObjectLayer.EnterTileBaseList.Get(localPos);
		foreach (var enterTileBase in loopTo)
		{
			if (enterTileBase.WillAffectPlayer(playerScript) == false) continue;
			enterTileBase.OnPlayerStep(playerScript);
		}
	}
}