using UnityEngine;
using US13.HealthV2;
using US13.Projectiles;
using US13.Projectiles.Behaviours;
using US13.ScriptableObjects.Gun;

namespace US13.Objects
{
	public class LaserReflectObject : MonoBehaviour, IOnHitDetect
	{
		[SerializeField]
		//Use to check whether a bullet is a laser
		private LayerMaskData laserData = null;

		public void OnHitDetect(OnHitDetectData data)
		{

			//Only reflect lasers
			if (data.BulletObject.TryGetComponent<Bullet>(out var bullet) == false || bullet.MaskData != laserData) return;

			ShootAtDirection(GetNewDirection(data), data);
		}

		private Vector2 GetNewDirection(OnHitDetectData data)
		{
			var normal = data.BulletShootNormal;
			var newDirection = data.BulletShootDirection - 2 * (data.BulletShootDirection * normal) * normal;
			return newDirection;
		}

		private void ShootAtDirection(Vector2 rotationToShoot, OnHitDetectData data)
		{
			var range = -1f;

			if (data.BulletObject.TryGetComponent<ProjectileRangeLimited>(out var rangeLimited))
			{
				range = rangeLimited.CurrentDistance;
			}

			ProjectileManager.InstantiateAndShoot(data.BulletObject.GetComponent<Bullet>().PrefabName, rotationToShoot, gameObject,
				null, BodyPartType.None, range);
		}
	}
}
