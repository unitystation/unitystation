using NaughtyAttributes;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.HealthV2.Living;
using US13.Managers.MatrixManager;
using US13.Managers.NetworkManagement;
using US13.Projectiles.Behaviours;
using US13.Systems.Explosions;

public class ProjectileFullHeal :  MonoBehaviour, IOnHit
{

		public bool OnHit(MatrixManager.CustomPhysicsHit hit)
		{
			if (hit.CollisionHit.GameObject == null)
			{
				return true;
			}
			else
			{
				hit.CollisionHit.GameObject?.GetComponent<LivingHealthMasterBase>()?.FullyHeal();
			}

			return true;
		}


}
