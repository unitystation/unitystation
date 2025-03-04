using System.Collections;
using NaughtyAttributes;
using Tiles;
using UnityEngine;

namespace Mobs.BrainAI.States.SimpleBot
{
	public class FloorBotTaskAi : SimpleBotTaskAi
	{
		[SerializeField] private LayerTile tileToPlace = null;

		public override void OnEnterState()
		{
			searchRadius = 2;
			taskPerformCoroutine = null;
			DoTask();
		}

		[Button()]
		public void ToggleEmagState()
		{
			isEmagged = !isEmagged;
		}

		protected override IEnumerator PerformTask()
		{
			SoundManager.PlayNetworkedAtPos(isEmagged ? emaggedPerformSound : taskPerformSound, LivingHealthMaster.gameObject.AssumedWorldPosServer());
			yield return WaitFor.Seconds(taskPerformDuration);

			if (IsCurrentTaskValid() == true)
			{
				if(isEmagged) targetMatrix.MetaTileMap.RemoveTileWithlayer(targetCell, LayerType.Floors);
				else targetMatrix.MetaTileMap.SetTile(targetCell, tileToPlace);
			}

			taskPerformCoroutine = null;

			searchRadius = 1; //Look for tiles in range of current position so can retain state
			bool found = FindTarget(out targetCell, out targetMatrix);
			searchRadius = 2;

			if (found == false) master.AddRemoveState(this, findSimpleTaskAi); //If no nearby tiles, return to search state.
			else DoTask();
		}

		protected override bool IsCurrentTaskValid()
		{
			if (isEmagged)
			{
				return Vector3.Distance(targetCell.ToWorld(targetMatrix), master.Body.gameObject.AssumedWorldPosServer()) <= 1.1f
					&& IsExposedFloorTile(targetCell, targetMatrix);
			}
			return Vector3.Distance(targetCell.ToWorld(targetMatrix), master.Body.gameObject.AssumedWorldPosServer()) <= 1.1f
			       && IsExposedBaseTile(targetCell, targetMatrix);
		}

		public override bool FindTarget(out Vector3Int targetPosition, out Matrix targetMatrixLocal)
		{
			this.targetMatrix = master.Body.UniversalObjectPhysics.registerTile.Matrix;
			var currentPosition = master.Body.gameObject.AssumedWorldPosServer().ToLocalInt(this.targetMatrix);

			targetMatrixLocal = this.targetMatrix;
			targetPosition = currentPosition;

			for (int y = -searchRadius; y <= searchRadius; y++)
			{
				for (int x = -searchRadius; x <= searchRadius; x++)
				{
					var checkPos = currentPosition;
					checkPos.x += x;
					checkPos.y += y;

					if ((isEmagged == false && IsExposedBaseTile(checkPos, this.targetMatrix))
					    || (isEmagged == true && IsExposedFloorTile(checkPos, this.targetMatrix)))
					{
						targetPosition = checkPos;
						targetCell = checkPos;
						return true;
					}
				}
			}
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