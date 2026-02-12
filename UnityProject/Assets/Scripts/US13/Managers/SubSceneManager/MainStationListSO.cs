using System.Collections.Generic;
using System.Linq;
using Logs;
using Newtonsoft.Json;
using SecureStuff;
using UnityEngine;
using UnityEngine.SceneManagement;
using US13.Core.Chat;
using US13.Map;
using Util;

namespace US13.Managers.SubSceneManager
{
	[CreateAssetMenu(fileName = "MainStationListSO", menuName = "ScriptableObjects/MainStationList", order = 1)]
	public class MainStationListSO : ScriptableObject
	{
		[Header("Provide the exact name of the scene in the fields below:")]
		public List<string> MainStations = new();

		public string mapsConfig = "maps.json";

		[Header("If these mainstations have their own set of unique asteroids, assign their list here:")]
		public AsteroidListSO AsteroidListOverride = null;

		public string GetRandomMainStation()
		{
			if (AccessFile.Exists(mapsConfig))
			{
				var maps = JsonConvert.DeserializeObject<MapList>(AccessFile.Load(mapsConfig));
				return maps.GetRandomMap();
			}
			Chat.AddGameWideSystemMsgToChat("Uh oh! We're using the legacy way of loading scenes!!" +
			                                "Make sure that the gamemode has a maps config file set or is not missing!!!");

			// Check that we can actually load the scene.
			var mapSoList = MainStations.Where(scene => SceneUtility.GetBuildIndexByScenePath(scene) > -1 ||	AccessFile.Exists(scene, true,FolderType.Maps)).ToList();

			if (mapSoList.Count == 0)
			{
				Loggy.Error("No valid maps found! Make sure theres a map inside the MainStationList that is also in the build settings");
			}

			return mapSoList.PickRandom();
		}

		public List<string> GetMaps()
		{
			if (AccessFile.Exists(mapsConfig))
			{
				var maps = JsonConvert.DeserializeObject<MapList>(AccessFile.Load(mapsConfig));

				return maps.highPopMaps.Union(maps.medPopMaps).Union(maps.lowPopMaps).ToList();
			}

			return MainStations;
		}

		public bool Contains(string sceneName)
		{
			return MainStations.Contains(sceneName);
		}
	}
}
