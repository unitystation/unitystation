using System.Collections.Generic;
using UnityEngine;
using ScriptableObjects;

[CreateAssetMenu(fileName = "RaceSOSingleton", menuName = "Singleton/RaceSOSingleton")]
public class RaceSOSingleton : SingletonScriptableObject<RaceSOSingleton>
{
	//do Config stuff to allow Certain ones
	public List<PlayerHealthData> Races = new();

	public static bool TryGetRaceByName(string raceName, out PlayerHealthData race)
	{
		foreach (var potentialRace in Instance.Races)
		{
			if (potentialRace.name == raceName)
			{
				race = potentialRace;
				return true;
			}
		}

		race = null;
		return false;
	}
}
