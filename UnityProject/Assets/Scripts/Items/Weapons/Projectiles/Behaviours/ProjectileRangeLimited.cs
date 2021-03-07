﻿using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Limits projectiles travel distance
	/// </summary>
	public class ProjectileRangeLimited : MonoBehaviour, IOnMove
	{
		[Tooltip("How many tiles it will travel.")]
		[SerializeField] private float maxDistance = 15;
		private float currentDistance;

		public float CurrentDistance => currentDistance;

		public bool OnMove(Vector2 traveledDistance)
		{
			return AddDistance(traveledDistance.magnitude);
		}

		private bool AddDistance(float distance)
		{
			currentDistance += distance;
			if (maxDistance <= currentDistance)
			{
				return true;
			}

			return false;
		}

		public void ResetDistance()
		{
			currentDistance = 0;
		}

		public void SetDistance(float newDistance)
		{
			currentDistance = newDistance;
		}

		private void OnDisable()
		{
			currentDistance = 0;
		}
	}
}