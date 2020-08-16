using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Identical to ProjectileDecal.cs, but creates a decal upon despawning instead of on hitting something.
	/// </summary>
	public class ProjectileDecalOnDespawn : MonoBehaviour, IOnDespawn
	{
		[SerializeField] private GameObject decal = null;

		[Tooltip("Living time of decal.")]
		[SerializeField] private float animationTime = 0;

		public void OnDespawn(RaycastHit2D hit, Vector2 point)
		{
			if (hit.collider == null)
			{
				OnBeamEnd(point);
			}
			else
			{
				OnCollision(hit);
			}
		}

		private bool OnBeamEnd(Vector2 position)
		{
			var newDecal = Spawn.ClientPrefab(decal.name,
				position).GameObject;
			var timeLimitedDecal = newDecal.GetComponent<TimeLimitedDecal>();
			timeLimitedDecal.SetUpDecal(animationTime);
			return false;
		}

		private bool OnCollision(RaycastHit2D hit)
		{
			var newDecal = Spawn.ClientPrefab(decal.name,
				hit.point).GameObject;
			var timeLimitedDecal = newDecal.GetComponent<TimeLimitedDecal>();
			timeLimitedDecal.SetUpDecal(animationTime);
			return false;
		}
	}
}