using System;
using UnityEngine;

namespace Weapons.Projectiles
{
	public abstract class Projectile : MonoBehaviour
	{
		public string visibleName = "bullet";

		//The original prefab name as it is changed when spawned
		private string prefabName;
		public string PrefabName => prefabName;

		public void Start()
		{
			prefabName = gameObject.name;
			gameObject.name = visibleName;
		}

		public abstract void Suicide(GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest);

		public abstract void Shoot(Vector2 direction, GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest);

	}
}