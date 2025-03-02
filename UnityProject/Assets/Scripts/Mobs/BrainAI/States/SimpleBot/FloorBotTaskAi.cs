using System.Collections;
using AddressableReferences;
using Logs;
using NaughtyAttributes;
using Tiles;
using UnityEngine;

namespace Mobs.BrainAI.States.SimpleBot
{
	public class FloorBotTaskAi : SimpleBotTaskAi
	{
		private Matrix targetMatrix;
		private Vector3Int targetCell;

		[SerializeField] private LayerTile tileToPlace = null;

		[SerializeField] private float timeToPlace = 2.0f;

		private int searchRange = 5;

		public override void OnEnterState()
		{
			taskPerformCoroutine = null;
			OnUpdateTick();
		}

		[Button()]
		public void ToggleEmagState()
		{
			isEmagged = !isEmagged;
		}
		public override void OnExitState()
		{
			taskPerformCoroutine = null;
		}

		public override void OnUpdateTick()
		{
			if (IsTaskValid() == false)
			{
				FindNewTarget(out targetCell, out targetMatrix);
				return;
			}

			if (taskPerformCoroutine is not null) return;

			taskPerformCoroutine = StartCoroutine(PlaceRemoveTile());
		}

		private IEnumerator PlaceRemoveTile()
		{
			SoundManager.PlayNetworkedAtPos(isEmagged ? emaggedPerformSound : taskPerformSound, LivingHealthMaster.gameObject.AssumedWorldPosServer());
			yield return WaitFor.Seconds(timeToPlace);

			taskPerformCoroutine = null;

			if (IsTaskValid())
			{
				if(isEmagged) targetMatrix.MetaTileMap.RemoveTileWithlayer(targetCell, LayerType.Floors);
				else targetMatrix.MetaTileMap.SetTile(targetCell, tileToPlace);
			}

			FindNewTarget(out targetCell, out targetMatrix);
		}

		private void FindNewTarget(out Vector3Int targetCell, out Matrix matrix)
		{
			searchRange = 3;
			bool found = FindTarget(out targetCell, out matrix);
			searchRange = 5;
			if (found == false) master.AddRemoveState(this, findSimpleTaskAi);
		}

		protected override bool IsTaskValid()
		{
			if (isEmagged)
			{
				return Vector3.Distance(targetCell.ToWorld(targetMatrix), LivingHealthMaster.gameObject.AssumedWorldPosServer()) <= 1.1f
					&& isExposedFloorTile(targetCell, targetMatrix);
			}
			return Vector3.Distance(targetCell.ToWorld(targetMatrix), LivingHealthMaster.gameObject.AssumedWorldPosServer()) <= 1.1f
			       && isExposedBaseTile(targetCell, targetMatrix);
		}

		private bool isExposedBaseTile(Vector3Int position, Matrix matrix)
		{
			return matrix.MetaTileMap.GetTile(position, LayerType.Floors) is null &&
			       matrix.MetaTileMap.GetTile(position, LayerType.Base) is not null &&
					matrix.MetaTileMap.IsAtmosPassableAt(position, matrix);
		}

		private bool isExposedFloorTile(Vector3Int position, Matrix matrix)
        		{
        			return matrix.MetaTileMap.GetTile(position, LayerType.Floors) is not null &&
        					matrix.MetaTileMap.IsAtmosPassableAt(position, matrix);
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

					if ((isEmagged == false && isExposedBaseTile(checkPos, this.targetMatrix))
					    || (isEmagged == true && isExposedFloorTile(checkPos, this.targetMatrix)))
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