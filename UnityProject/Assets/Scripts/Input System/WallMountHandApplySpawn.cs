using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallMountHandApplySpawn : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	public GameObject WallMountToSpawn;
	public bool IsAPC;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TargetObject == gameObject) return false;
		//can only be used on tiles
		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		var roundTargetWorldPosition = interaction.WorldPositionTarget.RoundToInt();
		MatrixInfo matrix = MatrixManager.AtPoint(roundTargetWorldPosition, true);
		if (matrix.Matrix == null)
		{
			return;
		}
		if (!MatrixManager.IsWallAt(interaction.WorldPositionTarget.RoundToInt(), true))
		{
			return;
		}
		var localPosInt = MatrixManager.WorldToLocalInt(roundTargetWorldPosition, matrix);
		Vector3 PlaceDirection = interaction.Performer.Player().Script.WorldPos - (Vector3) roundTargetWorldPosition;
		OrientationEnum FaceDirection = OrientationEnum.Down;

		if (PlaceDirection == Vector3.up)
		{
			FaceDirection = OrientationEnum.Up;
		}
		else if (PlaceDirection == Vector3.down)
		{
			FaceDirection = OrientationEnum.Down;
		}
		else if (PlaceDirection == Vector3.right)
		{
			FaceDirection = OrientationEnum.Right;
		}
		else if (PlaceDirection == Vector3.left)
		{
			FaceDirection = OrientationEnum.Left;
		}

		if (IsAPC)
		{
			var econs = interaction.Performer.GetComponentInParent<Matrix>().GetElectricalConnections(localPosInt);
			foreach (var Connection in econs)
			{
				if (Connection.Categorytype == PowerTypeCategory.APC)
				{
					econs.Clear();
					ElectricalPool.PooledFPCList.Add(econs);
					return;
				}
			}
		}
		GameObject WallMount = Spawn.ServerPrefab(WallMountToSpawn, roundTargetWorldPosition,  interaction.Performer.transform.parent, spawnItems: false).GameObject;
		var Directional = WallMount.GetComponent<Directional>();
		Directional.FaceDirection(Orientation.FromEnum(FaceDirection));
		Inventory.ServerConsume(interaction.HandSlot, 1);
	}
}