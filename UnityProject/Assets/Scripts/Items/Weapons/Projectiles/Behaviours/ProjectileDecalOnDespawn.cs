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

		[Tooltip("Spawn decal on collision?")]
		[SerializeField] private bool isTriggeredOnHit = true;

		public void OnDespawn(MatrixManager.CustomPhysicsHit hit, Vector2 point)
		{
			if (isTriggeredOnHit && hit.ItHit)
			{
				OnBeamEnd(hit.HitWorld);
			}
			else
			{
				OnBeamEnd(point);
			}
		}

		private void OnBeamEnd(Vector2 position)
		{
			var newDecal = Spawn.ClientPrefab(decal.name,
				position).GameObject;
			var timeLimitedDecal = newDecal.GetComponent<TimeLimitedDecal>();
			timeLimitedDecal.SetUpDecal(animationTime);
		}
	}
}