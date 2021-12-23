using System.Collections;
using System.Collections.Generic;
using Doors;
using UnityEngine;

public class MobObjective : MonoBehaviour
{
	public RegisterTile MobTile;

	public Directional directional;

	public void Awake()
	{
		MobTile = GetComponent<RegisterTile>();
		directional = GetComponent<Directional>();
	}


	public float Priority; //The priority that this action should be done next

	protected List<Vector3Int> Directions = new List<Vector3Int>()
	{
		new Vector3Int(1, 0, 0),
		new Vector3Int(-1, 0, 0),
		new Vector3Int(0, 1, 0),
		new Vector3Int(0, -1, 0),
	};

	public virtual void DoAction()
	{
	}


	public virtual void ContemplatePriority()
	{
	}


	public void Move(Vector3Int dirToMove)
	{
		var dest = MobTile.LocalPositionServer + (Vector3Int)dirToMove;

		if (!MobTile.customNetTransform.Push(dirToMove.To2Int(), context: gameObject))
		{
			DoorController tryGetDoor =
				MobTile.Matrix.GetFirst<DoorController>(
					dest, true);
			if (tryGetDoor)
			{
				tryGetDoor.MobTryOpen(gameObject);
			}

			//New doors
			DoorMasterController tryGetDoorMaster =
				MobTile.Matrix.GetFirst<DoorMasterController>(
					dest, true);
			if (tryGetDoorMaster)
			{
				tryGetDoorMaster.Bump(gameObject);
			}
		}

		if (directional != null)
		{
			directional.FaceDirection(Orientation.From(dirToMove.To2Int()));
		}
	}


	public Vector3Int ChooseDominantDirection(Vector3 InD)
	{
		if (Mathf.Abs(InD.x) > Mathf.Abs(InD.y))
		{
			if (InD.x > 0)
			{
				return new Vector3Int(1, 0, 0);
			}
			else
			{
				return new Vector3Int(-1, 0, 0);
			}
		}
		else
		{
			if (InD.y > 0)
			{
				return new Vector3Int(0, 1, 0);
			}
			else
			{
				return new Vector3Int(0, -1, 0);
			}
		}
	}
}