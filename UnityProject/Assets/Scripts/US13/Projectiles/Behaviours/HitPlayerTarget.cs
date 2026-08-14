using UnityEngine;
using US13.HealthV2.Living;
using US13.Managers.MatrixManager;
using US13.Player;
using US13.Projectiles;

public class HitPlayerTarget : MonoBehaviour,  ICustomHitValid
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
