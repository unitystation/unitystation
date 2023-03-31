using System.Collections;
using System.Collections.Generic;
using _3D;
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
		public SetCubeSprite AssociatedSetCubeSprite;

		//TODO Tile map upgrade , Converts into Vector4Int For under floor tiles
		public Vector3Int position = Vector3Int.zero;

		public bool InQueue = false;
		public bool NewTile = false;

		public void Clean()
		{
			NewTile = false;
			metaTileMap = null;
			layer = null;
			position = Vector3Int.zero;
			Colour = Color.white;
			layerTile = null;
			transformMatrix = Matrix4x4.identity;
			if (AssociatedSetCubeSprite != null)
			{
				GameObject.Destroy(AssociatedSetCubeSprite.gameObject);
			}
			AssociatedSetCubeSprite = null;
		}
	}
}
