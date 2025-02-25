using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BrainWanderState : BrainMobState
{
	protected List<Vector3Int> Directions = new List<Vector3Int>()
	{
		new Vector3Int(1, 0, 0),
		new Vector3Int(-1, 0, 0),
		new Vector3Int(0, 1, 0),
		new Vector3Int(0, -1, 0),
	};

	public override void OnEnterState()
	{
		// No Behavior Required
	}

	public override void OnExitState()
	{
		// No Behavior Required
	}

	public override void OnUpdateTick()
	{
		if (HasGoal()) return;
		Move(Directions.PickRandom(), master.Body);
	}

	public override bool HasGoal()
	{
		foreach (var state in master.CurrentActiveStates)
		{
			if (state == this) continue;
			if (state.Blacklist.Contains(this)) return false;
			if (state.HasGoal())
			{
				master.AddRemoveState(this, state);
				return false;
			}
		}
		return true;
	}

	private void Move(Vector3Int dirToMove, CommonComponents mob)
	{
		mob.UniversalObjectPhysics.TryTilePush(dirToMove.To2Int(), null);

		if (mob.Rotatable != null)
		{
			mob.Rotatable.SetFaceDirectionLocalVector(dirToMove.To2Int());
		}
	}
}
