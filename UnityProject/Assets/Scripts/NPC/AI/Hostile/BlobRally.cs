using UnityEngine;

namespace Systems.MobAIs
{
	public class BlobRally : MobPathFinder
	{
		//Null if no rally point set
		private Vector3? localTargetPosition;
		public Vector3? LocalTargetPosition => localTargetPosition;

		private int attempts = 0;

		private MobPathFinder mobPathFinder;

		private void Awake()
		{
			mobPathFinder = GetComponent<MobPathFinder>();
		}

		public void PathToRally(Vector3 worldPos)
		{
			localTargetPosition =
				MatrixManager.WorldToLocal(worldPos, MatrixManager.AtPoint(worldPos, true, registerTile.Matrix.MatrixInfo));

			var path =
				mobPathFinder.FindNewPath(
					MatrixManager.WorldToLocal(uop.OfficialPosition, registerTile.Matrix).To2Int(),
					localTargetPosition.Value.To2Int());

			if (path != null)
			{
				if (path.Count != 0)
				{
					FollowPath(path);
				}
			}
		}

		protected override void FollowCompleted()
		{
			base.FollowCompleted();

			Deactivate();
		}
	}
}