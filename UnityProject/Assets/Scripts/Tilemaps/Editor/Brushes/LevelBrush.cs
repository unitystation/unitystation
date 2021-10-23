using System;
using TileManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor.Tilemaps;
using Random = UnityEngine.Random;


[CustomGridBrush(false, true, true, "Level Brush")]
public class LevelBrush : GridBrush
{
	public class RecordedChimpEvent
	{
		public DateTime time = DateTime.Now;
		public Vector3Int loc = Vector3Int.zero;
	}

	public static RecordedChimpEvent recordedChimpEvent = new RecordedChimpEvent();

	public static bool ChimpEventTooRecent(Vector3Int position)
	{
		if ((DateTime.Now - recordedChimpEvent.time).Seconds > 0.2f || recordedChimpEvent.loc != position)
		{
			recordedChimpEvent.time = DateTime.Now;
			recordedChimpEvent.loc = position;
			//Logger.Log("successful chimp event");
			return false;
		}
		//Logger.Log("no chimp event");
		return true;
	}

	public override void Paint(GridLayout grid, GameObject layer, Vector3Int position)
	{
		if (ChimpEventTooRecent(position)) return;
		BoxFill(grid, layer, new BoundsInt(position, Vector3Int.one));
	}

	public override void Erase(GridLayout grid, GameObject layer, Vector3Int position)
	{
		if (ChimpEventTooRecent(position)) return;
		BoxErase(grid, layer, new BoundsInt(position, Vector3Int.one));
	}

	public override void BoxFill(GridLayout grid, GameObject layer, BoundsInt area)
	{
		if (cellCount > 0 && cells[0].tile != null)
		{
			MetaTileMap metaTileMap = grid.GetComponent<MetaTileMap>();

			if (!metaTileMap)
			{
				return;
			}

			foreach (Vector3Int position in area.allPositionsWithin)
			{
				TileBase tile = cells[Random.Range(0, cells.Length)].tile;

				if (tile == null)
				{
					tile = cells[0].tile;
				}

				if (tile is LayerTile)
				{
					PlaceLayerTile(metaTileMap, position, (LayerTile) tile);
				}
				else if (tile is MetaTile)
				{
					PlaceMetaTile(metaTileMap, position, (MetaTile) tile);
				}
			}
		}
	}

	public override void BoxErase(GridLayout grid, GameObject layer, BoundsInt area)
	{
		MetaTileMap metaTileMap = grid.GetComponent<MetaTileMap>();

		foreach (Vector3Int position in area.allPositionsWithin)
		{
			if (metaTileMap)
			{
				metaTileMap.RemoveTile(position);
			}
			else
			{
				layer.GetComponent<Layer>().SetTile(position, null, Matrix4x4.identity, Color.white);
			}
		}
	}

	public override void Flip(FlipAxis flip, GridLayout.CellLayout layout)
	{
		if (UnityEngine.Event.current.character == '>')
		{
			// TODO flip?
		}
	}

	public override void Rotate(RotationDirection direction, GridLayout.CellLayout layout)
	{
		LayerTile tile = cells[0].tile as LayerTile;

		if (tile != null)
		{
			cells[0].matrix = tile.Rotate(cells[0].matrix, direction == RotationDirection.Clockwise);
		}
	}

	private void PlaceMetaTile(MetaTileMap metaTileMap, Vector3Int position, MetaTile metaTile)
	{
		foreach (LayerTile tile in metaTile.GetTiles())
		{
			//metaTileMap.RemoveTileWithlayer(position, tile.LayerType);
			metaTileMap.SetTile(position, tile, cells[0].matrix, isPlaying: false);
		}
	}

	private void PlaceLayerTile(MetaTileMap metaTileMap, Vector3Int position, LayerTile tile)
	{
		//metaTileMap.RemoveTileWithlayer(position, tile.LayerType);
		SetTile(metaTileMap, position, tile);
	}

	private void SetTile(MetaTileMap metaTileMap, Vector3Int position, LayerTile tile)
	{
		foreach (LayerTile requiredTile in tile.RequiredTiles)
		{
			SetTile(metaTileMap, position, requiredTile);
		}

		metaTileMap.SetTile(position, tile, cells[0].matrix, cells[0].color, isPlaying: false);
	}
}