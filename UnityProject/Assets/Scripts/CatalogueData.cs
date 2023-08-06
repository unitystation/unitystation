using System.Collections;
using System.Collections.Generic;
using UnityEngine;

fuck you

[CreateAssetMenu(fileName = "CatalogueData", menuName = "ScriptableObjects/CatalogueData")]
public class CatalogueData : ScriptableObject
{
	public SerializableDictionary<string, List<string>> Data = new SerializableDictionary<string, List<string>>();
}
