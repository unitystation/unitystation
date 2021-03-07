using ScriptableObjects.Gun;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Interface for processing hit on objects with this interface, allows the object to react to what it has been hit by.
	/// Eg field generator charging when hit by laser from emitter
	/// </summary>
	public interface IOnHitDetect
	{
		void OnHitDetect(OnHitDetectData data);
	}

	public class OnHitDetectData
	{
		public DamageData DamageData;
		public string BulletName;
		public Vector2 BulletShootDirection;
		public Vector2 BulletShootNormal;
		public GameObject BulletObject;

		public OnHitDetectData(DamageData data, string bulletName, Vector2 bulletShootDirection, Vector2 bulletShootNormal,
			GameObject bulletObject)
		{
			DamageData = data;
			BulletName = bulletName;
			BulletShootDirection = bulletShootDirection;
			BulletShootNormal = bulletShootNormal;
			BulletObject = bulletObject;
		}
	}
}
