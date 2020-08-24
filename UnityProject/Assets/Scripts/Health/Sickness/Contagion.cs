using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Health.Sickness
{
	public class Contagion: MonoBehaviour
	{
		public Sickness Sickness;

		[SerializeField]
		[Tooltip("Time (in seconds) for the contagion to despawn itself")]
		private int contagionTime;

		private float spawnedTime;

		private RegisterTile registerTile;
		
		public void Start()
		{
			spawnedTime = Time.time;
		}

		public void Update()
		{
			// Check if the contagion zone should despawn itself (after a set amount of time).
			// One day, we should hook this with the air scrubbers and general atmos system
			if (Time.time > spawnedTime + contagionTime)
			{
				// Despawns itself
				Despawn.ServerSingle(gameObject);
				Chat.AddGameWideSystemMsgToChat($"Contagion zone despawned itself");
			}
		}

		public void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
		}

		/// <summary>
		/// Called from the Walkable component.  Handles what happens when a player enters the location of the contagion.
		/// </summary>
		public void OnWalkableEnter(BaseEventData eventData)
		{
			Chat.AddGameWideSystemMsgToChat($"Player {eventData.selectedObject.name} inside contagion zone!");
		}

		/// <summary>
		/// If we want to see where the contagion is
		/// </summary>
		void OnDrawGizmos()
		{
			DebugGizmoUtils.DrawText(Sickness.SicknessName, registerTile.WorldPositionServer);
		}
	}
}
