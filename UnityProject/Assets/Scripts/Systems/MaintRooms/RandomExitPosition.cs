using System.Collections;
using System.Collections.Generic;
using Core.Physics;
using Logs;
using Systems.Scenes;
using TileManagement;
using UnityEngine;

namespace MaintRooms
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
