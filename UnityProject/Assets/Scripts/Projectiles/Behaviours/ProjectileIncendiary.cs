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

		[SerializeField]
		private bool changeTemperatureOnHotspot = true;

		private Transform thisTransform;

		private void Awake()
		{
			thisTransform = transform;
		}

		public bool OnMove(Vector2 traveledDistance, Vector2 previousWorldPosition)
		{
			if (createsHotspots == false) return false;

			var currentTileWorldPos = thisTransform.position.RoundToInt();
			var previousTilePos = previousWorldPosition.RoundToInt();

			var amount = Mathf.RoundToInt(traveledDistance.y);
			if (amount != 0)
			{
				var xDifference = (currentTileWorldPos.x - previousTilePos.x) / amount;
				var yDifference = (currentTileWorldPos.y - previousTilePos.y) / amount;

				for (int i = 0; i < amount; i++)
				{
					CreateHotSpot(new Vector3Int(xDifference * i + previousTilePos.x,
						yDifference * i + previousTilePos.y, 0));
				}
			}

			if (amount == 0)
			{
				CreateHotSpot(currentTileWorldPos);
			}

			return false;
		}

		public bool OnHit(MatrixManager.CustomPhysicsHit hit)
		{
			if (setsMobsOnFire == false) return false;

			if (hit.CollisionHit.GameObject == null) return true;

			if (hit.CollisionHit.GameObject.TryGetComponent(out LivingHealthMasterBase livingHealth))
			{
				livingHealth.ChangeFireStacks(fireStacksToGive);
			}

			return true;
		}

		private void CreateHotSpot(Vector3Int tilePos)
		{
			var reactionManager = MatrixManager.AtPoint(tilePos, true).ReactionManager;
			reactionManager.ExposeHotspotWorldPosition(tilePos.To2Int(), fireHotspotTemperature, changeTemperatureOnHotspot);
		}
	}
}
