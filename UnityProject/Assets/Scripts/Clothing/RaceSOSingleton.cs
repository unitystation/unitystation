using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "RaceSOSingleton", menuName = "Singleton/RaceSOSingleton")]
public class RaceSOSingleton : SingletonScriptableObject<RaceSOSingleton>
{
	//do Config stuff to allow Certain ones
	public List<PlayerHealthData> Races = new List<PlayerHealthData>();
}
