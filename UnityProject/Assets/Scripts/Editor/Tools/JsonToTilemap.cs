using System;
using System.Collections.Generic;
using System.IO;
using FullSerializer;
using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Tiles;
using Tilemaps.Scripts.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStation.Tools;

public class JsonToTilemap : Editor
{
	[MenuItem("Tools/Import map (JSON)")]
	private static void Json2Map()
	{
		GameObject map = GameObject.FindGameObjectWithTag("Map");
		MetaTileMap metaTileMap = map.GetComponentInChildren<MetaTileMap>();

		metaTileMap.ClearAllTiles();

		TilemapConverter converter = new TilemapConverter();

		TileMapBuilder builder = new TileMapBuilder(metaTileMap, true);

		Dictionary<string, TilemapLayer> layers = DeserializeJson();

		List<Tuple<Vector3Int, ObjectTile>> objects = new List<Tuple<Vector3Int, ObjectTile>>();

		foreach (KeyValuePair<string, TilemapLayer> layer in layers)
		{
			List<Vector3Int> positions =
				layer.Value.TilePositions.ConvertAll(coord => new Vector3Int(coord.X, coord.Y, 0));

			for (int i = 0; i < positions.Count; i++)
			{
				Vector3Int position = positions[i];
				GenericTile tile = converter.DataToTile(layer.Value.Tiles[i]);

				if (tile is ObjectTile)
				{
					if (!objects.Exists(t => t.Item1.Equals(position) && t.Item2 == tile))
					{
						objects.Add(new Tuple<Vector3Int, ObjectTile>(position, (ObjectTile) tile));
					}
				}
				else
				{
					builder.PlaceTile(position, tile);
				}
			}
		}

		foreach (Tuple<Vector3Int, ObjectTile> tuple in objects)
		{
			Vector3Int position = tuple.Item1;
			ObjectTile obj = tuple.Item2;

			Matrix4x4 matrix = obj.Rotatable ? FindObjectPosition(metaTileMap, ref position, obj) : Matrix4x4.identity;

			builder.PlaceTile(position, obj, matrix);
		}

		// mark as dirty, otherwise the scene can't be saved.
		EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
		Debug.Log("Import kinda finished");
	}

	private static Matrix4x4 FindObjectPosition(MetaTileMap metaTileMap, ref Vector3Int position, LayerTile tile)
	{
		bool onStructure = metaTileMap.HasTile(position, LayerType.Walls) ||
		                   metaTileMap.HasTile(position, LayerType.Windows);

		Quaternion rotation = Quaternion.identity;

		for (int i = 0; i < 4; i++)
		{
			Vector3Int offset = Vector3Int.RoundToInt(rotation * Vector3.up);
			bool hasStructure = metaTileMap.HasTile(position + offset, LayerType.Walls) ||
			                    metaTileMap.HasTile(position, LayerType.Windows);
			bool isOccupied = metaTileMap.HasTile(position + offset, onStructure ? LayerType.Base : LayerType.Objects);

			if (onStructure != hasStructure && isOccupied == onStructure)
			{
				if (onStructure)
				{
					position += offset;
					rotation *= Quaternion.Euler(0f, 0f, 180);
				}
				break;
			}

			rotation *= Quaternion.Euler(0, 0, 90);
		}

		return Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
	}

	private static Dictionary<string, TilemapLayer> DeserializeJson()
	{
		Dictionary<string, TilemapLayer> deserializedLayers = new Dictionary<string, TilemapLayer>();
		TextAsset asset = Resources.Load(Path.Combine("metadata", SceneManager.GetActiveScene().name)) as TextAsset;
		if (asset != null)
		{
			fsData data = fsJsonParser.Parse(asset.text);
			fsSerializer serializer = new fsSerializer();
			serializer.TryDeserialize(data, ref deserializedLayers).AssertSuccessWithoutWarnings();
		}
		else
		{
			throw new FileNotFoundException("Put your map json to /Assets/Resources/metadata/%mapname%.json!");
		}
		return deserializedLayers;
	}

	internal static string GetSortingLayerName(SpriteRenderer renderer)
	{
		return renderer.sortingLayerName + renderer.sortingOrder;
	}

	private static string GetCleanLayerName(string dirtyName)
	{
		char[] lameTrimChars = {'1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '-'};
		return dirtyName.TrimEnd(lameTrimChars);
	}

	internal static int CompareSpriteLayer(string x, string y)
	{
		List<string> sortingLayerNames = MapToPNG.GetSortingLayerNames();
		string xTrim = GetCleanLayerName(x);
		string yTrim = GetCleanLayerName(y);
		int x_index = sortingLayerNames.FindIndex(s => s.StartsWith(xTrim));
		int y_index = sortingLayerNames.FindIndex(s => s.StartsWith(yTrim));

		if (x_index == y_index)
		{
			return GetLayerOffset(y) - GetLayerOffset(x);
		}
		return y_index - x_index;
	}

	private static int GetLayerOffset(string dirtyName)
	{
		return int.Parse(dirtyName.Replace(GetCleanLayerName(dirtyName), ""));
	}
}