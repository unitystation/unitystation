using System;
using System.Collections.Generic;
using Chemistry;
using Cysharp.Threading.Tasks;
using UnityEngine;
using US13.Core;
using US13.Health.Objects;
using US13.HealthV2.Living;
using US13.Managers;
using US13.Managers.MatrixManager;
using US13.Mobs.Traversal;
using US13.Player;
using US13.Tilemaps.Behaviours.Layers;
using Util;
using Effect = US13.Effects.Effect;

namespace US13.Mobs.BrainAI.States.SimpleBot
{

	public class FirebotTaskAi : SimpleBotTaskAi
	{
		[SerializeField] private List<PlayerHealthData> blackListedSpecies = new List<PlayerHealthData>();

		[SerializeField] private int sprayDistance = 6;
		private float SprayTileTravelDelay => 1.0f / (float)sprayDistance;
		[SerializeField] private ReagentMix fireRetardantMix;
		[SerializeField] private ReagentMix emaggedChemicalMix;

		public override void OnEnterState()
		{
			searchRadius = 5;
			isPerformingTask = false;

			DoTask();
		}

		protected override async UniTask PerformTask()
		{
			isPerformingTask = true;

			if (IsCurrentTaskValid())
			{
				Vector3Int startLocation = master.Body.gameObject.AssumedWorldPosServer().ToLocalInt(targetMatrix);
				Vector2 relative = (targetCell - startLocation).To2();
				Effect.PlayParticleDirectional(this.gameObject, relative.normalized);
				SoundManager.PlayNetworkedAtPos(IsEmagged ? emaggedPerformSound : taskPerformSound, LivingHealthMaster.gameObject.AssumedWorldPosServer(), global: false);
				SprayAreaWithReagent(targetMatrix, startLocation, targetCell.To2Int());
			}

			bool isCancelled = await UniTask
				.Delay(TimeSpan.FromSeconds(taskPerformDuration), cancellationToken: cancellationTokenSource.Token)
				.SuppressCancellationThrow();

			isPerformingTask = false;

			if (isCancelled)
			{
				master.RemoveAddState(this, findSimpleTaskAi);
				return;
			}

			searchRadius = 1; //Search nearby tiles to see if it can continue to heal without moving
			var path = FindTarget(out targetCell, out targetMatrix);
			searchRadius = 5;

			if (path == null || path.Count == 0) master.RemoveAddState(this, findSimpleTaskAi); //If cant heal without moving, return to search state
			else DoTask();
		}


		private void SprayAreaWithReagent(Matrix currentMatrix, Vector3Int startLocation, Vector2Int targetLocation)
		{
			Vector2 startLocation2D = startLocation.To2Int();
			Vector2 relative = targetLocation - startLocation2D;
			Vector2 normalVector = Vector2.Perpendicular(relative).normalized;
			Vector2 parallelStart = startLocation2D - normalVector;

			List<Vector3Int> passableTiles = GetPassableTiles(startLocation, targetLocation);
			passableTiles.AddRange(GetPassableTiles(parallelStart.To3Int(), (parallelStart + relative).RoundTo2Int()));
			parallelStart = startLocation2D + normalVector;
			passableTiles.AddRange(GetPassableTiles(parallelStart.To3Int(), (parallelStart + relative).RoundTo2Int()));

			_ = ApplyReagentWithTravelTime(currentMatrix, IsEmagged ? emaggedChemicalMix : fireRetardantMix, passableTiles);
		}

		private List<Vector3Int> GetPassableTiles(Vector3Int startLocation, Vector2Int targetLocation)
		{
			List<Vector3Int> passableTiles = new List<Vector3Int>();
			List<Vector3Int> positionList = MatrixManager.GetTiles(startLocation, targetLocation, sprayDistance);
			for (int i = 0; i < positionList.Count; i++)
			{
				if (MatrixManager.IsAtmosPassableAt(positionList[i], true) == false) return passableTiles;
				passableTiles.Add(positionList[i]);
			}
			return passableTiles;
		}

