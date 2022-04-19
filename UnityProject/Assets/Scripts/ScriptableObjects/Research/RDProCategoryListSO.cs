using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "CategoryListSO",menuName = "ScriptableObjects/Systems/Techweb/CategoryListSO")]
public class RDProCategoryListSO : ScriptableObject
{
	[Header("Provide the exact category names in the fields below")]
	[InfoBox("Categories in this list will determine what shows up in the GUI, make sure to include a 'Misc' " +
		"category if you wish to view designs that don't fit into the other design categories.")]
	public List<string> Categories = new List<string>();
}
