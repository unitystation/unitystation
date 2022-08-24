using System.Collections.Generic;
using TileManagement;
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
		[Tooltip("Overlay type of theses tiles")]
		private OverlayType overlayType = OverlayType.None;
		public OverlayType OverlayType => overlayType;

		[SerializeField]
		[Tooltip("The most used tiles of this type")]
		private List<GenericTile> commonTileList = new List<GenericTile>();

		[SerializeField]
		[Tooltip("Other tiles of this type")]
		private List<GenericTile> tileList = new List<GenericTile>();

		[field: HideInInspector]
		[field: SerializeField]
		public List<GenericTile> CombinedTileList { get; private set; }

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
