using System;
using Chemistry;
using Cysharp.Threading.Tasks;
using Logs;
using UnityEngine;
using US13.Managers;
using US13.Objects.Construction.FloorDecals;
using US13.Tilemaps.Behaviours.Layers;
using Util;

namespace US13.Mobs.BrainAI.States.SimpleBot
{
	public class CleanBotTaskAi : SimpleBotTaskAi
	{
		private FloorDecal decalToClean = null;
		[SerializeField] private Reagent reagentToSpill = null;

		private Collider2D[] possibleDecals = new Collider2D[10];
		[SerializeField] private ContactFilter2D contactFilter;

		public override void OnEnterState()
		{
			if (IsEmagged == false && decalToClean == false)
			{
				Loggy.Error("CleanBotTaskAi: Attempted to enter state but decalToClean was null!");
				master.RemoveAddState(this, findSimpleTaskAi);
				return;
			}

			searchRadius = 3;
			isPerformingTask = false;

			DoTask();
		}

		protected override async UniTask PerformTask()
		{
			isPerformingTask = true;
			SoundManager.PlayNetworkedAtPos(IsEmagged ? emaggedPerformSound : taskPerformSound, LivingHealthMaster.gameObject.AssumedWorldPosServer());
			bool isCancelled = await UniTask.Delay(TimeSpan.FromSeconds(taskPerformDuration),
				cancellationToken: cancellationTokenSource.Token).SuppressCancellationThrow();
			isPerformingTask = false;

			if (isCancelled)
			{
				master.RemoveAddState(this, findSimpleTaskAi);
				return;
			}

			if (IsCurrentTaskValid() == true)
			{
				Vector3Int worldPos = targetCell.ToWorldInt(targetMatrix);

				if (IsEmagged)
				{
					var mix = new ReagentMix(reagentToSpill, 5f, 273.15f);
					targetMatrix.MatrixInfo.MetaDataLayer.ReagentReact(mix, worldPos, targetCell);
				}
				else targetMatrix.MatrixInfo.MetaDataLayer.Clean(worldPos, targetCell, false);

			}

			searchRadius = 1; //Search nearby tiles to see if it can continue to clean without moving
			bool found = FindTarget(out targetCell, out targetMatrix);
			searchRadius = 5;

			if (found == false) master.RemoveAddState(this, findSimpleTaskAi); //If cant clean without moving, return to search state
			else DoTask();
		}

		/// <summary>
		/// Checks to see if the target decal exists, is cleanable and is still at the recorded position
		/// </summary>
		/// <param name="positionToCheck">The assumed world position of the decal</param>
		/// <returns></returns>
		private bool IsDecalValid(Vector3 positionToCheck)
		{
			return decalToClean && decalToClean.Cleanable && Vector3.Distance(decalToClean.gameObject.AssumedWorldPosServer(), positionToCheck) < 1.1f;
		}

		private static bool IsAccessableAt(Vector3Int position, Matrix matrix)
		{
			return matrix.MetaDataLayer.IsOccupiedAt(position) == false;
		}

		protected override bool IsCurrentTaskValid()
		{
			if(IsEmagged)
			{
				var worldPosToSlip = targetCell.ToWorld(targetMatrix);
				return Vector3.Distance(worldPosToSlip, LivingHealthMaster.gameObject.AssumedWorldPosServer()) <= 1.5f;
			}

			Vector3 worldPos = targetCell.ToWorld(targetMatrix);

			return Vector3.Distance(worldPos, LivingHealthMaster.gameObject.AssumedWorldPosServer()) <= 1.5f
			       && IsDecalValid(worldPos);
		}

		public override bool FindTarget(out Vector3Int targetPosition, out Matrix targetMatrixLocal)
		{
			if (IsEmagged) return FindTargetEmagged(out targetPosition, out targetMatrixLocal);

			targetMatrixLocal = master.Body.UniversalObjectPhysics.registerTile.Matrix;
			var currentPosition = master.Body.gameObject.AssumedWorldPosServer().ToLocalInt(targetMatrixLocal);

			targetPosition = currentPosition;
			decalToClean = null;

			int decalCount = Physics2D.OverlapCircle(currentPosition.ToWorld(targetMatrixLocal), searchRadius, contactFilter, possibleDecals);
			for(int i = 0; i < decalCount; i++)
			{
				FloorDecal decal = possibleDecals[i].GetComponentCustom<FloorDecal>();
				if (decal == false || decal.Cleanable == false) continue;


				var worldPos = decal.gameObject.AssumedWorldPosServer();
				targetPosition = worldPos.ToLocalInt(targetMatrixLocal);

				if(IsAccessableAt(targetPosition, targetMatrixLocal) == false) continue;

				this.decalToClean = decal;
				targetMatrix = targetMatrixLocal;
				targetCell = targetPosition;
				return true;
			}

			targetMatrix = null;
			targetMatrixLocal = null;
			return false;
		}

		private bool FindTargetEmagged(out Vector3Int targetPosition, out Matrix targetMatrixLocal)
		{
			targetMatrixLocal = master.Body.UniversalObjectPhysics.registerTile.Matrix;
			var currentPosition = master.Body.gameObject.AssumedWorldPosServer().ToLocalInt(targetMatrixLocal);

			targetPosition = currentPosition;
			decalToClean = null;

			for (int y = -searchRadius; y <= searchRadius; y++)
			{
				for (int x = -searchRadius; x <= searchRadius; x++)
				{
					var checkPos = currentPosition;
					checkPos.x += x;
					checkPos.y += y;

					if (targetMatrixLocal.MetaDataLayer.IsSlipperyAt(checkPos) == false && targetMatrixLocal.MetaTileMap.IsAtmosPassableAt(checkPos, targetMatrixLocal))
					{
						targetMatrix = targetMatrixLocal;
						targetPosition = checkPos;
						targetCell = checkPos;
						return true;
					}
				}
			}

			targetMatrix = null;
			targetMatrixLocal = null;
			return false;
		}
	}
}