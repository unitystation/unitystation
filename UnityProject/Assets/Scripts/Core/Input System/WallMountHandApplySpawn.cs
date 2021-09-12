using Objects.Construction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: namespace me
public class WallMountHandApplySpawn : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	public GameObject WallMountToSpawn;
	public bool IsWallProtrusion;

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
		if (matrix?.Matrix == null)
		{
			return;
		}
		if (!MatrixManager.IsWallAtAnyMatrix(roundTargetWorldPosition, true))
		{
			return;
		}

		Vector3Int PlaceDirection = interaction.Performer.Player().Script.WorldPos - roundTargetWorldPosition;
		OrientationEnum FaceDirection;

		//is there a wall in the direction of the new wallmount? taking into account diagonal clicking
		var tileInFront = roundTargetWorldPosition + new Vector3Int(PlaceDirection.x, 0, 0);
		if (!MatrixManager.IsWallAtAnyMatrix(tileInFront, true))
		{
			if (PlaceDirection.x > 0)
			{
				FaceDirection = OrientationEnum.Right;
			}
			else
			{
				FaceDirection = OrientationEnum.Left;
			}
		}
		else
		{
			tileInFront = roundTargetWorldPosition + new Vector3Int(0, PlaceDirection.y, 0);
			if (!MatrixManager.IsWallAtAnyMatrix(tileInFront, true))
			{
				if (PlaceDirection.y > 0)
				{
					FaceDirection = OrientationEnum.Up;
				}
				else
				{
					FaceDirection = OrientationEnum.Down;
				}
			}
			else
			{
				return;
			}
		}

		if (IsWallProtrusion)
		{
			roundTargetWorldPosition = tileInFront;
		}

		GameObject WallMount = Spawn.ServerPrefab(WallMountToSpawn, roundTargetWorldPosition,  interaction.Performer.transform.parent, spawnItems: false).GameObject;
		var Directional = WallMount.GetComponent<Directional>();
		if (Directional != null) Directional.FaceDirection(Orientation.FromEnum(FaceDirection));

		Inventory.ServerConsume(interaction.HandSlot, 1);

		var construction = WallMount.GetComponent<LightFixtureConstruction>();
		if(construction!= null)
		{
			construction.ServerSetState(LightFixtureConstruction.State.initial);
		}
	}
}
