using System.Collections.Generic;
using System.IO;
using FullSerializer;
using Sprites;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class JsonToTilemap
{
    [MenuItem("Tools/Import map (JSON)")]
    static void Json2Map()
    {
        //todo: assign GO layer?
        var gridGameObject = new GameObject(SceneManager.GetActiveScene().name + "_Tiled");
        var grid = gridGameObject.AddComponent<Grid>();
        //set up grid here
        grid.cellSize = new Vector3(1f,1f,0);
        
        var layers = DeserializeJson();
        foreach (var layer in layers)
        {
            var layerGO = new GameObject(layer.Key);
            
            //convert positions
            var positions = layer.Value.TilePositions.ConvertAll(coord => new Vector3Int(coord.X, coord.Y, 0));
            Debug.LogFormat("Decoded {0} positions for layer {1}", positions.Count, layer.Key);
            //convert tiles
            var tiles = layer.Value.Tiles.ConvertAll(DataToTile);
            Debug.LogFormat("Generated {0} tiles for layer {1}", tiles.Count, layer.Key);


            layerGO.transform.parent = gridGameObject.transform;
            var layerTilemap = layerGO.AddComponent<Tilemap>();
            var layerRenderer = layerGO.AddComponent<TilemapRenderer>();
            //set em up here
            layerRenderer.sortingLayerName = MapToJSON.GetCleanLayerName(layer.Key);
            layerRenderer.sortingOrder = MapToJSON.GetLayerOffset(layer.Key);
            
            layerTilemap.SetTiles(positions.ToArray(), tiles.ToArray());
        }
        Debug.Log("Import kinda finished");
    }
    private static UniTile DataToTile(UniTileData data)
    {
        var tile = ScriptableObject.CreateInstance<UniTile>();
        tile.name = data.Name;
//        tile.transform = data.Transform;//apply with caution!
        tile.transform = data.ChildTransform;//experimental
        tile.ChildTransform = data.ChildTransform;//not being interpreted by Tilemap 
        tile.colliderType = data.ColliderType;
        //generate asset?
        tile.sprite = data.IsLegacy ? SpriteManager.Instance.dmi.getSpriteFromLegacyName(data.SpriteSheet, data.SpriteName) 
                                    : SpriteManager.Instance.dmi.getSprite(data.SpriteSheet, data.SpriteName);
        
        return tile;
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
}