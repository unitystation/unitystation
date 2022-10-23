using System.Collections.Generic;
using UnityEngine;

namespace Systems.Scenes
{
	public class MaintGeneratorManager : MonoBehaviour
	{

		private static List<MaintGenerator> maintGenerators => MaintGenerators;

		public static List<MaintGenerator> MaintGenerators = new List<MaintGenerator>();

		private void Awake()
		{
			if (CustomNetworkManager.IsServer == false) return;
			maintGenerators.Clear();

			EventManager.AddHandler(Event.ScenesLoadedServer, GenerateMaints);
		}

		private void GenerateMaints()
		{
			foreach (MaintGenerator maintGenerator in maintGenerators)
			{
				maintGenerator.CreateTiles();
				maintGenerator.PlaceObjects();
			}
			maintGenerators.Clear();
			Logger.Log("Finished generating maints!", Category.Round);
		}
	}
}
