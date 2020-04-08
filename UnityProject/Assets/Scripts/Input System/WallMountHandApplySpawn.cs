using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallMountHandApplySpawn : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	public GameObject WallMountToSpawn;
	public bool IsAPC = false;
	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TargetObject == this.gameObject) return false;
		//can only be used on tiles
		return true;
	}


	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		Vector3Int worldPosInt = interaction.WorldPositionTarget.To2Int().To3Int();
		MatrixInfo matrix = MatrixManager.AtPoint(worldPosInt, true);
		var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrix);
		if (matrix.Matrix == null) return;
		//no matrix found to place wires in

		var roundTargetWorldPosition = interaction.WorldPositionTarget.RoundToInt();
		Vector3 PlaceDirection = interaction.Performer.Player().Script.WorldPos - (Vector3) roundTargetWorldPosition;
		OrientationEnum FaceDirection = OrientationEnum.Down;
		Vector3 PlaceLocation = new Vector3();

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
		//BuildCable(roundTargetWorldPosition, interaction.Performer.transform.parent, WireEndB);
		GameObject WallMount = Spawn.ServerPrefab(WallMountToSpawn, roundTargetWorldPosition,  interaction.Performer.transform.parent).GameObject;
		//ElectricalCableMessage.Send(Cable, WireEndA, WireEndB, CableType);
		var Directional = WallMount.GetComponent<Directional>();
		Directional.FaceDirection(Orientation.FromEnum(FaceDirection));
		Inventory.ServerConsume(interaction.HandSlot, 1);
	}
}