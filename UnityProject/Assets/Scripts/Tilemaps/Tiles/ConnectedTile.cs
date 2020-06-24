using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public enum ConnectCategory
{
	Walls,
	Windows,
	Tables,
	Floors,
	None
}

public enum ConnectType
{
	ToAll,
	ToSameCategory,
	ToSelf
}

public class ConnectedTile : BasicTile
{
	private static readonly int[] map =
	{
			0, 2, 4, 8, 1, 255, 3, 6, 12, 9, 10, 5, 7, 14, 13, 11, 15, 19, 38, 76, 137, 23, 39, 46, 78, 77, 141, 139, 27, 31, 47, 79, 143, 63, 111, 207, 159,
			191, 127, 239, 223, 55, 110, 205, 155, 175, 95
		};

	private Sprite[] _sprites;

	public ConnectCategory connectCategory = ConnectCategory.None;
	public ConnectType connectType = ConnectType.ToAll;
	public SpriteSheetAndData spriteSheet;
	public string texturePath;
	/// <summary>
	/// Cached layer we live in, so we can determine our rotation
	/// </summary>
	private Layer layer;

	public override Sprite PreviewSprite => sprites != null && sprites.Length > 0 ? sprites[0] : null;

	private Sprite[] sprites
	{
		get
		{
			if (_sprites == null || _sprites.Length == 0)
			{

				_sprites = spriteSheet.Sprites;
				//Logger.Log(texturePath + "/" + spriteSheet.name);
			}
			return _sprites;
		}
	}

	public override bool StartUp(Vector3Int location, ITilemap tilemap, GameObject go)
	{
		if (Application.isPlaying)
		{
			layer = tilemap.GetComponent<Layer>();
		}
		return true;
	}

	public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
	{
		//find our offset by checking our parent layer
		Quaternion rotation;
		//ensure we have our layer (if possible)
		if (layer == null)
		{
			layer = tilemap.GetComponent<Layer>();
		}
		if (layer != null)
		{
			//I dont really get the need for this since
			//to make a rotation makes sense you would have to rotate The tile positionally
			//rotation = layer.RotationOffset.QuaternionInverted;
			rotation = Quaternion.identity;
		}
		else
		{
			rotation = Quaternion.identity;
		}

		if (tilemap.GetComponent<Tilemap>().name == "Layer1")
		{
			// don't connect while in palette
			base.GetTileData(position, tilemap, ref tileData);
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

	private bool HasSameTile(Vector3Int position, Vector3Int direction, Quaternion rotation, ITilemap tilemap)
	{
		TileBase tile = tilemap.GetTile(position + (rotation * direction).RoundToInt());

		if (tile == null)
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
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
}
