using System.Collections.Generic;
using UnityEngine;
using Gateway;

namespace Systems.Scenes
{
	public class MaintGeneratorManager : MonoBehaviour
	{
		public static List<MaintGenerator> MaintGenerators { get; set; } = new List<MaintGenerator>();

		private void Awake()
		{
			TransportUtility.MaintRoomLocations.Clear();

			if (CustomNetworkManager.IsServer == false) return;
			MaintGenerators.Clear();

			EventManager.AddHandler(Event.ScenesLoadedServer, GenerateMaints);
			EventManager.AddHandler(Event.RoundStarted, PlaceObjects);
		}

		private void GenerateMaints()
		{
			foreach (MaintGenerator maintGenerator in MaintGenerators)
			{
				if (maintGenerator == null) continue;
				maintGenerator.CreateTiles();
			}
			Logger.Log("Finished generating maints tiles!", Category.Round);
		}

		private void PlaceObjects() //Have to do this later than the tiles, doors cannot be spawned too early less they dont initialise correctly.
		{
			foreach (MaintGenerator maintGenerator in MaintGenerators)
			{
				if (maintGenerator == null) continue;
				maintGenerator.PlaceObjects();
			}
			MaintGenerators.Clear();
			Logger.Log("Finished generating maints objects!", Category.Round);
		}
	}
}
