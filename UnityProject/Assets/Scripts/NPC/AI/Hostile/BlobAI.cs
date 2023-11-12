using Blob;
using Logs;
using UnityEngine;

namespace Systems.MobAIs
{
	public class BlobAI : GenericHostileAI
	{
		//Null if no rally point set
		public Vector3? worldTargetPosition;

		private BlobRally blobRally;
		private BlobStructure blobStructure;
		public BlobStructure BlobStructure => blobStructure;

		[ContextMenu("Log world target")]
		public void LogTarget()
		{
			Loggy.LogError(worldTargetPosition.ToString());
		}

		protected override void Awake()
		{
			base.Awake();

			blobRally = GetComponent<BlobRally>();
			blobStructure = GetComponent<BlobStructure>();
		}

		public void SetTarget(Vector3 worldPos)
		{
			ResetBehaviours();
			blobRally.Deactivate();
			worldTargetPosition = worldPos;
		}

		private void PathToRally()
		{
			if(blobRally.activated || worldTargetPosition == null) return;

			blobRally.Activate();
			blobRally.PathToRally(worldTargetPosition.Value);
		}

		public override void ContemplatePriority()
		{
			if (isServer == false) return;

			if (IsDead || IsUnconscious)
			{
				HandleDeathOrUnconscious();
				blobRally.Deactivate();
				worldTargetPosition = null;
			}

			StatusLoop();
		}

		private void StatusLoop()
		{
			if (currentStatus == MobStatus.None || currentStatus == MobStatus.Attacking)
			{
				if (worldTargetPosition != null)
				{
					PathToRally();
					return;
				}

				MonitorIdleness();
				return;
			}

			if (currentStatus == MobStatus.Searching)
			{
				moveWaitTime += MobController.UpdateTimeInterval;
				if (moveWaitTime >= movementTickRate)
				{
					moveWaitTime = 0f;
				}

				searchWaitTime += MobController.UpdateTimeInterval;
				if (searchWaitTime >= searchTickRate)
				{
					searchWaitTime = 0f;
					var findTarget = SearchForTarget();
					if (findTarget != null)
					{
						BeginAttack(findTarget);
					}
					else
					{
						BeginSearch();
					}
				}
			}
		}
	}
}