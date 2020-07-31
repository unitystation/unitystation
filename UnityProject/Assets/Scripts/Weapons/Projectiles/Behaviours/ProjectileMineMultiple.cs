using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	[RequireComponent(typeof(ProjectileRangeLimited))]
	public class ProjectileMineMultiple : ProjectileMine, IOnDespawn
	{
		private ProjectileRangeLimited projectileRangeLimited;

		[Tooltip("Maximum amount of tiles to hit.")]
		[SerializeField] private int maxHits = 5;
		private int currentHitCount = 0;

		private void Awake()
		{
			projectileRangeLimited = GetComponent<ProjectileRangeLimited>();
		}

		protected override bool ProcessHit(RaycastHit2D hit)
		{
			if (base.ProcessHit(hit) == false)
			{
				return true;
			}

			return ProcessCount();
		}

		private bool ProcessCount()
		{
			projectileRangeLimited.ResetDistance();

			currentHitCount++;

			if (currentHitCount < maxHits) return false;

			currentHitCount = 0;
			return true;
		}

		public void OnDespawn(RaycastHit2D hit, Vector2 point)
		{
			currentHitCount = 0;
		}
	}
}