using Mirror;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public class ProjectileDecal : NetworkBehaviour, IOnHit
	{
		[SerializeField] private GameObject decal = null;

		[Tooltip("Living time of decal.")]
		[SerializeField] private float animationTime = 0;

		public bool OnHit(MatrixManager.CustomPhysicsHit hit)
		{
			if (decal == null)
			{
				Logger.LogError($"{this} on {gameObject} decal field not set in inspector!", Category.Firearms);
				return false;
			}

			RpcClientSpawn(hit.HitWorld);
			return false;
		}

		[ClientRpc]
		public void RpcClientSpawn(Vector3 WorldPosition)

		{
			var newDecal = Spawn.ClientPrefab(decal,
				WorldPosition).GameObject;
			var timeLimitedDecal = newDecal.GetComponent<TimeLimitedDecal>();
			timeLimitedDecal.SetUpDecal(animationTime);
		}
	}
}