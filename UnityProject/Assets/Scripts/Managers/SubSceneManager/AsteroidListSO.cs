using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "AsteroidListSO", menuName = "ScriptableObjects/AsteroidList", order = 1)]
public class AsteroidListSO : ScriptableObject
{
	[Header("Provide the exact name of the scene in the fields below:")]
	[InfoBox("Add your asteroid scenes to this list for it to be " +
	         "spawned at runtime. Remember to also add your scene to " +
	         "the build settings list",EInfoBoxType.Normal)]
	public List<string> Asteroids = new List<string>();
}
