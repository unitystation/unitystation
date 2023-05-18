using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mobs.AI.States
{
	public class WanderState : MobState
	{
		protected List<Vector3Int> Directions = new List<Vector3Int>()
		{
			new Vector3Int(1, 0, 0),
			new Vector3Int(-1, 0, 0),
			new Vector3Int(0, 1, 0),
			new Vector3Int(0, -1, 0),
		};

		public override void OnEnterState(MobAI master)
		{
			// No Behavior Required
		}

		public override void OnExitState(MobAI master)
		{
			// No Behavior Required
		}

		public override void OnUpdateTick(MobAI master)
		{
			if (master.Mob.IsControlledByPlayer || HasGoal(master) == false) return;
			Move(Directions.PickRandom(), master.Mob);
		}

		public override bool HasGoal(MobAI master)
		{
			return master.CurrentActiveStates.Any(x => x.Blacklist.Contains(this)) == false;
		}

		private void Move(Vector3Int dirToMove, Mob mob)
		{
			mob.Movement.TryTilePush(dirToMove.To2Int(), null);

			if (mob.Pathfinder.Rotatable != null)
			{
				mob.Pathfinder.Rotatable.SetFaceDirectionLocalVector(dirToMove.To2Int());
			}
		}
	}
}