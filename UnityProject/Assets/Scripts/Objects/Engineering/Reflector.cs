using System;
using Mirror;
using ScriptableObjects.Gun;
using UnityEngine;
using Weapons;
using Weapons.Projectiles;
using Weapons.Projectiles.Behaviours;

namespace Objects.Engineering
{
	public class Reflector : NetworkBehaviour, IOnHitDetect
	{
		[SerializeField]
		private ReflectorType startingState = ReflectorType.Base;
		private ReflectorType currentState = ReflectorType.Base;

		[SerializeField]
		private float startingAngle = 0;

		[SerializeField]
		private GameObject emitterBullet = null;
		private string emitterBulletName;

		private SpriteHandler spriteHandler;

		[SyncVar(hook = nameof(SyncRotation))]
		private float rotation;

		#region LifeCycle

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			emitterBulletName = emitterBullet.GetComponent<Bullet>().visibleName;
		}

		private void Start()
		{
			if (CustomNetworkManager.IsServer == false) return;

			ChangeState(startingState);
			rotation = startingAngle;
		}

		private void SyncRotation(float oldVar, float newVar)
		{
			rotation = newVar;
			transform.Rotate(0, 0, newVar);
		}

		#endregion

		private void ChangeState(ReflectorType newState)
		{
			currentState = newState;
			spriteHandler.ChangeSprite((int)newState);
		}

		#region Mathhelpers

		public static Vector2 RadianToVector2(float radian)
		{
			return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
		}
		public static Vector2 DegreeToVector2(float degree)
		{
			return RadianToVector2(degree * Mathf.Deg2Rad);
		}

		#endregion

		private enum ReflectorType
		{
			Base,
			Box,
			Double,
			Single
		}

		public void OnHitDetect(OnHitDetectData data)
		{
			//Only reflect emitter bullets
			//if (data.BulletName != emitterBulletName) return;

			if(currentState == ReflectorType.Base) return;

			switch (currentState)
			{
				//Sends all to rotation direction
				case ReflectorType.Box:
					ShootAtDirection(rotation);
					break;
				case ReflectorType.Double:
					ShootAtDirection(data.BulletShootNormal);
					break;
				case ReflectorType.Single:
					TryAngleSingle(data);
					break;
			}
		}

		private void TryAngleSingle(OnHitDetectData data)
		{
			if (Vector2.Angle(data.BulletShootNormal, DegreeToVector2(rotation)) <= 45)
			{
				ShootAtDirection(data.BulletShootNormal);
			}
		}

		private void ShootAtDirection(float rotationToShoot)
		{
			CastProjectileMessage.SendToAll(gameObject, emitterBullet, DegreeToVector2(rotationToShoot), default);
		}

		private void ShootAtDirection(Vector2 rotationToShoot)
		{
			CastProjectileMessage.SendToAll(gameObject, emitterBullet, rotationToShoot, default);
		}
	}
}
