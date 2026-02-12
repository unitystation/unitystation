using UnityEngine;
using US13.HealthV2;
using US13.Items.Weapons;

namespace US13.Projectiles.Behaviours
{
	/// <summary>
	/// Interface to gather shooter information
	/// Used for logs or chat messages
	/// </summary>
	public interface IOnShoot
	{
		void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, MagazineBehaviour MagazineBehaviour, BodyPartType targetZone = BodyPartType.Chest);
	}
}