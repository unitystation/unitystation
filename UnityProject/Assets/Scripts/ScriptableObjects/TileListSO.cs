using System;
using System.Collections.Generic;
using Tiles;
using UnityEngine;

namespace ScriptableObjects
{
	/// <summary>
	/// Contains a list of tiles, used in dev tile changer
	/// </summary>
	[CreateAssetMenu(fileName = "TileList", menuName = "ScriptableObjects/Systems/Tiles/TileList")]
	public class TileListSO : ScriptableObject
	{
		[SerializeField]
		[Tooltip("The layertype these tiles use, this is used to determine removal")]
		private LayerType layerType = LayerType.Base;
		public LayerType LayerType => layerType;

		[SerializeField]
		[Tooltip("The most used tiles of this type")]
		private List<GenericTile> commonTileList = new List<GenericTile>();

		[SerializeField]
		[Tooltip("Other tiles of this type")]
		private List<GenericTile> tileList = new List<GenericTile>();

		private List<GenericTile> combinedTileList = new List<GenericTile>();
		public List<GenericTile> CombinedTileList => combinedTileList;

		private void OnValidate()
		{
			foreach (var value in commonTileList)
			{
				if (tileList.Contains(value))
				{
					tileList.Remove(value);
				}
			}

			combinedTileList = commonTileList;
			combinedTileList.AddRange(tileList);

			if (combinedTileList.Count > commonTileList.Count + tileList.Count)
			{
				Debug.LogError("That shouldnt happen...");
			}
		}
	}
}
