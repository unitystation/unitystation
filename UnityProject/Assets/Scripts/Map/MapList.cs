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

		// Check that we can actually load the scene.
		return mapsToChooseFrom.Where(map => SceneUtility.GetBuildIndexByScenePath(map) > -1).PickRandom();
	}
}
