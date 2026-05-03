using System;
using System.Collections.Generic;
using Chemistry;
using Cysharp.Threading.Tasks;
using Logs;
using UnityEngine;
using US13.Core;
using US13.Managers;
using US13.Mobs.Traversal;
using US13.Objects.Construction.FloorDecals;
using US13.Tilemaps.Behaviours.Layers;
using Util;

namespace US13.Mobs.BrainAI.States.SimpleBot
{
	public class CleanBotTaskAi : SimpleBotTaskAi
	{
		private FloorDecal decalToClean = null;
		[SerializeField] private Reagent reagentToSpill = null;

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

			if (IsCurrentTaskValid())
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
			var path = FindTarget(out targetCell, out targetMatrix);
			searchRadius = 5;

			if (path == null || path.Count == 0) master.RemoveAddState(this, findSimpleTaskAi); //If cant clean without moving, return to search state
			else DoTask();
		}

		protected override bool IsCurrentTaskValid()
		{
			Vector3 worldPos = targetCell.ToWorld(targetMatrix);
			return Vector3.Distance(worldPos, LivingHealthMaster.gameObject.AssumedWorldPosServer()) <= 1.5f;
		}

		public override List<Vector3Int> FindTarget(out Vector3Int targetPosition, out Matrix targetMatrixLocal)
		{
			var path = FindPuddles(out targetPosition, out targetMatrixLocal);
			if (IsEmagged) return path;

			decalToClean = null;
			targetMatrix = null;

			var decals = ComponentsTracker<FloorDecal>.GetAllNearbyTypesToTarget(master.Body.gameObject, searchRadius);
			if (decals == null) return null;

			targetMatrixLocal = master.Body.UniversalObjectPhysics.registerTile.Matrix;
			var currentPosition = master.Body.gameObject.AssumedWorldPosServer().ToLocalInt(targetMatrixLocal);

			foreach(var decal in decals)
			{
				if (decal.Cleanable == false) continue;

				var worldPos = decal.gameObject.AssumedWorldPosServer();
				targetPosition = worldPos.ToLocalInt(targetMatrixLocal);

				var possiblePath = MobTraversal.GeneratePath(currentPosition, targetPosition, targetMatrixLocal, PathfinderType.AStar);
				if (possiblePath == null || possiblePath.Count == 0) continue;

				this.decalToClean = decal;
				targetMatrix = targetMatrixLocal;
				targetCell = targetPosition;
				return possiblePath;
			}

			return null;
		}

		private List<Vector3Int> FindPuddles(out Vector3Int targetPosition, out Matrix targetMatrixLocal)
		{
			targetPosition = Vector3Int.zero;
			targetMatrixLocal = master.Body.UniversalObjectPhysics.registerTile.Matrix;
			var currentPosition = master.Body.gameObject.AssumedWorldPosServer().ToLocalInt(targetMatrixLocal);

			for (int y = -searchRadius; y <= searchRadius; y++)
			{
				for (int x = -searchRadius; x <= searchRadius; x++)
				{
					var checkPos = currentPosition;
					checkPos.x += x;
					checkPos.y += y;

					if ((IsEmagged == false && targetMatrixLocal.MetaDataLayer.HasReagentSpatter(checkPos))
					    || (IsEmagged && targetMatrixLocal.MetaDataLayer.IsSlipperyAt(checkPos) == false))
					{
						var possiblePath = MobTraversal.GeneratePath(currentPosition, checkPos, targetMatrixLocal, PathfinderType.AStar);
						if (possiblePath == null || possiblePath.Count == 0) continue;

						targetMatrix = targetMatrixLocal;
						targetPosition = checkPos;
						targetCell = checkPos;
						return possiblePath;
					}
				}
			}
			return null;
		}
	}
}