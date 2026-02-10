using UnityEngine;
using US13.Managers.MatrixManager;
using US13.Projectiles.Behaviours;

namespace US13.ScriptableObjects.Gun
{
	/// <summary>
	/// Inherit from it to write your own hit processor logic for projectiles
	/// </summary>
	public abstract class HitProcessor : ScriptableObject
	{
		public abstract bool ProcessHit(MatrixManager.CustomPhysicsHit hit, IOnHit[] behavioursOnBulletHit);
	}
}