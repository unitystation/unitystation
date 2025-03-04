using System.Collections;
using Chemistry;
using Logs;
using Objects.Construction;
using UnityEngine;

namespace Mobs.BrainAI.States.SimpleBot
{
	public class CleanBotTaskAi : SimpleBotTaskAi
	{
		private FloorDecal decalToClean = null;
		[SerializeField] private Reagent reagentToSpill = null;

		public override void OnEnterState()
		{
			if (decalToClean == false)
			{
				Loggy.Error("CleanBotTaskAi: Attempted to enter state but decalToClean was null!");
				master.AddRemoveState(this, findSimpleTaskAi);
			}

			searchRadius = 5;
			taskPerformCoroutine = null;

			DoTask();
		}

		protected override IEnumerator PerformTask()
		{
			SoundManager.PlayNetworkedAtPos(isEmagged ? emaggedPerformSound : taskPerformSound, LivingHealthMaster.gameObject.AssumedWorldPosServer());
			yield return WaitFor.Seconds(taskPerformDuration);

			if (IsCurrentTaskValid() == true)
			{
				Vector3Int worldPos = targetCell.ToWorldInt(targetMatrix);

				if (isEmagged)
				{
					var mix = new ReagentMix(reagentToSpill, 5f, 273.15f);
					targetMatrix.MatrixInfo.MetaDataLayer.ReagentReact(mix,worldPos,targetCell);
				}
				else targetMatrix.MatrixInfo.MetaDataLayer.Clean(worldPos, targetCell, false);

			}

			taskPerformCoroutine = null;

			searchRadius = 1; //Search nearby tiles to see if it can continue to clean without moving
			bool found = FindTarget(out targetCell, out targetMatrix);
			searchRadius = 5;

			if (found == false) master.AddRemoveState(this, findSimpleTaskAi); //If cant clean without moving, return to search state
			else DoTask();
		}

		/// <summary>
		/// Checks to see if the target decal exists, is cleanable and is still at the recorded position
		/// </summary>
		/// <param name="positionToCheck">The assumed world position of the decal</param>
		/// <returns></returns>
		private bool IsDecalValid(Vector3 positionToCheck)
		{
			return decalToClean && decalToClean.Cleanable && Vector3.Distance(decalToClean.gameObject.AssumedWorldPosServer(), positionToCheck) < 0.1f;
		}

		protected override bool IsCurrentTaskValid()
		{
			Vector3 worldPos = targetCell.ToWorld(targetMatrix);

			return Vector3.Distance(worldPos, LivingHealthMaster.gameObject.AssumedWorldPosServer()) <= 1.1f
			       && IsDecalValid(worldPos);
		}

		public override bool FindTarget(out Vector3Int targetPosition, out Matrix targetMatrix)
		{
			this.targetMatrix = LivingHealthMaster.playerScript.RegisterPlayer.Matrix;
			var currentPosition = LivingHealthMaster.playerScript.RegisterPlayer.LocalPosition;

			targetMatrix = this.targetMatrix;
			targetPosition = currentPosition;
			decalToClean = null;

			var possibleTargets = Physics2D.OverlapCircleAll(currentPosition.ToWorld(targetMatrix), searchRadius, LayerMask.GetMask("Floor"));
			foreach (var possibleDecal in possibleTargets)
			{
				FloorDecal decal = possibleDecal.GetComponentCustom<FloorDecal>();
				if (decal == false || decal.Cleanable == false) continue;

				var worldPos = decal.gameObject.AssumedWorldPosServer();
				targetPosition = worldPos.ToLocalInt(targetMatrix);

				this.decalToClean = decal;
				return true;
			}
			return false;
		}
	}
}