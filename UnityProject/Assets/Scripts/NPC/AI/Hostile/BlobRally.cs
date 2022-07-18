using UnityEngine;

namespace Systems.MobAIs
{
	public class BlobRally : MobPathFinder
	{
		//Null if no rally point set
		private Vector3 localTargetPosition;

		private BlobAI blobAI;

		public override void Awake()
		{
			base.Awake();

			blobAI = GetComponent<BlobAI>();
		}

		public void PathToRally(Vector3 worldPos)
		{
			localTargetPosition =
				MatrixManager.WorldToLocal(worldPos, MatrixManager.AtPoint(worldPos, true, registerTile.Matrix.MatrixInfo));

			var path = FindNewPath(
				MatrixManager.WorldToLocal(uop.OfficialPosition, registerTile.Matrix).To2Int(),
					localTargetPosition.To2Int());

			if (path != null)
			{
				if (path.Count != 0)
				{
					FollowPath(path);
					return;
				}
			}

			Deactivate();
		}

		protected override void FollowCompleted()
		{
			base.FollowCompleted();

			Deactivate();
		}

		protected override void OnTileReached(Vector3 tilePos)
		{
			base.OnTileReached(tilePos);

			// Get to two tiles around the point before completion
			if((tilePos - localTargetPosition).sqrMagnitude > 4) return;

			FollowCompleted();
		}

		public override void Deactivate()
		{
			base.Deactivate();
			blobAI.worldTargetPosition = null;
		}
	}
}