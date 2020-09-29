using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Identical to ProjectileDecalOnDespawn.cs, but only creates an object instead of doing animation time stuff. Used for reusable ammo such as foam darts.
	/// </summary>
	public class ProjectileObjectOnDespawn : MonoBehaviour, IOnDespawn
	{
		[SerializeField] private GameObject droppedObject = null;

		[Tooltip("Spawn object on collision?")]
		[SerializeField] private bool isTriggeredOnHit = true;

		public void OnDespawn(RaycastHit2D hit, Vector2 point)
		{
			if (isTriggeredOnHit && hit.collider != null)
			{
				OnBeamEnd(hit.point);
			}
			else
			{
				OnBeamEnd(point);
			}
		}

		private void OnBeamEnd(Vector2 position)
        {
            Quaternion? rot = Quaternion.Euler(0.0f, 0.0f, Random.Range(0, 360f));
            var newObject = Spawn.ServerPrefab(droppedObject.name,
            position, localRotation: rot );
        }
	}
}