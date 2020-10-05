using UnityEngine;

namespace Weapons.Projectiles
{
	public abstract class Projectile : MonoBehaviour
	{
		public abstract void Suicide(GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest);

		public abstract void Shoot(Vector2 direction, GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest);
	}
}