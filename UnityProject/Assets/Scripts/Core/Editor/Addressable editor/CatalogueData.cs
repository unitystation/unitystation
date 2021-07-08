using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CatalogueData", menuName = "ScriptableObjects/CatalogueData")]
public class CatalogueData : ScriptableObject
{
	public CatalogueDictionary Data = new CatalogueDictionary();
}
