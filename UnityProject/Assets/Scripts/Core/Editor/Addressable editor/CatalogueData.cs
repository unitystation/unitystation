using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CatalogueData", menuName = "ScriptableObjects/CatalogueData")]
public class CatalogueData : ScriptableObject
{
	public SerializableDictionary<string, List<string>> Data = new SerializableDictionary<string, List<string>>();
}
