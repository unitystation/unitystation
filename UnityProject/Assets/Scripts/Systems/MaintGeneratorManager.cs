using System.Collections.Generic;
using UnityEngine;

namespace Systems.Scenes
{
	public class MaintGeneratorManager : MonoBehaviour
	{
		public static List<MaintGenerator> MaintGenerators { get; set; } = new List<MaintGenerator>();

		private void Awake()
		{
			if (CustomNetworkManager.IsServer == false) return;
			MaintGenerators.Clear();

			EventManager.AddHandler(Event.ScenesLoadedServer, GenerateMaints);
		}

		private void GenerateMaints()
		{
			foreach (MaintGenerator maintGenerator in MaintGenerators)
			{
				maintGenerator.CreateTiles();
				maintGenerator.PlaceObjects();
			}
			MaintGenerators.Clear();
			Logger.Log("Finished generating maints!", Category.Round);
		}
	}
}
