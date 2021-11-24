﻿using HealthV2;
using Objects;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Health.Sickness
{
	public class Contagion: MonoBehaviour, IEnterable
	{
		public Sickness Sickness;

		[SerializeField]
		[Tooltip("Time (in seconds) for the contagion to despawn itself")]
		private int contagionTime = 20;

		private float spawnedTime;

		private RegisterTile registerTile;

		public void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
		}

		public void Start()
		{
			spawnedTime = Time.time;
		}

		private void OnEnable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		//Server Side Only
		private void UpdateMe()
		{
			// Check if the contagion zone should despawn itself (after a set amount of time).
			// One day, we should hook this with the air scrubbers and general atmos system
			if (Time.time > spawnedTime + contagionTime)
			{
				_ = Despawn.ServerSingle(gameObject);
			}
		}

		/// <summary>
		/// If we want to see where the contagion is
		/// </summary>
		void OnDrawGizmos()
		{
			DebugGizmoUtils.DrawText(Sickness.SicknessName, registerTile.WorldPositionServer);
		}

		public void OnStep(GameObject eventData)
		{
			if (eventData.TryGetComponent(out PlayerHealthV2 playerHealth))
			{
				playerHealth.AddSickness(Sickness);
			}
		}

		public bool WillStep(GameObject eventData)
		{
			if (eventData.TryGetComponent<PlayerHealthV2>(out var _)) return true;
			return false;
		}
	}
}
