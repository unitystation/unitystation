using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mobs;
using Mobs.BrainAI;
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
		if ( HasGoal() == false) return;
		Move(Directions.PickRandom(), master.Body);
	}

	public override bool HasGoal()
	{
		return master.CurrentActiveStates.Any(x => x.Blacklist.Contains(this)) == false;
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
