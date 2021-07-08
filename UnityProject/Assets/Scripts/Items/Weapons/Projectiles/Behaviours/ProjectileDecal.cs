using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public class ProjectileDecal : MonoBehaviour, IOnHit
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

			var newDecal = Spawn.ClientPrefab(decal.name,
				hit.HitWorld).GameObject;
			var timeLimitedDecal = newDecal.GetComponent<TimeLimitedDecal>();
			timeLimitedDecal.SetUpDecal(animationTime);
			return false;
		}
	}
}