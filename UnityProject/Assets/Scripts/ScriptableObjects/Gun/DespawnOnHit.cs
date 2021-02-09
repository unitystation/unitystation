using UnityEngine;
using Weapons.Projectiles.Behaviours;

namespace ScriptableObjects.Gun
{
	/// <summary>
	/// Returns true always
	/// Script for applying hit behaviour
	/// and requesting object with bool, to despawn
	/// </summary>
	[CreateAssetMenu(fileName = "DespawnOnHit", menuName = "ScriptableObjects/Gun/DespawnOnHit", order = 0)]
	public class DespawnOnHit : HitProcessor
	{
		public override bool ProcessHit(MatrixManager.CustomPhysicsHit  hit, IOnHit[] behavioursOnBulletHit)
		{
			foreach (var behaviour in behavioursOnBulletHit)
			{
				behaviour.OnHit(hit);
			}

			return true;
		}
	}
}