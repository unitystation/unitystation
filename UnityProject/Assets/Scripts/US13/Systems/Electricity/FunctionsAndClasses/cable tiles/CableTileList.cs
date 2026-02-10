using System.Collections.Generic;
using UnityEngine;

namespace US13.Systems.Electricity.FunctionsAndClasses.cable_tiles
{
	[CreateAssetMenu(fileName = "CableTileList", menuName = "ScriptableObjects/CableTileList")]
	public class CableTileList : ScriptableObject
	{
		public List<ElectricalCableTile> Tiles = new List<ElectricalCableTile>();
	}
}
