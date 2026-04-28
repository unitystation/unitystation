using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Util;
using Random = UnityEngine.Random;

namespace US13.Tilemaps.Tiles
{
	public abstract class GenericTile : TileBase
	{
		public virtual Sprite PreviewSprite
		{
			get
			{
				if (sprite != null)
				{
					return sprite;
				}
				else if (Sprites != null && Sprites.Length > 0)
				{
					return Sprites[0];
				}
				else if (spriteSheet != null && spriteSheet?.Sprites?.Length > 0)
				{
					return spriteSheet.Sprites[0];
				}
				else if (spriteSheets?.Count > 0)
				{
					return spriteSheets[0].Sprites[0];
				}

				return _PreviewSprite;
			}
			set
			{
				_PreviewSprite = value;
			}
		}

		[HideInInspector]
		public Sprite _PreviewSprite;

		public Sprite sprite;
		public Sprite[] Sprites;
		public SpriteSheetAndData spriteSheet;
		public List<SpriteSheetAndData> spriteSheets = new List<SpriteSheetAndData>();

		public static readonly int[] map =
		{
			0, 2, 4, 8, 1, 255, 3, 6, 12, 9, 10, 5, 7, 14, 13, 11, 15, 19, 38, 76, 137, 23, 39, 46, 78, 77, 141, 139, 27, 31, 47, 79, 143, 63, 111, 207, 159,
			191, 127, 239, 223, 55, 110, 205, 155, 175, 95
		};

		protected Sprite[] _sprites;

		public ConnectCategory connectCategory = ConnectCategory.None;
		public ConnectType connectType = ConnectType.ToAll;

		public string texturePath;

		public List<LayerTile> Blacklist;
		public List<LayerTile> WhiteList;

		protected Sprite[] sprites
		{
			get
			{
				if (_sprites == null || _sprites.Length == 0)
				{

					_sprites = spriteSheet.Sprites;
					//Loggy.Log(texturePath + "/" + spriteSheet.name);
				}
				return _sprites;
			}
		}


		public float AnimationSpeed = 1f;
		public float AnimationStartTime = 0;
		public bool randomizeStartTime;

		public TileAnimationFlags TileAnimationFlags = TileAnimationFlags.None;

		public override void RefreshTile(Vector3Int position, ITilemap tilemap)
		{
			foreach (Vector3Int p in new BoundsInt(-1, -1, 0, 3, 3, 1).allPositionsWithin)
			{
				tilemap.RefreshTile(position + p);
			}
		}

		public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap,
			ref TileAnimationData tileAnimationData)
		{
			if ((Sprites==null ||  Sprites.Length == 0)&& spriteSheets.Count == 0)
			{
				tileAnimationData.animatedSprites = new []{PreviewSprite};
				tileAnimationData.animationSpeed = AnimationSpeed;
				tileAnimationData.flags = TileAnimationFlags;
				return false;
			}

			if (spriteSheets.Count > 0)
			{
				//find our offset by checking our parent layer
				Quaternion rotation;
				{
					rotation = Quaternion.identity;
				}

				if (tilemap.GetComponent<Tilemap>().name == "Layer1")
				{
					// don't connect while in palette
					return base.GetTileAnimationData(position, tilemap, ref tileAnimationData);

				}

				int mask = (HasSameTile(position, Vector3Int.up, rotation, tilemap) ? 1 : 0) +
				           (HasSameTile(position, Vector3Int.right, rotation, tilemap) ? 2 : 0) +
				           (HasSameTile(position, Vector3Int.down, rotation, tilemap) ? 4 : 0) +
				           (HasSameTile(position, Vector3Int.left, rotation, tilemap) ? 8 : 0);

				if ((mask & 3) == 3)
				{
					mask += HasSameTile(position, Vector3Int.right + Vector3Int.up, rotation, tilemap) ? 16 : 0;
				}

				if ((mask & 6) == 6)
				{
					mask += HasSameTile(position, Vector3Int.right + Vector3Int.down, rotation, tilemap) ? 32 : 0;
				}

				if ((mask & 12) == 12)
				{
					mask += HasSameTile(position, Vector3Int.left + Vector3Int.down, rotation, tilemap) ? 64 : 0;
				}

				if ((mask & 9) == 9)
				{
					mask += HasSameTile(position, Vector3Int.left + Vector3Int.up, rotation, tilemap) ? 128 : 0;
				}

				int i = Array.IndexOf(map, mask);

				if (i >= 0)
				{
					if (spriteSheets.Count > 0)
					{
						tileAnimationData.animatedSprites = spriteSheets.Select(x => x.Sprites[i]).ToArray();
					}
				}
			}
			else
			{
				tileAnimationData.animatedSprites = Sprites;
			}


			tileAnimationData.animationSpeed = AnimationSpeed;
			tileAnimationData.flags = TileAnimationFlags;
			if (!randomizeStartTime)
			{
				tileAnimationData.animationStartTime = AnimationStartTime;
			}
			else
			{
				tileAnimationData.animationStartTime = Random.Range(0f, 10f);
			}

			return true;
		}

