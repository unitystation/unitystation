using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using US13.Managers;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Tiles;
using US13.Tilemaps.Utils;
using Util;

namespace US13.Mobs.BrainAI.States.SimpleBot
{
	public class FloorBotTaskAi : SimpleBotTaskAi
	{
		[SerializeField] private LayerTile tileToPlace = null;

		public override void OnEnterState()
		{
			searchRadius = 2;
			isPerformingTask = false;
			DoTask();
		}

		protected override async UniTask PerformTask()
		{
			isPerformingTask = true;

			SoundManager.PlayNetworkedAtPos(IsEmagged ? emaggedPerformSound : taskPerformSound, LivingHealthMaster.gameObject.AssumedWorldPosServer());

			bool isCancelled = await UniTask.Delay(TimeSpan.FromSeconds(taskPerformDuration), cancellationToken: cancellationTokenSource.Token).SuppressCancellationThrow();
			isPerformingTask = false;

			if (isCancelled)
			{
				master.RemoveAddState(this, findSimpleTaskAi);
				return;
			}

			if (IsCurrentTaskValid() == true)
			{
				if(IsEmagged) targetMatrix.MetaTileMap.RemoveTileWithlayer(targetCell, LayerType.Floors);
				else targetMatrix.MetaTileMap.SetTile(targetCell, tileToPlace);
			}

			searchRadius = 1; //Look for tiles in range of current position so can retain state
			bool found = FindTarget(out targetCell, out targetMatrix);
			searchRadius = 2;

			if (found == false) master.RemoveAddState(this, findSimpleTaskAi); //If no nearby tiles, return to search state.
			else DoTask();
		}

		protected override bool IsCurrentTaskValid()
		{
			if (IsEmagged)
			{
				return Vector3.Distance(targetCell.ToWorld(targetMatrix), master.Body.gameObject.AssumedWorldPosServer()) <= 1.5f
					&& IsExposedFloorTile(targetCell, targetMatrix);
			}
			return Vector3.Distance(targetCell.ToWorld(targetMatrix), master.Body.gameObject.AssumedWorldPosServer()) <= 1.5f
			       && IsExposedBaseTile(targetCell, targetMatrix);
		}

		public override bool FindTarget(out Vector3Int targetPosition, out Matrix targetMatrixLocal)
		{
			targetMatrixLocal = master.Body.UniversalObjectPhysics.registerTile.Matrix;
			var currentPosition = master.Body.gameObject.AssumedWorldPosServer().ToLocalInt(targetMatrixLocal);

			targetPosition = currentPosition;

			for (int y = -searchRadius; y <= searchRadius; y++)
			{
				for (int x = -searchRadius; x <= searchRadius; x++)
				{
					var checkPos = currentPosition;
					checkPos.x += x;
					checkPos.y += y;

					if ((IsEmagged == false && IsExposedBaseTile(checkPos, targetMatrixLocal))
					    || (IsEmagged == true && IsExposedFloorTile(checkPos, targetMatrixLocal)))
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

		private static bool IsExposedBaseTile(Vector3Int position, Matrix matrix)
		{
			return matrix.MetaTileMap.GetTile(position, LayerType.Floors) is null &&
			       matrix.MetaTileMap.GetTile(position, LayerType.Base) is not null &&
			       matrix.MetaTileMap.IsAtmosPassableAt(position, matrix);
		}

		private static bool IsExposedFloorTile(Vector3Int position, Matrix matrix)
		{
			return matrix.MetaTileMap.GetTile(position, LayerType.Floors) is not null
			       && matrix.MetaTileMap.IsAtmosPassableAt(position, matrix);
		}
	}
}