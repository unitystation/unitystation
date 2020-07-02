using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

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
		var mapsToChooseFrom = new List<string>(lowPopMaps);
		var playerCount = PlayerList.LastRoundPlayerCount;

		if (playerCount < PlayerList.Instance.ConnectionCount)
		{
			playerCount = PlayerList.Instance.ConnectionCount;
		}

		if (playerCount <= medPopMinLimit && lowPopMaps.Count > 0)
		{
			return mapsToChooseFrom[Random.Range(0, mapsToChooseFrom.Count)];
		}

		mapsToChooseFrom = new List<string>(medPopMaps);
		if (playerCount >= highPopMinLimit)
		{
			mapsToChooseFrom.AddRange(highPopMaps);
		}

		var rand = Random.Range(0, mapsToChooseFrom.Count);
		return mapsToChooseFrom[rand];
	}
}
