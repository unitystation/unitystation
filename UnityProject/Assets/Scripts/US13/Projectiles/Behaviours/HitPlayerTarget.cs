using UnityEngine;
using US13.Managers.MatrixManager;

namespace US13.Projectiles.Behaviours
{
	public class HitPlayerTarget : MonoBehaviour, ICustomHitValid
	{
		public Bullet Bullet;
		public GameObject target;

		public bool IsHitValid(MatrixManager.CustomPhysicsHit hit)
		{
			if (hit.ItHit == false) return false;
			if (hit.CollisionHit.GameObject == Bullet.Shooter && Bullet.WillHurtShooter == false) return false;

			if (hit.CollisionHit.GameObject != target)
			{
				return false;
			}

			return true;
		}
	}
}