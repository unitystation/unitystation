using UnityEngine;

namespace Systems.MobAIs
{
	public class BlobAI : GenericHostileAI
	{
		//Null if no rally point set
		public Vector3? worldTargetPosition;

		private BlobRally blobRally;

		protected override void Awake()
		{
			base.Awake();

			blobRally = GetComponent<BlobRally>();
		}

		public void SetTarget(Vector3 worldPos)
		{
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
				moveWaitTime += Time.deltaTime;
				if (moveWaitTime >= movementTickRate)
				{
					moveWaitTime = 0f;
				}

				searchWaitTime += Time.deltaTime;
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