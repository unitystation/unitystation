using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tiles;

namespace TileManagement
{
	public class TileLocation //No directly accessing this class!!!
	{
		public MetaTileMap metaTileMap = null;
		public Layer layer = null;
		public Color Colour = Color.white;
		public LayerTile layerTile;
		public Matrix4x4 transformMatrix = Matrix4x4.identity;

		//TODO Tile map upgrade , Converts into Vector4Int For under floor tiles
		public Vector3Int position = Vector3Int.zero;

		public void Clean()
		{
			metaTileMap = null;
			layer = null;
			position = Vector3Int.zero;
			Colour = Color.white;
			layerTile = null;
			transformMatrix = Matrix4x4.identity;
		}
	}
}
