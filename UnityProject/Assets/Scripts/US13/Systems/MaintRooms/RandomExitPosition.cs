using System.Collections.Generic;
using Logs;
using UnityEngine;
using US13.Core.Physics;
using US13.Managers;
using Util;
using Event = US13.Managers.Event;

namespace US13.Systems.MaintRooms
{
	public class RandomExitPosition : MonoBehaviour
	{
		public static readonly List<GameObject> ExitMarkers = new List<GameObject>();

		public void OnEnable()
		{
			EventManager.AddHandler(Event.RoundStarted, RandomisePosition);
		}

		public void OnDisable()
		{
			EventManager.RemoveHandler(Event.RoundStarted, RandomisePosition);
		}

		public void RandomisePosition()
		{
			Loggy.Info($"Randomising gateway location... there are {ExitMarkers.Count} markers in play.");

			if (ExitMarkers.Count == 0) return;

			GetComponent<UniversalObjectPhysics>()
				.AppearAtWorldPositionServer(ExitMarkers.PickRandom().gameObject
					.AssumedWorldPosServer()); //Randomise gateway position.

			Loggy.Info($"Gateway location set to {transform.position}");
		}
	}
}
