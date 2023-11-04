using Doors;
using System;
using System.Collections.Generic;
using System.Linq;
using Tiles;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

namespace Objects.Doors
{
	public class FalseWallTileBlend : MonoBehaviour
	{
		private SpriteRenderer sprite;
		[Tooltip("Empty tile to spawn when false wall texture is setted.")]
		[SerializeField] private BasicTile falseTile = null;
		private TileChangeManager tileChangeManager;
		private GeneralDoorAnimator doorAnimator;
		private DoorController doorController;
		private Vector3Int falseWallPosition;
		private List<LayerTile> tilesPresendtedNear = new List<LayerTile>();
		private List<FalseWallTileBlend> falseWallsPresendtedNear = new List<FalseWallTileBlend>();
		private static Dictionary<Vector3Int, FalseWallTileBlend> falseWallPresented = new Dictionary<Vector3Int, FalseWallTileBlend>();
		private bool emptyWallIsPlaced = false;

		private Sprite[] sprites;

		private static readonly int[] map =
		{
			0, 2, 4, 8, 1, 255, 3, 6, 12, 9, 10, 5, 7, 14, 13, 11, 15, 19, 38, 76, 137, 23, 39, 46, 78, 77, 141, 139, 27, 31, 47, 79, 143, 63, 111, 207, 159,
			191, 127, 239, 223, 55, 110, 205, 155, 175, 95
		};

		private void Start()
		{
			sprite = GetComponentInChildren<SpriteRenderer>();
			var registerDoor = GetComponentInChildren<RegisterDoor>();
			tileChangeManager = GetComponentInParent<TileChangeManager>();
			falseWallPosition = Vector3Int.RoundToInt(transform.localPosition);
			doorAnimator = GetComponent<GeneralDoorAnimator>();
			sprites = doorAnimator.GetAnimationSprites();
			doorController = GetComponent<DoorController>();

			if (falseWallPresented.ContainsKey(falseWallPosition))
				falseWallPresented.Remove(falseWallPosition);
			falseWallPresented.Add(falseWallPosition, this);
			tilesPresendtedNear = GetNearTilesExtended(falseWallPosition);
			falseWallsPresendtedNear = GetNearFalseWalls(falseWallPosition);
			UpdateWallSprite();
			ChangeNearFalseWallSprites(GetNearFalseWalls(falseWallPosition));

			tileChangeManager.MetaTileMap.Layers[LayerType.Walls].onTileMapChanges.AddListener(UpdateMethod);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.LATE_UPDATE, UpdateMethod);
			falseWallPresented.Remove(falseWallPosition);
			tileChangeManager.MetaTileMap.Layers[LayerType.Walls].RemoveTile(falseWallPosition);
		}

		private void UpdateMethod()
		{
			UpdateWallSprite();
		}

		private bool CheckIfNeedUpdateNow()
		{
			var tilesPresendtsNear = GetNearTilesExtended(falseWallPosition, true);
			var falseWallsPresendtsNear = GetNearFalseWalls(falseWallPosition);

			var needUpdateNow = false;

			if (!tilesPresendtsNear.SequenceEqual(tilesPresendtedNear))
			{
				tilesPresendtedNear.Clear();
				tilesPresendtedNear.AddRange(tilesPresendtsNear);
				needUpdateNow = true;
			} else if (!falseWallsPresendtsNear.SequenceEqual(falseWallsPresendtedNear))
			{
				falseWallsPresendtedNear.Clear();
				falseWallsPresendtedNear.AddRange(falseWallsPresendtsNear);
				needUpdateNow = true;
			}
			return needUpdateNow;
		}

		private void ChangeNearFalseWallSprites(List<FalseWallTileBlend> falseWallTileBlends)
		{
			foreach (var falseWall in falseWallTileBlends)
			{
				falseWall.UpdateWallSprite();
			}
		}

		/// <summary>
		/// Updating tile sprite for blending with walls
		/// </summary>
		[ContextMenu("Update wall sprite")]
		public void UpdateWallSprite()
		{
			var smoothFrameIndex = SmoothFrame();
			// Double check to be sure and don`t stop door animation
			if (doorController.IsClosed && !doorController.isPerformingAction)
				sprite.sprite = sprites[smoothFrameIndex];
			doorAnimator.closeFrame = smoothFrameIndex;

			if (HasRealWall(falseWallPosition, tileChangeManager.MetaTileMap.Layers[LayerType.Walls]?.GetComponent<Tilemap>()) == false && emptyWallIsPlaced == false)
			{
				tileChangeManager.MetaTileMap.SetTile(falseWallPosition, falseTile);
				emptyWallIsPlaced = true;
			}
		}

		private List<FalseWallTileBlend> GetNearFalseWalls(Vector3Int location)
		{
			var nearTiles = new List<FalseWallTileBlend>();

			for (int i = 0; i < 8; i++)
			{
				var locToCheckPos = LocationToCheckExtended(i, location);
				var falseWall = GetFalseWall(locToCheckPos);

				if (falseWall != null)
				{
					nearTiles.Add(falseWall);
				}
			}
			return nearTiles;
		}

