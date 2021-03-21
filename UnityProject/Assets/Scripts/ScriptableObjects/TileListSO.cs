using System;
using System.Collections.Generic;
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
		[Tooltip("The most used tiles of this type")]
		private List<GenericTile> commonTileList = new List<GenericTile>();
		public List<GenericTile> CommonTiles => commonTileList;

		[SerializeField]
		[Tooltip("Other tiles of this type")]
		private List<GenericTile> tileList = new List<GenericTile>();
		public List<GenericTile> Tiles => tileList;

		private void OnValidate()
		{
			foreach (var value in commonTileList)
			{
				if (tileList.Contains(value))
				{
					tileList.Remove(value);
				}
			}
		}
	}
}
