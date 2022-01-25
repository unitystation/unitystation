using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

[Serializable]
public class MapList
{
	public List<string> lowPopMaps = new List<string>();
	public List<string> medPopMaps = new List<string>();
	public List<string> highPopMaps = new List<string>();
	public int medPopMinLimit = 15; //how many players needed to use medpop maps
	public int highPopMinLimit = 40; //how many players needed to include highpop maps

	public string GetRandomMap()
	{
		var playerCount = PlayerList.LastRoundPlayerCount;
		if (playerCount < PlayerList.Instance.ConnectionCount)
		{
			playerCount = PlayerList.Instance.ConnectionCount;
		}

		List<string> mapsToChooseFrom;

		if (playerCount < medPopMinLimit)
		{
			mapsToChooseFrom = lowPopMaps;
		}
		else if (playerCount < highPopMinLimit)
		{
			mapsToChooseFrom = medPopMaps;
		}
		else
		{
			mapsToChooseFrom = highPopMaps;
		}
		
		if (mapsToChooseFrom.Count == 0)
		{
			Logger.LogError($"No maps with playerCount: {playerCount} were found, trying to pick any map now.");
			var allMaps = new List<string>();
			allMaps.AddRange(lowPopMaps);
			allMaps.AddRange(medPopMaps);
			allMaps.AddRange(highPopMaps);

			mapsToChooseFrom = allMaps;
		}

		// Check that we can actually load the scene.
		var validMaps = mapsToChooseFrom.Where(map => SceneUtility.GetBuildIndexByScenePath(map) > -1).ToList();

		if (validMaps.Count == 0)
		{
			Logger.LogError("No valid maps found! Make sure theres a map inside the Maps.json that is also in the build settings");
		}
		
		return validMaps.PickRandom();
	}
}
