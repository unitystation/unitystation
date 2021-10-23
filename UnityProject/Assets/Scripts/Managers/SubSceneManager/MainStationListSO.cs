using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "MainStationListSO", menuName = "ScriptableObjects/MainStationList", order = 1)]
public class MainStationListSO : ScriptableObject
{
	[Header("Provide the exact name of the scene in the fields below:")]
	[InfoBox("Remember to also add your scene to " +
	         "the build settings list",EInfoBoxType.Normal)]
	[Scene]
	public List<string> MainStations = new List<string>();

	public string GetRandomMainStation()
	{
		var mapConfigPath = Path.Combine(Application.streamingAssetsPath, "maps.json");

		if (File.Exists(mapConfigPath))
		{
			var maps = JsonUtility.FromJson<MapList>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,
				"maps.json")));

			return maps.GetRandomMap();
		}

		// Check that we can actually load the scene.
		return MainStations.Where(scene => SceneUtility.GetBuildIndexByScenePath(scene) > -1).PickRandom();
	}
}
