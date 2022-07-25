using System;
using System.Collections;
using UnityEngine;

namespace Systems.Mob.MobV2.AI.CommonStates
{
	public class WanderAI : MobAIStateBase
	{

		public override IEnumerator DoAction()
		{
			Move(blackBoard.Directions.PickRandom());
			yield return base.DoAction();
		}

		private void Move(Vector3Int dirToMove)
		{
			blackBoard.ObjectPhysics.OrNull()?.TryTilePush(dirToMove.To2Int(), null);
			if (blackBoard.Rotatable != null) blackBoard.Rotatable.SetFaceDirectionLocalVector(dirToMove.To2Int());
		}
	}
}