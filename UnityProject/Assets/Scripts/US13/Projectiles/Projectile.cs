using UnityEngine;
using US13.HealthV2;
using US13.Items.Weapons;

namespace US13.Projectiles
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

		public abstract void Suicide(GameObject controlledByPlayer, Gun fromWeapon, MagazineBehaviour CurrentMagazine, BodyPartType targetZone = BodyPartType.Chest);

		public abstract void Shoot(Vector2 direction, GameObject controlledByPlayer, Gun fromWeapon, MagazineBehaviour Magazine, GameObject target, BodyPartType targetZone = BodyPartType.Chest);

	}
}