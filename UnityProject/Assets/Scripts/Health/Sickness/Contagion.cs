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
		private Sickness sickness;

		[SerializeField]
		[Tooltip("Time (in seconds) for the contagion to despawn itself")]
		private int contagionTime;

		private float spawnedTime;
		
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

			}
		}

		/// <summary>
		/// Called from the Walkable component.  Handles what happens when a player enters the location of the contagion.
		/// </summary>
		public void OnWalkableEnter(BaseEventData eventData)
		{
			Chat.AddGameWideSystemMsgToChat($"Player {eventData.selectedObject.name} inside contagion zone!");
		}
	}
}
