using System;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEditor;
using UnityEngine;
using US13.Tilemaps.Utils;
using Util;
#if UNITY_EDITOR
#endif

namespace US13.Tilemaps.Tiles
{
	[Serializable]
	public class LayerTile : GenericTile, ISOTracker
	{

		[field: SerializeField]
		public string ForeverID { get; set; }

		public Sprite OldSprite => PreviewSprite;

		public string Name => name;

		public SpriteDataSO Sprite => null;

		[SerializeField]
		[Tooltip("Name to dispay to the player for this tile.")]
		private string displayName = null;

		[SerializeField]
		[Tooltip("Text seen by the player when examining the tile.")]
		private string description = default;

		/// <summary>
		/// Name to display to the player for this tile. Defaults to the tile type.
		/// </summary>
		public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? TileType.ToString().ToLower() : displayName;

		/// <summary>
		/// Text seen by the player when examining the tile.
		/// </summary>
		public string Description => description;

		private static LayerTile emptyTile;

		public static LayerTile EmptyTile => emptyTile ? emptyTile : (emptyTile = ScriptableObject.CreateInstance<LayerTile>());

		public LayerType LayerType;
		public TileType TileType;

		[SerializeField]
		private List<TileTrait> tileTraits = new List<TileTrait>();

		/// <summary>
		/// Checks whether this tile has the given trait. A null trait returns false.
		/// </summary>
		public bool HasTrait(TileTrait toCheck)
		{
			if (toCheck == null) return false;
			return tileTraits.Contains(toCheck);
		}

		/// <summary>
		/// Checks whether this tile has any of the given traits. A null or empty list returns false.
		/// </summary>
		public bool HasAnyTrait(List<TileTrait> toCheck)
		{
			if (toCheck == null) return false;

			foreach (var trait in toCheck)
			{
				if (trait != null && tileTraits.Contains(trait)) return true;
			}
			return false;
		}

		public LayerTile[] RequiredTiles = { };

		public float Mass = 1;

		public virtual Matrix4x4 Rotate(Matrix4x4 transformMatrix, bool anticlockwise = true, int count = 1)
		{
			return transformMatrix;
		}

		[NaughtyAttributes.Button("Assign random ID")]
		public void ForceSetID() //Assuming it's a prefab Variant
		{
#if UNITY_EDITOR
			// Can possibly change over time so need some prevention
			ForeverID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
			if (string.IsNullOrEmpty(ForeverID))
			{
				ForeverID = CreateString(20);
			}

			EditorUtility.SetDirty(this);
			Undo.RecordObject(this, "gen ID");
#endif
		}

		internal static string CreateString(int stringLength)
		{
			const string allowedChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!$?_-";
			char[] chars = new char[stringLength];

			for (int i = 0; i < stringLength; i++)
			{
				chars[i] = allowedChars.PickRandom();
			}

			return new string(chars);
		}
	}
}
