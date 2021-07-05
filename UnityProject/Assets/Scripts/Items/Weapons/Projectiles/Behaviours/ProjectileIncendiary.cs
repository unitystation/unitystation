using System.Collections;
using HealthV2;
using UnityEngine;
using NaughtyAttributes;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Can create a trail of hotspots for each tile the projectile visits.
	/// Can set mobs on fire.
	/// </summary>
	public class ProjectileIncendiary : MonoBehaviour, IOnMove, IOnHit
	{
		[SerializeField]
		private bool createsHotspots = true;
		[SerializeField]
		private bool setsMobsOnFire = true;
		[SerializeField, ShowIf(nameof(setsMobsOnFire)), Range(1, 10)]
		private int fireStacksToGive = 4;

		[SerializeField]
		private float fireHotspotTemperature = 700;

		private Transform thisTransform;

		private Vector3Int currentTileWorldPos = default;
		private Vector3Int previousTileWorldPos = default;

		private void Awake()
		{
			thisTransform = transform;
		}

		public bool OnMove(Vector2 traveledDistance)
		{
			if (createsHotspots == false) return false;

			currentTileWorldPos = thisTransform.position.CutToInt();
			if (currentTileWorldPos == previousTileWorldPos) return false;
			previousTileWorldPos = currentTileWorldPos;

			CreateHotSpot();

			return false;
		}

		public bool OnHit(MatrixManager.CustomPhysicsHit hit)
		{
			if (setsMobsOnFire == false) return false;

			if (hit.CollisionHit.GameObject == null) return true;

			//TODO REMOVE AFTER MOBS ARE MOVED TO NEW HEALTH
			if (hit.CollisionHit.GameObject.TryGetComponent(out LivingHealthMasterBase health))
			{
				health.ChangeFireStacks(fireStacksToGive);
			}

			if (hit.CollisionHit.GameObject.TryGetComponent(out LivingHealthMasterBase healthMasterBase))
			{
				healthMasterBase.ChangeFireStacks(fireStacksToGive);
			}

			return true;
		}

		private void CreateHotSpot()
		{
			var reactionManager = MatrixManager.AtPoint(currentTileWorldPos, true).ReactionManager;
			reactionManager.ExposeHotspotWorldPosition(currentTileWorldPos.To2Int(), fireHotspotTemperature, true);
		}
	}
}
