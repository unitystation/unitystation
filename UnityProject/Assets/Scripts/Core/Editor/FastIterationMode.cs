using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;

public class FastIterationMode
{
	[MenuItem("Debug/Enable fast iteration")]
	public static void SetupFastIterationMode()
	{
		// remove all stations except TestStation
		RemoveAllStations(exceptStation: "TestStation");

		// remove all asteroids except first one
		/*var asteroids = currentScenes.Where((scene) =>
		scene.path.Contains("AsteroidScenes")).Skip(1);

		// remove all away sites except first one
		var awaySites = currentScenes.Where((scene) =>
			scene.path.Contains("AwaySites")).Skip(1);

		// combine them all and remove from build settings
		var scenesToRemove = mainStations.Union(asteroids).Union(awaySites);
		foreach (var scene in currentScenes)
		{
			if (scenesToRemove.Contains(scene))
				scene.enabled = false;
		}

		EditorBuildSettings.scenes = currentScenes;*/
	}

	/// <summary>
	/// Remove all stations from build settings, station scriptable object and json configuration
	/// </summary>
	/// <param name="exceptStation">One station to leave in rotation</param>
	private static void RemoveAllStations(string exceptStation)
	{
		var exceptStations = new List<string> { exceptStation };

		// get scriptable object with list of all stations
		var mainStationsSO = Resources.LoadAll<MainStationListSO>("").FirstOrDefault();
		if (!mainStationsSO)
		{
			Debug.LogError("Can't find MainStationListSO in resources folder!");
			return;
		}

		// remove all stations from scriptable object 
		mainStationsSO.MainStations = exceptStations;
		SaveScriptableObject(mainStationsSO);

		// replace maps in json configuration in streaming assets
		var mapConfigPath = Path.Combine(Application.streamingAssetsPath, "maps.json");
		var mapConfig = JsonUtility.FromJson<MapList>(File.ReadAllText(mapConfigPath));
		mapConfig.lowPopMaps = exceptStations;
		mapConfig.medPopMaps = exceptStations;
		mapConfig.highPopMaps = exceptStations;

		// save new json
		var mapConfigJson = JsonUtility.ToJson(mapConfig);
		File.WriteAllText(mapConfigPath, mapConfigJson);

		// find stations to delete in editor settings
		var currentScenes = EditorBuildSettings.scenes;
		var mainStations = currentScenes.Where((scene) =>
			scene.path.Contains("MainStations") && !scene.path.Contains(exceptStation));

		// now disable all station scenes from build settings
		foreach (var scene in currentScenes)
		{
			if (mainStations.Contains(scene))
				scene.enabled = false;
		}
		EditorBuildSettings.scenes = currentScenes;
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	private static void SaveScriptableObject(Object so)
	{
		EditorUtility.SetDirty(so);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

	}
}
