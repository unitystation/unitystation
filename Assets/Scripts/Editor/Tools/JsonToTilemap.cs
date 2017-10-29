using System.Collections.Generic;
using System.IO;
using FullSerializer;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class JsonToTilemap
{
    [MenuItem("Tools/Import map (JSON)")]
    static void Json2Map()
    {
        /* create "%stashunName%" parent GO with Grid component
         * create n(layer amount) child "%layerName%" GOs with Tilemap and TilemapRenderer components
         * tilemap.SetTiles( V3IntPositions[], TileBases[] )
         */
        var grid = new GameObject(SceneManager.GetActiveScene().name + "_Tiled");
        var layers = DeserializeJson();
        foreach (var layer in layers)
        {
            //todo layer sort!
            var layerGO = new GameObject(layer.Key);
            var positions = new Vector3Int()[layer.Value.TilePositions.Count];
            var tiles = new List<TileBase>()[layer.Value.Tiles.Count];
            
        }
    }
    private static UniTile DataToTile(UniTileData data)
    {
        return ScriptableObject.CreateInstance<UniTile>();
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
        } else throw new FileNotFoundException("Put yer map json to /Assets/Resources/metadata/%mapname%.json!");
        return deserializedLayers;
    }
}