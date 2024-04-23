using System;
using ScriptableObjects;
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace Tiles
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
			const string allowedChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@$?_-";
			char[] chars = new char[stringLength];

			for (int i = 0; i < stringLength; i++)
			{
				chars[i] = allowedChars.PickRandom();
			}

			return new string(chars);
		}
	}
}
