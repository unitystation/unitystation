using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tiles
{
	public class ConnectedTileV2 : ConnectedTile
	{
		[Flags]
		public enum ConnectionDirections
		{
			None = 0,
			Up = 1 << 0,
			UpRight = 1 << 1,
			Right = 1 << 2,
			DownRight = 1 << 3,
			Down = 1 << 4,
			DownLeft = 1 << 5,
			Left = 1 << 6,
			UpLeft = 1 << 7
		}

		[Serializable]
		public class ConnectionData
		{
			public ConnectionDirections Direction;

			public Sprite Sprite;
		}

		public override Sprite PreviewSprite => connectionData != null && connectionData.Count > 0 ? connectionData[0].Sprite : null;

		[SerializeField]
		private List<ConnectionData> connectionData = new List<ConnectionData>();

		[SerializeField]
		//This is needed for the build
		private Texture2D wholeSpriteTexture = null;

		public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
		{
			//find our offset by checking our parent layer
			Quaternion rotation;
			{
				rotation = Quaternion.identity;
			}

			if (tilemap.GetComponent<Tilemap>().name == "Layer1")
			{
				// don't connect while in palette
				base.GetTileData(position, tilemap, ref tileData);
				return;
			}

			ConnectionDirections mask = 0;

			mask |= HasSameTile(position, Vector3Int.up, rotation, tilemap) ? ConnectionDirections.Up : 0;

			mask |= HasSameTile(position, Vector3Int.right, rotation, tilemap) ? ConnectionDirections.Right: 0;

			mask |= HasSameTile(position, Vector3Int.down, rotation, tilemap) ? ConnectionDirections.Down : 0;

			mask |= HasSameTile(position, Vector3Int.left, rotation, tilemap) ? ConnectionDirections.Left : 0;

			if (mask.HasFlag(ConnectionDirections.Right) && mask.HasFlag(ConnectionDirections.Up))
			{
				var newMask = HasSameTile(position, Vector3Int.right + Vector3Int.up, rotation, tilemap) ? ConnectionDirections.UpRight : 0;

				mask |= newMask;

				if (HasSprite(mask) == false)
				{
					//remove the mask if theres no sprite for it
					mask &= newMask;
				}
			}

			if (mask.HasFlag(ConnectionDirections.Right) && mask.HasFlag(ConnectionDirections.Down))
			{
				var newMask = HasSameTile(position, Vector3Int.right + Vector3Int.down, rotation, tilemap) ? ConnectionDirections.DownRight : 0;

				mask |= newMask;

				if (HasSprite(mask) == false)
				{
					//remove the mask if theres no sprite for it
					mask &= newMask;
				}
			}

			if (mask.HasFlag(ConnectionDirections.Left) && mask.HasFlag(ConnectionDirections.Down))
			{
				var newMask = HasSameTile(position, Vector3Int.left + Vector3Int.down, rotation, tilemap) ? ConnectionDirections.DownLeft : 0;

				mask |= newMask;

				if (HasSprite(mask) == false)
				{
					//remove the mask if theres no sprite for it
					mask &= newMask;
				}
			}

			if (mask.HasFlag(ConnectionDirections.Left) && mask.HasFlag(ConnectionDirections.Up))
			{
				var newMask = HasSameTile(position, Vector3Int.left + Vector3Int.up, rotation, tilemap) ? ConnectionDirections.UpLeft : 0;

				mask |= newMask;

				if (HasSprite(mask) == false)
				{
					//remove the mask if theres no sprite for it
					mask &= newMask;
				}
			}

			if (SetSprite(ref tileData, mask, rotation)) return;

			//Can't find correct direction so default to none
			if (SetSprite(ref tileData, ConnectionDirections.None, rotation) == false)
			{
				Loggy.LogError($"No None direction for {name}");
			}
		}

		private bool SetSprite(ref TileData tileData, ConnectionDirections mask, Quaternion rotation)
		{
			foreach (var data in connectionData)
			{
				if (data.Direction != mask) continue;

				tileData.sprite = data.Sprite;

				tileData.flags = TileFlags.None;

				// create collider for tiles, None, Sprite or Grid
				tileData.colliderType = Tile.ColliderType.Grid;
				tileData.transform = Matrix4x4.Rotate(rotation);

				return true;
			}

			return false;
		}

		private bool HasSprite(ConnectionDirections mask)
		{
			foreach (var data in connectionData)
			{
				if (data.Direction != mask) continue;

				return true;
			}

			return false;
		}
	}
}