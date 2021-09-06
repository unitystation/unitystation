using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TileManagement
{
	public class TileLocation //No directly accessing this class!!!
	{
		public bool InQueue = false;

		public MetaTileMap PresentMetaTileMap = null;
		public Layer PresentlyOn = null;

		//TODO Tile map upgrade , Converts into Vector4Int For under floor tiles
		public Vector3Int TileCoordinates = Vector3Int.zero;
		private Color colour = Color.white;

		public Color Colour
		{
			get => colour;
			set
			{
				if (colour != value)
				{
					colour = value;
					//OnStateChange();
				}
			}
		}

		private LayerTile tile;

		public LayerTile Tile
		{
			get => tile;
			set
			{
				if (tile != value)
				{
					tile = value;
					//OnStateChange();
				}
			}
		}

		private Matrix4x4 transformMatrix = Matrix4x4.identity;

		public Matrix4x4 TransformMatrix
		{
			get => transformMatrix;
			set
			{
				if (transformMatrix != value)
				{
					transformMatrix = value;
					//OnStateChange();
				}
			}
		}

		public void OnStateChange()
		{
			lock (PresentMetaTileMap.QueuedChanges)
			{
				lock (this)
				{
					if (InQueue) return;
					InQueue = true;
				}

				PresentMetaTileMap.QueuedChanges.Enqueue(this);
			}
		}

		public void Clean()
		{
			PresentMetaTileMap = null;
			PresentlyOn = null;
			TileCoordinates = Vector3Int.zero;
			colour = Color.white;
			tile = null;
			transformMatrix = Matrix4x4.identity;
			InQueue = false;
		}
	}
}