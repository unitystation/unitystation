using UnityEngine;

namespace Systems.MobAIs
{
	public class BlobAI : GenericHostileAI
	{
		//Null if no rally point set
		private Vector3? localTargetPosition;
		public Vector3? LocalTargetPosition => localTargetPosition;

		private BlobRally blobRally;

		protected override void Awake()
		{
			base.Awake();

			blobRally = GetComponent<BlobRally>();
		}

		public void PathToRally(Vector3 worldPos)
		{
			localTargetPosition = worldPos;
			if(blobRally.activated || localTargetPosition == null) return;

			blobRally.Activate();
			blobRally.PathToRally(localTargetPosition.Value);
		}
	}
}