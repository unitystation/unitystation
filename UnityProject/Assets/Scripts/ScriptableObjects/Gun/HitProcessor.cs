using UnityEngine;
using Weapons.Projectiles.Behaviours;

namespace ScriptableObjects.Gun
{
	/// <summary>
	/// Inherit from it to write your own hit processor logic for projectiles
	/// </summary>
	public abstract class HitProcessor : ScriptableObject
	{
		public abstract bool ProcessHit(RaycastHit2D hit, IOnHit[] behavioursOnBulletHit);
	}
}