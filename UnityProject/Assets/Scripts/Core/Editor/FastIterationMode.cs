using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;

public class FastIterationMode
{
	private static List<string> AdditionalScenesToRemove = new List<string>()
	{
		"Fallstation Centcom",
		"Fallstation Syndicate"
	};

	[MenuItem("Debug/Remove non-essential scenes")]
	public static void SetupFastIterationMode()
	{
		// remove all stations except TestStation
		RemoveAllStations(exceptStation: "TestStation");

		LeaveOneAsteroid();
		LeaveOneAwaySite();
		DisableLavaland();
		DisableAdditionalScenes();

		// make sure that editor saved all changes above
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	/// <summary>
	/// Disable additional scenes in build settings
	/// May cause unexpected behaviour on certain stations/gamemodes
	/// </summary>
	private static void DisableAdditionalScenes()
	{
		var currentScenes = EditorBuildSettings.scenes;
		var additionalMaps = currentScenes.Where((scene) =>
		{
			// select all scene to removal
			foreach (var sceneToRemove in AdditionalScenesToRemove)
			{
				if (scene.path.Contains(sceneToRemove))
					return true;
			}

			return false;
		});
		DisableScenes(additionalMaps);
	}

	/// <summary>
	/// Disable LavaLand in json config and build-settings
	/// </summary>
	private static void DisableLavaland()
	{
		// Disable LavaLand in config file
		var path = Path.Combine(Application.streamingAssetsPath, "config", "gameConfig.json");
		var config = JsonUtility.FromJson<GameConfig.GameConfig>(File.ReadAllText(path));
		config.SpawnLavaLand = false;

		// save config json
		var gameConfigJson = JsonUtility.ToJson(config);
		File.WriteAllText(path, gameConfigJson);

		// Disable LavaLand scene
		var currentScenes = EditorBuildSettings.scenes;
		var lavaLandMaps = currentScenes.Where((scene) => scene.path.Contains("LavaLand"));
		DisableScenes(lavaLandMaps);
	}

	/// <summary>
	/// Disable all away sites subscenes, except first one
	/// </summary>
	private static void LeaveOneAwaySite()
	{
		// get scriptable object with list of all away sites
		var awayWorldsSO = Resources.LoadAll<AwayWorldListSO>("").FirstOrDefault();
		if (!awayWorldsSO)
		{
			Debug.LogError("Can't find AwayWorldListSO in resources folder!");
			return;
		}

		// remove all away worlds (except first one)
		var firstAwayWorld = awayWorldsSO.AwayWorlds.First();
		awayWorldsSO.AwayWorlds = new List<string> { firstAwayWorld };
		EditorUtility.SetDirty(awayWorldsSO);

		// remove all away worlds from scene list
		var currentScenes = EditorBuildSettings.scenes;
		var awayWorlds = currentScenes.Where((scene) =>
			scene.path.Contains("AwaySites") && !scene.path.Contains(firstAwayWorld))
			.ToList();
		DisableScenes(awayWorlds);
	}

	/// <summary>
	/// Disable all asteroids subscenes, except first one
	/// </summary>
	private static void LeaveOneAsteroid()
	{
		// get scriptable object with list of all asteroids
		var asteroidListSO = Resources.LoadAll<AsteroidListSO>("").FirstOrDefault();
		if (!asteroidListSO)
		{
			Debug.LogError("Can't find AsteroidListSO in resources folder!");
			return;
		}

		// remove all except first asteroid
		var firstAsteroid = asteroidListSO.Asteroids.First();
		asteroidListSO.Asteroids = new List<string> { firstAsteroid };
		EditorUtility.SetDirty(asteroidListSO);

		// remove all asteroids from scene list
		var currentScenes = EditorBuildSettings.scenes;
		var asteroids = currentScenes.Where((scene) =>
			scene.path.Contains("AsteroidScenes") && !scene.path.Contains(firstAsteroid));
		DisableScenes(asteroids);
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
		EditorUtility.SetDirty(mainStationsSO);

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
		DisableScenes(mainStations);
	}

	private static void DisableScenes(IEnumerable<EditorBuildSettingsScene> scenesToDisable)
	{
		var currentScenes = EditorBuildSettings.scenes;
		foreach (var sceneInList in currentScenes)
		{
			foreach (var sceneToDisable in scenesToDisable)
			{
				if (sceneToDisable.guid == sceneInList.guid)
				{
					sceneInList.enabled = false;
				}
			}
		}
		EditorBuildSettings.scenes = currentScenes;
	}
}