		private async UniTaskVoid ApplyReagentWithTravelTime(Matrix currentMatrix, ReagentMix reagentMix, List<Vector3Int> positionList)
		{
			for (int i = 0; i < positionList.Count; i++)
			{
				MatrixManager.ReagentReact(reagentMix, positionList[i], currentMatrix.MatrixInfo);
				await UniTask.Delay(TimeSpan.FromSeconds(SprayTileTravelDelay));
			}
		}

		protected override bool IsCurrentTaskValid()
		{
			return Vector3.Distance(targetCell.ToWorld(targetMatrix), master.Body.gameObject.AssumedWorldPosServer()) <= 3.0f;
		}

		public List<Vector3Int> FindTargetWhileEmagged(out Vector3Int targetPosition, out Matrix targetMatrixLocal)
		{
			targetMatrixLocal = null;
			targetPosition = Vector3Int.zero;
			targetMatrix = null;

			var targets = ComponentsTracker<LivingHealthMasterBase>.GetAllNearbyTypesToTarget(master.Body.gameObject, searchRadius, bypassInventories: false);
			if (targets == null) return null;

			targetMatrixLocal = master.Body.UniversalObjectPhysics.registerTile.Matrix;
			var currentPosition  = master.Body.gameObject.AssumedWorldPosServer().ToLocalInt(targetMatrixLocal);

			foreach(var living in targets)
			{
				if (living.mobID == LivingHealthMaster.mobID) continue;
				if (blackListedSpecies.Contains(living.InitialSpecies)) continue;
				if (living.FireStacks == 0) continue;

				var worldPos = living.gameObject.AssumedWorldPosServer();
				targetPosition = worldPos.ToLocalInt(targetMatrixLocal);

				var possiblePath = MobTraversal.GeneratePath(currentPosition, targetPosition, targetMatrixLocal, PathfinderType.AStar);
				if (possiblePath == null || possiblePath.Count == 0) continue;

				targetMatrix = targetMatrixLocal;
				targetCell = targetPosition;
				return possiblePath;
			}

			return null;
		}
		public override List<Vector3Int> FindTarget(out Vector3Int targetPosition, out Matrix targetMatrixLocal)
		{
			if(IsEmagged) return FindTargetWhileEmagged(out targetPosition, out targetMatrixLocal);

			targetMatrixLocal = null;
			targetPosition = Vector3Int.zero;
			targetMatrix = null;

			var targets = ComponentsTracker<Flammable>.GetAllNearbyTypesToTarget(master.Body.gameObject, searchRadius, bypassInventories: false);
			if (targets == null) return null;

			targetMatrixLocal = master.Body.UniversalObjectPhysics.registerTile.Matrix;
			var currentPosition  = master.Body.gameObject.AssumedWorldPosServer().ToLocalInt(targetMatrixLocal);

			foreach(var flammable in targets)
			{
				if (flammable.IsOnFire == false) continue;
				var worldPos = flammable.gameObject.AssumedWorldPosServer();
				targetPosition = worldPos.ToLocalInt(targetMatrixLocal);

				var possiblePath = MobTraversal.GeneratePath(currentPosition, targetPosition, targetMatrixLocal, PathfinderType.AStar);
				if (possiblePath == null || possiblePath.Count == 0) continue;

				targetMatrix = targetMatrixLocal;
				targetCell = targetPosition;
				return possiblePath;
			}

			return FindHotspots(targetMatrixLocal, in currentPosition, out targetPosition);
		}

		private List<Vector3Int> FindHotspots(in Matrix targetMatrixLocal, in Vector3Int currentPosition, out Vector3Int targetPosition)
		{
			targetPosition = currentPosition;
			for (int y = -searchRadius; y <= searchRadius; y++)
			{
				for (int x = -searchRadius; x <= searchRadius; x++)
				{
					var checkPos = currentPosition + new Vector3Int(x, y, 0);

					if (targetMatrixLocal.ReactionManager?.HasHotspot(checkPos) == false) continue;
					var possiblePath = MobTraversal.GeneratePath(currentPosition, checkPos, targetMatrixLocal, PathfinderType.AStar);
					if (possiblePath == null || possiblePath.Count == 0) continue;

					targetMatrix = targetMatrixLocal;
					targetPosition = checkPos;
					targetCell = checkPos;
					return possiblePath;

				}
			}
			targetMatrix = null;
			return null;
		}
	}
}