using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public class ProjectileDecal : MonoBehaviour, IOnHit
	{
		[SerializeField] private GameObject decal = null;

		[Tooltip("Living time of decal.")]
		[SerializeField] private float animationTime = 0;

		public bool OnHit(RaycastHit2D hit)
		{
			var newDecal = Spawn.ClientPrefab(decal.name,
				hit.point).GameObject;
			var timeLimitedDecal = newDecal.GetComponent<TimeLimitedDecal>();
			timeLimitedDecal.SetUpDecal(animationTime);
			return false;
		}
	}
}