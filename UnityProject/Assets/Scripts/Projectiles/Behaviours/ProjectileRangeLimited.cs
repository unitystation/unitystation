using System;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Limits projectiles travel distance
	/// </summary>
	public class ProjectileRangeLimited : MonoBehaviour, IOnMove, ICloneble
	{
		[Tooltip("How many tiles it will travel.")]
		[SerializeField] private float maxDistance = 15;
		private float currentDistance;

		/// <summary>
		/// Invoked when the projectile reaches or exceeds its max distance (before the projectile is removed).
		/// </summary>
		public event Action OnMaxDistanceReached;

		public float CurrentDistance => currentDistance;

		public bool OnMove(Vector2 traveledDistance, Vector2 previousWorldPosition)
		{
			return AddDistance(traveledDistance.magnitude);
		}

		public void CloneTo(GameObject InCloneTo)
		{
			InCloneTo.GetComponent<ProjectileRangeLimited>().SetDistance(currentDistance);
		}


		private bool AddDistance(float distance)
		{
			currentDistance += distance;
			if (maxDistance <= currentDistance)
			{
				OnMaxDistanceReached?.Invoke();
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