		private List<LayerTile> GetNearTiles(Vector3Int position, bool addTileUnderPosition = false)
		{
			var nearTiles = new List<LayerTile>();
			for (int i = 0; i < 4; i++)
			{
				var posToCheck = LocationToCheck(i, position);
				nearTiles.Add(tileChangeManager.MetaTileMap.GetTile(posToCheck));
			}

			if (addTileUnderPosition)
			{
				nearTiles.Add(tileChangeManager.MetaTileMap.GetTile(position));
			}
			return nearTiles;
		}

		private List<LayerTile> GetNearTilesExtended(Vector3Int position, bool addTileUnderPosition = false)
		{
			var nearTiles = new List<LayerTile>();
			for (int i = 0; i < 8; i++)
			{
				var posToCheck = LocationToCheckExtended(i, position);
				nearTiles.Add(tileChangeManager.MetaTileMap.GetTile(posToCheck));
			}

			if (addTileUnderPosition)
			{
				nearTiles.Add(tileChangeManager.MetaTileMap.GetTile(position));
			}
			return nearTiles;
		}

		private FalseWallTileBlend GetFalseWall(Vector3Int pos)
		{
			if (falseWallPresented.ContainsKey(pos) && falseWallPresented[pos] != null)
				return falseWallPresented[pos];
			return null;
		}

		private bool HasWall(Vector3Int position, Vector3Int direction, Quaternion rotation, Tilemap tilemap)
		{
			TileBase tile = tilemap.GetTile(position + (rotation * direction).RoundToInt());
			ConnectedTile t = tile as ConnectedTile;
			return (t != null && t.connectCategory == ConnectCategory.Walls) ||
			GetFalseWall(new Vector3Int(
			(position + (rotation * direction)).x.RoundToLargestInt(),
			(position + (rotation * direction)).y.RoundToLargestInt(),
			(position + (rotation * direction)).z.RoundToLargestInt())) != null;
		}

		private bool HasRealWall(Vector3Int position, Tilemap tilemap)
		{
			TileBase tile = tilemap.GetTile(position);
			ConnectedTile t = tile as ConnectedTile;
			return (t != null) && (!t.PreviewSprite.name.Contains("false_wall"));
		}

		private int SmoothFrame()
		{
			var metaTileMap = tileChangeManager.MetaTileMap;
			var tilemap = metaTileMap.Layers[LayerType.Walls].GetComponent<Tilemap>();
			Vector3Int position = Vector3Int.RoundToInt(transform.localPosition);
			var layer = tilemap.GetComponent<Layer>();
			Quaternion rotation = layer.RotationOffset.QuaternionInverted;
			int mask = (HasWall(position, Vector3Int.up, rotation, tilemap) ? 1 : 0) + (HasWall(position, Vector3Int.right, rotation, tilemap) ? 2 : 0) +
				   (HasWall(position, Vector3Int.down, rotation, tilemap) ? 4 : 0) + (HasWall(position, Vector3Int.left, rotation, tilemap) ? 8 : 0);
			if ((mask & 3) == 3)
			{
				mask += HasWall(position, Vector3Int.right + Vector3Int.up, rotation, tilemap) ? 16 : 0;
			}
			if ((mask & 6) == 6)
			{
				mask += HasWall(position, Vector3Int.right + Vector3Int.down, rotation, tilemap) ? 32 : 0;
			}
			if ((mask & 12) == 12)
			{
				mask += HasWall(position, Vector3Int.left + Vector3Int.down, rotation, tilemap) ? 64 : 0;
			}
			if ((mask & 9) == 9)
			{
				mask += HasWall(position, Vector3Int.left + Vector3Int.up, rotation, tilemap) ? 128 : 0;
			}
			int i = Array.IndexOf(map, mask);
			i += 6; //The first 6 frames are reserved for the animation
			return i;
		}

		private Vector3Int LocationToCheck(int direction, Vector3Int directionToCheck)
		{
			switch (direction)
			{
				case 0:
					directionToCheck.y += 1;
					return directionToCheck;
				case 1:
					directionToCheck.x += 1;
					return directionToCheck;
				case 2:
					directionToCheck.y -= 1;
					return directionToCheck;
				case 3:
					directionToCheck.x -= 1;
					return directionToCheck;
			}
			return directionToCheck;
		}

		private Vector3Int LocationToCheckExtended(int direction, Vector3Int directionToCheck)
		{
			switch (direction)
			{
				case 0:
					directionToCheck.y += 1;
					return directionToCheck;
				case 1:
					directionToCheck.x += 1;
					directionToCheck.y += 1;
					return directionToCheck;
				case 2:
					directionToCheck.x += 1;
					return directionToCheck;
				case 3:
					directionToCheck.x += 1;
					directionToCheck.y -= 1;
					return directionToCheck;
				case 4:
					directionToCheck.y -= 1;
					return directionToCheck;
				case 5:
					directionToCheck.x -= 1;
					directionToCheck.y -= 1;
					return directionToCheck;
				case 6:
					directionToCheck.x -= 1;
					return directionToCheck;
				case 7:
					directionToCheck.x -= 1;
					directionToCheck.y += 1;
					return directionToCheck;
			}
			return directionToCheck;
		}
	}
}
