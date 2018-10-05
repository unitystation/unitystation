﻿using UnityEngine;


	public enum LayerType
	{
		Walls,
		Windows,
		Objects,
		Floors,
		Base,
		Grills,
		None
	}

	public enum TileType
	{
		None,
		Wall,
		Window,
		Floor,
		Table,
		Effects,
		Grill,
		Base,
		ReinforcedWall
	}

	public class LayerTile : GenericTile
	{
		private static LayerTile _emptyTile;

		public LayerType LayerType;

		public LayerTile[] RequiredTiles;
		public TileType TileType;
		public static LayerTile EmptyTile => _emptyTile ?? (_emptyTile = CreateInstance<LayerTile>());

		public virtual Matrix4x4 Rotate(Matrix4x4 transformMatrix, bool anticlockwise = true, int count = 1)
		{
			return transformMatrix;
		}
	}
