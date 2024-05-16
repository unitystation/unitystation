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

		public bool Removing = false;

		public LayerTile layerTile
		{
			get
			{
				if (Removing)
				{
					return null;
				}
				else
				{
					return InternalLayerTile;
				}
			}
			set
			{
				if (value != null)
				{
					Removing = false;
					InternalLayerTile = value;
				}
				else
				{
					Removing = true;

				}
			}
		}

		public LayerTile InternalLayerTile;


		public Matrix4x4 transformMatrix = Matrix4x4.identity;
		public SetCubeSprite AssociatedSetCubeSprite;

		//TODO Tile map upgrade , Converts into Vector4Int For under floor tiles
		public Vector3Int LocalPosition = Vector3Int.zero;

		public bool InQueue = false;
		public bool NewTile = false;

		public void Clean()
		{
			NewTile = false;
			metaTileMap = null;
			layer = null;
			LocalPosition = Vector3Int.zero;
			Colour = Color.white;
			layerTile = null;
			transformMatrix = Matrix4x4.identity;
			Removing = false;
			if (AssociatedSetCubeSprite != null)
			{
				GameObject.Destroy(AssociatedSetCubeSprite.gameObject);
			}
			AssociatedSetCubeSprite = null;
		}
	}
}
