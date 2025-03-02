using System.Collections;
using System.Linq;
using AddressableReferences;
using Chemistry;
using Logs;
using Objects.Construction;
using Tiles;
using UnityEngine;
using Reagent = Systems.Botany.Reagent;

namespace Mobs.BrainAI.States.SimpleBot
{
	public class CleanBotTaskAi : SimpleBotTaskAi
	{
		private RegisterTile tileToClean = null;
		[SerializeField] private float timeToClean = 2.0f;
		[SerializeField] private Chemistry.Reagent reagentToSpill = null;

		private Matrix targetMatrix;
		private Vector3Int targetCell;
		private int searchRange = 5;

		public override void OnEnterState()
		{
			if (tileToClean == null)
			{
				Loggy.Error("CleanBotTaskAi: Attemped to enter state but tileToClean was null!");
				master.AddRemoveState(this, findSimpleTaskAi);
			}
		}

		public override void OnExitState()
		{
			tileToClean = null;
			taskPerformCoroutine = null;
		}

		public override void OnUpdateTick()
		{
			if (IsTaskValid() == false)
			{
				master.AddRemoveState(this, findSimpleTaskAi);
				return;
			}

			if (taskPerformCoroutine is not null) return;
			taskPerformCoroutine = StartCoroutine(CleanMessTile());

		}

		private IEnumerator CleanMessTile()
		{
			SoundManager.PlayNetworkedAtPos(isEmagged ? emaggedPerformSound : taskPerformSound, LivingHealthMaster.gameObject.AssumedWorldPosServer());
			yield return WaitFor.Seconds(timeToClean);

			taskPerformCoroutine = null;
			master.AddRemoveState(this, findSimpleTaskAi);

			if (IsTaskValid() == false)
			{
				FindNewTarget(out targetCell, out targetMatrix);
				yield break;
			}

			var matrixInfo = master.Body.RegisterTile.Matrix.MatrixInfo;
			Vector3Int worldPos = master.RelatedPart.HealthMaster.gameObject.AssumedWorldPosServer().CutToInt();

			if (isEmagged)
			{
				var mix = new ReagentMix(reagentToSpill, 5f, 273.15f);
				matrixInfo.MetaDataLayer.ReagentReact(mix,worldPos,tileToClean.LocalPosition);
			}
			else matrixInfo.MetaDataLayer.Clean(worldPos, tileToClean.LocalPosition, false);

			FindNewTarget(out targetCell, out targetMatrix);
		}

		private bool DoesTileNeedCleaning(Vector3Int positionToCheck)
		{
			return targetMatrix.Get<FloorDecal>(positionToCheck, true).Any(p => p.Cleanable);
		}

		protected override bool IsTaskValid()
		{
			return Vector3.Distance(targetCell.ToWorld(targetMatrix), LivingHealthMaster.gameObject.AssumedWorldPosServer()) <= 1.1f
			       && DoesTileNeedCleaning(targetCell);
		}

		private void FindNewTarget(out Vector3Int targetCell, out Matrix matrix)
		{
			bool found = FindTarget(out targetCell, out matrix);

			if (found == false) master.AddRemoveState(this, findSimpleTaskAi);
		}


		public override bool FindTarget(out Vector3Int targetPosition, out Matrix targetMatrix)
		{
			int bound = (int)(searchRange / 2);
			targetPosition = Vector3Int.zero;

			this.targetMatrix = LivingHealthMaster.RegisterTile.Matrix;
			targetMatrix = this.targetMatrix;

			var currentPosition = LivingHealthMaster.RegisterTile.LocalPosition;
			for (int y = bound; y > -1 - bound; y--)
			{
				for (int x = -bound; x < 1 + bound; x++)
				{
					var checkPos = currentPosition;
					checkPos.x += x;
					checkPos.y += y;

					if (DoesTileNeedCleaning(checkPos))
					{
						targetPosition = checkPos;
						targetCell = checkPos;
						return true;
					}
				}
			}
			return false;
		}
		public override bool HasGoal()
		{
			return true;
		}
	}
}