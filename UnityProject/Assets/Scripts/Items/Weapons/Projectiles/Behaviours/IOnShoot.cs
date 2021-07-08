using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Interface to gather shooter information
	/// Used for logs or chat messages
	/// </summary>
	public interface IOnShoot
	{
		void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest);
	}
}