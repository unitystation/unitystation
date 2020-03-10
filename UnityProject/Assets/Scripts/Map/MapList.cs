using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

[Serializable]
public class MapList
{
	public List<string> lowPopMaps = new List<string>();
	public List<string> highPopMaps = new List<string>();
	public int highPopMinLimit = 40; //how many players needed to include highpop maps

	public string GetRandomMap()
	{
		List<string> mapsToChooseFrom = new List<string>(lowPopMaps);
		if (PlayerList.Instance.ConnectionCount >= highPopMinLimit)
		{
			mapsToChooseFrom.AddRange(highPopMaps);
		}

		var rand = Random.Range(0, mapsToChooseFrom.Count);
		return mapsToChooseFrom[rand];
	}
}
