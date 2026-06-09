using System.Collections.Generic;
using UnityEngine;
using US13.Tilemaps.Tiles;

namespace US13.ScriptableObjects
{
	/// <summary>
	/// Contains a list of all possible ghost roles. This information is available on both server and client.
	/// </summary>
	[CreateAssetMenu(fileName = "GraffitiTileList", menuName = "ScriptableObjects/Systems/Tiles/GraffitiTileList")]
	public class GraffitiTileList : ScriptableObject
	{
		[SerializeField]
		//[ReorderableList]
		private List<OverlayTile> graffitiTiles = new List<OverlayTile>();
		public List<OverlayTile> GraffitiTiles => graffitiTiles;
	}
}
