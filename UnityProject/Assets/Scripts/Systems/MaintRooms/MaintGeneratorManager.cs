using System;
using System.Collections.Generic;
using Core;
using UnityEngine;
using Gateway;
using Logs;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

namespace Systems.Scenes
{
	public class MaintGeneratorManager : MonoBehaviour
	{
		internal static readonly List<MaintGenerator> MaintGenerators = new List<MaintGenerator>();
		internal static readonly List<GameObject> possibleExits = new List<GameObject>();

		[SerializeField] private GameObject gateway;

		private void Awake()
		{
			TransportUtility.MaintRoomLocations.Clear();

			if (CustomNetworkManager.IsServer == false) return;
			MaintGenerators.Clear();
			possibleExits.Clear();
		}

		public void OnEnable()
		{
			EventManager.AddHandler(Event.ScenesLoadedServer, GenerateMaints);
			EventManager.AddHandler(Event.RoundStarted, PostStart);
		}

		public void OnDisable()
		{
			EventManager.RemoveHandler(Event.ScenesLoadedServer, GenerateMaints);
			EventManager.RemoveHandler(Event.RoundStarted, PostStart);
		}

		private void GenerateMaints()
		{
			foreach (MaintGenerator maintGenerator in MaintGenerators)
			{
				if (maintGenerator == null) continue;
				maintGenerator.CreateTiles();
			}
			Loggy.Log("Finished generating maints tiles!", Category.Round);
		}

		private void PostStart()
		{
			if (this == null) return;
			if (gateway == null) return;
			gateway.GetComponent<UniversalObjectPhysics>().AppearAtWorldPositionServer(possibleExits.PickRandom().AssumedWorldPosServer()); //Randomise gateway position.

			PlaceObjects();
		}

		private void PlaceObjects() //Have to do this later than the tiles, doors cannot be spawned too early less they dont initialise correctly.
		{
			foreach (MaintGenerator maintGenerator in MaintGenerators)
			{
				if (maintGenerator == null) continue;
				maintGenerator.PlaceObjects();
			}
			MaintGenerators.Clear();
			Loggy.Log("Finished generating maints objects!", Category.Round);
		}
	}
}
