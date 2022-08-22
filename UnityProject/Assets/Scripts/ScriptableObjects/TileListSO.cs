using System;
using System.Collections.Generic;
using System.Linq;
using Tiles;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

		[HideInInspector]
		public List<GenericTile> CombinedTileList = new List<GenericTile>();

#if UNITY_EDITOR
		private void OnValidate()
		{
			var newList = new List<GenericTile>();

			foreach (var tile in commonTileList)
			{
				if(tile == null) continue;

				if(newList.Contains(tile)) continue;

				newList.Add(tile);
			}

			foreach (var tile in tileList)
			{
				if(tile == null) continue;

				if(newList.Contains(tile)) continue;

				newList.Add(tile);
			}

			CombinedTileList = newList;

			EditorUtility.SetDirty(this);
			//AssetDatabase.SaveAssets();
			//AssetDatabase.Refresh();
		}
#endif
	}
}
