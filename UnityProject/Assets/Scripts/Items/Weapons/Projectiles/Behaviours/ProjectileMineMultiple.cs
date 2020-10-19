using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	[RequireComponent(typeof(ProjectileRangeLimited))]
	public class ProjectileMineMultiple : ProjectileMine
	{
		private ProjectileRangeLimited projectileRangeLimited;

		[Tooltip("Maximum amount of tiles to hit.")]
		[SerializeField] private int maxHits = 5;
		private int currentHitCount = 0;

		private void Awake()
		{
			projectileRangeLimited = GetComponent<ProjectileRangeLimited>();
		}

		public override bool Interact(MatrixManager.CustomPhysicsHit hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			if (base.Interact(hit, interactableTiles, worldPosition) == false)
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

		private void OnDisable()
		{
			currentHitCount = 0;
		}
	}
}