using UnityEngine;
using US13.Managers.MatrixManager;
using US13.Projectiles.Behaviours;

namespace US13.ScriptableObjects.Gun
{
	/// <summary>
	/// Requests to despawn game object only if a hit behaviour returns true
	/// </summary>
	[CreateAssetMenu(fileName = "DespawnOnRequest", menuName = "ScriptableObjects/Gun/DespawnOnRequest", order = 0)]
	public class DespawnOnRequest : HitProcessor
	{
		public override bool ProcessHit(MatrixManager.CustomPhysicsHit hit, IOnHit[] behavioursOnBulletHit)
		{
			var isRequesting = false;
			foreach (var behaviour in behavioursOnBulletHit)
			{
				if (behaviour.OnHit(hit))
				{
					isRequesting = true;
				}
			}

			return isRequesting;
		}
	}
}