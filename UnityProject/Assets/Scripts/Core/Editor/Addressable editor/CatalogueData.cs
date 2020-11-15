using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CatalogueData", menuName = "ScriptableObjects/CatalogueData")]
public class CatalogueData : ScriptableObject
{
	public List<string> SoundAndMusic = new List<string>();
}
