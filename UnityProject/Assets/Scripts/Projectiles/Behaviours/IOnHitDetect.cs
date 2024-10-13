using ScriptableObjects.Gun;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Interface for processing hit (after damage) on objects with this interface, allows the object to react to what it has been hit by.
	/// Eg field generator charging when hit by laser from emitter
	/// </summary>
	public interface IOnHitDetect
	{
		void OnHitDetect(OnHitDetectData data);
	}

	/// <summary>
	/// Interface for processing hit (before damage) on objects with this interface, allows the object to react to what it is going to been hit by.
	/// Eg field generator charging when hit by laser from emitter
	/// </summary>
	public interface IOnPreHitDetect
	{
		/// <summary>
		/// Called when a bullet/laser hits an object, before the damage is taken
		/// Returning false prevents the damage and stops IOnHitDetect
		/// </summary>
		/// <param name="data"></param>
		/// <returns>returning false prevents the damage</returns>
		bool OnPreHitDetect(OnHitDetectData data);
	}

	public class OnHitDetectData
	{
		public DamageData DamageData;
		public string BulletName;
		public Vector2 BulletShootDirection;
		public Vector2 BulletShootNormal;
		public GameObject BulletObject;
		public Vector3 HitWorldPosition;

		public OnHitDetectData(DamageData data, string bulletName, Vector2 bulletShootDirection, Vector2 bulletShootNormal,
			GameObject bulletObject, Vector3 _HitWorldPosition)
		{
			DamageData = data;
			BulletName = bulletName;
			BulletShootDirection = bulletShootDirection;
			BulletShootNormal = bulletShootNormal;
			BulletObject = bulletObject;
			HitWorldPosition = _HitWorldPosition;
		}
	}
}
