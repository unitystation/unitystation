using System.Collections.Generic;
using System.Linq;
using Logs;
using SecureStuff;
using UnityEngine;
using NaughtyAttributes;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "MainStationListSO", menuName = "ScriptableObjects/MainStationList", order = 1)]
public class MainStationListSO : ScriptableObject
{
	[Header("Provide the exact name of the scene in the fields below:")]
	[InfoBox("Remember to also add your scene to " +
	         "the build settings list",EInfoBoxType.Normal)]
	public List<string> MainStations = new();

	public string mapsConfig = "maps.json";

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
			Loggy.LogError("No valid maps found! Make sure theres a map inside the MainStationList that is also in the build settings");
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
