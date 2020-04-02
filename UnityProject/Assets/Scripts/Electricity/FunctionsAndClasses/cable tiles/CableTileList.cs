using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CableTileList", menuName = "ScriptableObjects/CableTileList")]
public class CableTileList : ScriptableObject
{
	public List<ElectricalCableTile> Tiles = new List<ElectricalCableTile>();
}
