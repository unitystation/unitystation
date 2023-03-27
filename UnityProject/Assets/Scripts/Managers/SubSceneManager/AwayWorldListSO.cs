using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "AwayWorldListSO", menuName = "ScriptableObjects/AwayWorldList", order = 1)]
public class AwayWorldListSO : ScriptableObject
{
	[Header("Provide the exact name of the scene in the fields below:")]
	[InfoBox("Add your Away site scenes to this list for it to be included in possible " +
	         "away sites from the Station Gateway. Remember to also add your scene to " +
	         "the build settings list",EInfoBoxType.Normal)]
	public List<AssetReference> AwayWorlds = new List<AssetReference>();
}
