using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using FullSerializer;
using Sprites;
using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Tiles;
using Tilemaps.Scripts.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityStation.Tools;

public class JsonToTilemap : Editor
{
    [MenuItem("Tools/Import map (JSON)")]
    static void Json2Map()
    {
        var map = GameObject.FindGameObjectWithTag("Map");
        var metaTileMap = map.GetComponentInChildren<MetaTileMap>();

        TilemapConverter.LoadMapping();
        
        var builder = new TileMapBuilder(metaTileMap, true);
        
        var layers = DeserializeJson();
        foreach (var layer in layers)
        {
            //convert positions
            var positions = layer.Value.TilePositions.ConvertAll(coord => new Vector3Int(coord.X, coord.Y, 0));

            for (int i = 0; i < positions.Count; i++)
            {
                var position = positions[i];
                var tile = TilemapConverter.DataToTile(layer.Value.Tiles[i]);
                
                builder.PlaceTile(position, tile, Matrix4x4.identity);
            }
        }

        Debug.Log("Import kinda finished");
    }

    private static void logOverlaps(IList<Vector3Int> positions, IReadOnlyList<UniTile> tiles)
    {
        var overlaps = positions.GroupBy(v => v)
            .Where(v => v.Count() > 1)
            .Select(v => new {Pos = new XYCoord(v.Key.x, v.Key.y), Tile = tiles[positions.IndexOf(v.Key)], Count = v.Count()})
            .ToList();
        if ( overlaps.Count != 0 )
        {
            Debug.LogWarning(overlaps.Aggregate("Overlaps found: ", (current, ds) => current + ds.ToString()));
        }
    }

    private static Dictionary<string, TilemapLayer> DeserializeJson()
    {
        var deserializedLayers = new Dictionary<string, TilemapLayer>();
        var asset = Resources.Load(Path.Combine("metadata", SceneManager.GetActiveScene().name)) as TextAsset;
        if (asset != null)
        {
            var data = fsJsonParser.Parse(asset.text);
            var serializer = new fsSerializer();
            serializer.TryDeserialize(data, ref deserializedLayers).AssertSuccessWithoutWarnings();
        } else throw new FileNotFoundException("Put your map json to /Assets/Resources/metadata/%mapname%.json!");
        return deserializedLayers;
    }
    
    internal static HashSet<string> GetSortingLayerOrderNames(IEnumerable<SpriteRenderer> renderers)
    {
        var hs = new HashSet<string>();
        foreach ( var renderer in renderers )
        {
            hs.Add(renderer.sortingLayerName + renderer.sortingOrder);
        }
        return hs;
    }

    internal static string GetSortingLayerName(SpriteRenderer renderer)
    {
        return renderer.sortingLayerName + renderer.sortingOrder;
    }

    private static string GetCleanLayerName(string dirtyName)
    {
        var lameTrimChars = new[] {'1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '-'};
        return dirtyName.TrimEnd(lameTrimChars);
    }

    internal static int CompareSpriteLayer(string x, string y)
    {
        var sortingLayerNames = MapToPNG.GetSortingLayerNames();
        var xTrim = GetCleanLayerName(x);
        var yTrim = GetCleanLayerName(y);
        var x_index = sortingLayerNames.FindIndex(s => s.StartsWith(xTrim));
        var y_index = sortingLayerNames.FindIndex(s => s.StartsWith(yTrim));

        if ( x_index == y_index )
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