		public override bool StartUp(Vector3Int location, ITilemap tilemap, GameObject go)
		{
			return true;
		}

		public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
		{

			if (spriteSheet?.Texture == null && spriteSheets?.Count == 0)
			{
				tileData.sprite = PreviewSprite;
				tileData.flags = TileFlags.None;
				tileData.colliderType = Tile.ColliderType.Grid;
				return;
			}

			//find our offset by checking our parent layer
			Quaternion rotation;
			{
				rotation = Quaternion.identity;
			}

			if (tilemap.GetComponent<Tilemap>().name == "Layer1")
			{
				// don't connect while in palette
				tileData.sprite = PreviewSprite;
				tileData.flags = TileFlags.None;
				tileData.colliderType = Tile.ColliderType.Grid;
				return;
			}

			int mask = (HasSameTile(position, Vector3Int.up, rotation, tilemap) ? 1 : 0) + (HasSameTile(position, Vector3Int.right, rotation, tilemap) ? 2 : 0) +
					   (HasSameTile(position, Vector3Int.down, rotation, tilemap) ? 4 : 0) + (HasSameTile(position, Vector3Int.left, rotation, tilemap) ? 8 : 0);

			if ((mask & 3) == 3)
			{
				mask += HasSameTile(position, Vector3Int.right + Vector3Int.up, rotation, tilemap) ? 16 : 0;
			}
			if ((mask & 6) == 6)
			{
				mask += HasSameTile(position, Vector3Int.right + Vector3Int.down, rotation, tilemap) ? 32 : 0;
			}
			if ((mask & 12) == 12)
			{
				mask += HasSameTile(position, Vector3Int.left + Vector3Int.down, rotation, tilemap) ? 64 : 0;
			}
			if ((mask & 9) == 9)
			{
				mask += HasSameTile(position, Vector3Int.left + Vector3Int.up, rotation, tilemap) ? 128 : 0;
			}

			int i = Array.IndexOf(map, mask);

			if (i >= 0)
			{

				if (sprites != null && sprites.Length > i)
				{
					tileData.sprite = sprites[i];
				}
				tileData.flags = TileFlags.None;
				// create collider for tiles, None, Sprite or Grid
				tileData.colliderType = Tile.ColliderType.Grid;
				tileData.transform = Matrix4x4.Rotate(rotation);
				//tileData.flags = TileFlags.LockTransform;
			}
		}

		protected bool HasSameTile(Vector3Int position, Vector3Int direction, Quaternion rotation, ITilemap tilemap)
		{
			TileBase tile = tilemap.GetTile(position + (rotation * direction).RoundToInt());

			if (tile == null)
			{
				return false;
			}

			if (Blacklist.Contains(tile))
			{
				return false;
			}

			switch (connectType)
			{
				case ConnectType.ToAll:
					return true;
				case ConnectType.ToSameCategory:
					ConnectedTile t = tile as ConnectedTile;
					return t != null && t.connectCategory == connectCategory;
				case ConnectType.ToSelf:
					return tile == this;
				case ConnectType.ToCategoryAndSelf:
					if (tile == this) return true;
					ConnectedTile x = tile as ConnectedTile;
					return x != null && x.connectCategory == connectCategory;
				case ConnectType.WhiteList:
					return WhiteList.Contains(tile);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
