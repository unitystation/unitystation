using System;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Limits projectiles travel distance
	/// </summary>
	public class ProjectileRangeLimited : MonoBehaviour, IOnShoot, IOnDespawn
	{
		private IOnDespawn[] behavioursOnDespawn;

		private Vector2 direction;
		private float projectileVelocity;

		[Tooltip("How many tiles it will travel.")]
		[SerializeField] private float maxDistance = 15;
		private float currentDistance;

		private void Awake()
		{
			behavioursOnDespawn = GetComponents<IOnDespawn>();
		}

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.direction = direction;
			projectileVelocity = weapon.ProjectileVelocity;
		}

		private void Update()
		{
			var distance = direction * (projectileVelocity * Time.deltaTime);
			AddDistance(distance.magnitude);
		}

		private void AddDistance(float distance)
		{
			currentDistance += distance;
			if (maxDistance <= currentDistance)
			{
				foreach (var behaviours in behavioursOnDespawn)
				{
					behaviours.OnDespawn();
				}
				Despawn.ClientSingle(gameObject);
			}
		}

		public void OnDespawn()
		{
			ResetDistance();
		}

		public void ResetDistance()
		{
			currentDistance = 0;
		}
	}
}