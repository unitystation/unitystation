using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using Core;

[CreateAssetMenu(fileName = "AwayWorldListSO", menuName = "ScriptableObjects/AwayWorldList", order = 1)]
public class AwayWorldListSO : ScriptableObject
{
	[Header("Provide the exact name of the scene in the fields below:")]
	[InfoBox("Add your Away site scenes to this list for it to be included in possible " +
	         "away sites from the Station Gateway. Remember to also add your scene to " +
	         "the build settings list",EInfoBoxType.Normal)]
	[Scene]
	public List<string> AwayWorlds = new List<string>();

	public string GetRandomAwaySite()
	{
		if (AwayWorlds.Count > 0 && QuickLoad.IsEnabled == false)
		{
			return AwayWorlds[Random.Range(0, AwayWorlds.Count)];
		}
		else
		{
			return "";
		}

	}
}
