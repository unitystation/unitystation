using System.Collections.Generic;
using System.IO;
using System.Linq;
using FullSerializer;
using Sprites;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
public class JsonToTilemap : Editor
{
    public const string TC = "tc_";
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
            //convert tiles
            List<UniTile> tiles = layer.Value.Tiles.ConvertAll(DataToTile);
            Debug.LogFormat("Decoded {0} positions, generated {1} tiles for layer {2}", positions.Count, tiles.Count, layer.Key);
            
            logOverlaps(positions, tiles);

            layerGO.transform.parent = gridGameObject.transform;
            var layerTilemap = layerGO.AddComponent<Tilemap>();
            var layerRenderer = layerGO.AddComponent<TilemapRenderer>();
            layerRenderer.sortingLayerName = GetCleanLayerName(layer.Key);
            layerRenderer.sortingOrder = GetLayerOffset(layer.Key);
            
            layerTilemap.SetTiles(positions.ToArray(), tiles.ToArray());
        }
        gridGameObject.transform.position = new Vector3(-100,0,0); 

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
        var c = data.ChildTransform;
        var p = data.Transform;
        //upscaling TC tiles for eye candy, fixme: you will want to turn that off later when new tileconnect is ready
        if ( data.SpriteName.StartsWith(TC) )
        {
            SetXyScale(ref p, 2);
        }
        tile.transform = CombineTransforms(p, c);//customMainTransform;//experimental
        tile.ChildTransform = c;//not being interpreted by Tilemap 
        tile.colliderType = data.ColliderType;

        //generate asset here?
        tile.sprite = data.IsLegacy ? SpriteManager.Instance.dmi.getSpriteFromLegacyName(data.SpriteSheet, data.SpriteName) 
                                    : SpriteManager.Instance.dmi.getSprite(data.SpriteSheet, data.SpriteName);
        
        return tile;
    }

    private static void SetXyScale(ref Matrix4x4 matrix, float scale)
    {
        matrix.m00 = scale;
        matrix.m11 = scale;
    }


    private static Matrix4x4 CombineTransforms(Matrix4x4 p, Matrix4x4 c)
    {
        var rotation = p.rotation * c.rotation;
        var position = rotation * GetPosFromMatrix4x4(c); //parent position offsets are huge! don't think we should use them at all
        var scale = p.lossyScale;
        return Matrix4x4.TRS( position, rotation, scale );
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

    internal static string GetCleanLayerName(string dirtyName)
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

    internal static int GetLayerOffset(string dirtyName)
    {
        return int.Parse(dirtyName.Replace(GetCleanLayerName(dirtyName), ""));
    }
}
#endif