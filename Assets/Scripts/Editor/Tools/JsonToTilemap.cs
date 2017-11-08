using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        //TODO investigate:
        //AssetDatabase.CreateAsset(asset, assetPath);
        //AssetDatabase.LoadAssetAtPath
        var gridGameObject = new GameObject(SceneManager.GetActiveScene().name + "_Tiled");
        var grid = gridGameObject.AddComponent<Grid>();
        //set up grid here
        grid.cellSize = new Vector3(1f,1f,0);
        
        var layers = DeserializeJson();
        foreach (var layer in layers)
        {
            var layerGO = new GameObject(layer.Key);
            
            //convert positions
            List<Vector3Int> positions = layer.Value.TilePositions.ConvertAll(coord => new Vector3Int(coord.X, coord.Y, 0));
            Debug.LogFormat("Decoded {0} positions for layer {1}", positions.Count, layer.Key);
            //convert tiles
            List<UniTile> tiles = layer.Value.Tiles.ConvertAll(DataToTile);
            Debug.LogFormat("Generated {0} tiles for layer {1}", tiles.Count, layer.Key);
            
            logOverlaps(positions, tiles);

            layerGO.transform.parent = gridGameObject.transform;
            var layerTilemap = layerGO.AddComponent<Tilemap>();
            var layerRenderer = layerGO.AddComponent<TilemapRenderer>();
            //set em up here
            layerRenderer.sortingLayerName = MapToJSON.GetCleanLayerName(layer.Key);
            layerRenderer.sortingOrder = MapToJSON.GetLayerOffset(layer.Key);
            
            layerTilemap.SetTiles(positions.ToArray(), tiles.ToArray());
        }
        gridGameObject.transform.position = new Vector3(-100,0,0); //nudge map's x -100 px?

        Debug.Log("Import kinda finished");
    }

    private static void logOverlaps(List<Vector3Int> positions, List<UniTile> tiles)
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

    private static UniTile DataToTile(UniTileData data)
    {
        var tile = ScriptableObject.CreateInstance<UniTile>();
        tile.name = data.Name;
//        tile.transform = data.Transform;//apply with caution as x,y offsets are huge
        var c = data.ChildTransform;
        var p = data.Transform;
        var customMainTransform = Matrix4x4.TRS(GetPosFromMatrix4x4(c), p.rotation, c.lossyScale );
        tile.transform = customMainTransform;//experimental
        tile.ChildTransform = c;//not being interpreted by Tilemap 
        tile.colliderType = data.ColliderType;
        //generate asset?
        tile.sprite = data.IsLegacy ? SpriteManager.Instance.dmi.getSpriteFromLegacyName(data.SpriteSheet, data.SpriteName) 
                                    : SpriteManager.Instance.dmi.getSprite(data.SpriteSheet, data.SpriteName);
        
        return tile;
    }

    private static Vector3 GetPosFromMatrix4x4(Matrix4x4 c)
    {
        return new Vector3(c.m03, c.m13, 0 /*c.m23*/);
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