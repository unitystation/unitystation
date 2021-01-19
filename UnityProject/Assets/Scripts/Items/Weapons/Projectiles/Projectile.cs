using System;
using UnityEngine;

namespace Weapons.Projectiles
{
	public abstract class Projectile : MonoBehaviour
	{
		public string visibleName = "bullet";
		public void Start()
		{
			gameObject.name = visibleName;
		}

		public abstract void Suicide(GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest);

		public abstract void Shoot(Vector2 direction, GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest);
	}
}