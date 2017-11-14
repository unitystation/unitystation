using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FullSerializer;
using Matrix;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static JsonToTilemap;

#if UNITY_EDITOR
public class MapToJSON : Editor
{
    //FIXME: try to combine local transforms and check if it helps the bottom shuttle fire closet

    //adding +50 offset to spriteRenderers containing these:
    static readonly List<string> separateLayerMarkers = new List<string>(new []{"WarningLine"});
    //not marking these as legacy
    static readonly List<string> legacyExclusionList = new List<string>(new[] {"turf/shuttle.png"/*,"lighting.png","obj/power.png"*/});
    //pretending these contain TileConnect component (however, four of these are still required to generate a temporary tile)
    //Item1: name to lookup (via Contains())
    //Item2: asset path to use instead while exporting
    static readonly List<Tuple<string,string>> tileConnectWannabes = new List<Tuple<string,string>>
        (new []{new Tuple<string, string>("shuttle_wall_Skew","walls/shuttle_wall")});

    

    [MenuItem("Tools/Export map (JSON)")]
    static void Map2JSON()
    {
        AssetDatabase.Refresh();
        // for smart stuff(doors, windoors): create new Tile with transform, no sprite and source GO (?)
        // 
        // export: (grilles, airlocks, firelocks, disposal, solars, wallmounts(incl.lights), furniture(chairs, beds, tables))
        /*  {
         *       ColliderType = Grid (all but wallmounts), none (wallmounts)
         */

        var nodesMapped = MapToPNG.GetMappedNodes();
        var tilemapLayers = new SortedDictionary<string, TilemapLayer>(Comparer<string>.Create(CompareSpriteLayer));
        var tempGameObjects = new List<GameObject>();

        for ( int y = 0; y < nodesMapped.GetLength(0); y++ )
        {
            for ( int x = 0; x < nodesMapped.GetLength(1); x++ )
            {
                var node = nodesMapped[y, x];

                if ( node == null )
                    continue;

                var allRenderers = new List<SpriteRenderer>();

                foreach ( var tile in node.GetTiles() )
                {
                    var tileRenderers = tile.GetComponentsInChildren<SpriteRenderer>();
                    if ( tileRenderers == null || tileRenderers.Length < 1 ) continue;
//                    //TODO fix excessive layers with obvious duplicates(identical transform and spritename?)
                        var tileconnects = 0;
                        foreach ( var renderer in tileRenderers )
                        {
                            if ( thisRendererSucks(renderer) || renderer.sortingLayerID == 0 )
                                continue;
                            
                            TryMoveToSeparateLayer(renderer);
                            
                            if ( renderer.GetComponent<TileConnect>() || IsTileConnectWannabe(renderer) )
                            {
                                tileconnects++;
                                if ( tileconnects != 4 ) continue;
                                if ( tileconnects > 4 )
                                {
                                    Debug.LogWarningFormat("{0} — more than 4 tileconnects found!", renderer.name);
                                }
                                // grouping four tileconnect sprites into a single temporary thing
                                GameObject tcMergeGameObject = Instantiate(renderer.gameObject, tile.transform.position,
                                    Quaternion.identity, tile.transform);
                                tempGameObjects.Add(tcMergeGameObject);
                                var childClone = tcMergeGameObject.GetComponent<SpriteRenderer>();
                                var spriteName = childClone.sprite.name;

                                if ( spriteName.Contains("_") )
                                {
                                    childClone.name = TC + spriteName.Substring(0,
                                                          spriteName.LastIndexOf("_", StringComparison.Ordinal));
                                }
                                allRenderers.Add(childClone);
                            }
                            else
                            {
                                renderer.name = renderer.sprite.name;
                                var uniqueSortingOrder = GetUniqueSortingOrder(renderer, allRenderers);
                                if ( !uniqueSortingOrder.Equals(renderer.sortingOrder) )
                                {
                                    renderer.sortingOrder = uniqueSortingOrder;
                                }
                                allRenderers.Add(renderer);
                            }
                        }
                }

                foreach ( var renderer in allRenderers )
                {
                    var currentLayerName = GetSortingLayerName(renderer);
                    TilemapLayer tilemapLayer;
                    if ( tilemapLayers.ContainsKey(currentLayerName) )
                    {
                        tilemapLayer = tilemapLayers[currentLayerName];
                    }
                    else
                    {
                        tilemapLayer = new TilemapLayer();
                        tilemapLayers[currentLayerName] = tilemapLayer;
                    }
                    if ( tilemapLayer == null )
                    {
                        continue;
                    }
                    UniTileData instance = CreateInstance<UniTileData>();
                    var parentObject = renderer.transform.parent.gameObject;
                    if ( parentObject )
                    {
                        instance.Name = parentObject.name;
                    }
                    var childtf = renderer.transform;
                    var parenttf = renderer.transform.parent.gameObject.transform;
                    //don't apply any rotation for tileconnects
                    var isTC = renderer.name.StartsWith(TC);
                    var zeroRot = Quaternion.Euler(0,0,0);

                    instance.ChildTransform = 
                        Matrix4x4.TRS(childtf.localPosition, isTC ? zeroRot : childtf.localRotation, childtf.localScale);

                    instance.Transform = 
                        Matrix4x4.TRS(parenttf.position, isTC ? zeroRot : parenttf.localRotation, parenttf.localScale);
                    
                    instance.OriginalSpriteName = renderer.sprite.name;
                    instance.SpriteName = renderer.name;
                    var assetPath = AssetDatabase.GetAssetPath(renderer.sprite.GetInstanceID());
                    instance.IsLegacy = looksLikeLegacy(assetPath, instance) && !isExcluded(assetPath);

                    string sheet = assetPath
                        .Replace("Assets/Resources/", "")
                        .Replace("Assets/textures/", "")
                        .Replace("Resources/", "")
                        .Replace(".png", "");
                    string overrideSheet;
                    instance.SpriteSheet = IsTileConnectWannabe(renderer, out overrideSheet) ? overrideSheet : sheet;
                    tilemapLayer.Add(x, y, instance);
                }
            }
        }

        foreach ( var layer in tilemapLayers )
        {
            Debug.LogFormat("{0}: {1}", layer.Key, layer.Value);
        }

        fsData data;
        new fsSerializer().TrySerialize(tilemapLayers, out data);
        File.WriteAllText(Application.dataPath + "/Resources/metadata/" + SceneManager.GetActiveScene().name + ".json",
            fsJsonPrinter.PrettyJson(data));

        //Cleanup
        foreach ( var o in tempGameObjects )
        {
            DestroyImmediate(o);
        }

        Debug.Log("Export kinda finished");
        AssetDatabase.Refresh();
    }

    private static int GetUniqueSortingOrder(SpriteRenderer renderer, List<SpriteRenderer> list)
    {
        return GetUniqueSortingOrderRecursive(
            new Tuple<string, int>(renderer.sortingLayerName, renderer.sortingOrder), list);
    }

    private static bool IsTileConnectWannabe(SpriteRenderer renderer)
    {
        string strEmpty;
        return IsTileConnectWannabe(renderer, out strEmpty);
    }

    private static bool IsTileConnectWannabe(SpriteRenderer renderer, out string newPath)
    {
        var parentObj = renderer.transform.parent.gameObject;
        var isTileConnectWannabe = parentObj && tileConnectWannabes.Any(tuple => parentObj.name.Contains(tuple.Item1));
        newPath = "";
        if ( isTileConnectWannabe )
        {
            newPath = tileConnectWannabes.Find(tuple => parentObj.name.Contains(tuple.Item1)).Item2;
        }
        return isTileConnectWannabe;
    }

    private static void TryMoveToSeparateLayer(SpriteRenderer renderer)
    {
        var parentObj = renderer.transform.parent.gameObject;
        var moveToSeparateLayer = parentObj && separateLayerMarkers.Any(parentObj.name.Contains);
        if ( moveToSeparateLayer )
        {
            renderer.sortingOrder += 50;
        }
    }

    private static int GetUniqueSortingOrderRecursive(Tuple<string, int> renderer, List<SpriteRenderer> list)
    {
        var overlapFound = list.Any(r => r.sortingLayerName.Equals(renderer.Item1)
                                         && r.sortingOrder.Equals(renderer.Item2));
        // increment sorting order by 100 if overlap is detected and try again
        if ( overlapFound )
        {
            return GetUniqueSortingOrderRecursive(new Tuple<string, int>(renderer.Item1, renderer.Item2 + 100), list);
        }
        return renderer.Item2;
    }

    private static bool isExcluded(string assetPath)
    {
        return legacyExclusionList.Any(assetPath.Contains);
    }

    private static bool looksLikeLegacy(string assetPath, UniTileData instance)
    {
        return assetPath.Contains("textures") && !instance.SpriteName.StartsWith(TC);
    }

    private static bool thisRendererSucks(SpriteRenderer spriteRenderer)
    {
        return !spriteRenderer || !spriteRenderer.sprite;
    }

//    /// <summary>
//    /// check whether all SR contain the same sprite
//    /// </summary>
//    private static bool SpritesMatch(SpriteRenderer[] children)
//    {
//        if ( children.Length < 2 )
//        {
//            return false;
//        }
//        string spritename = "";
//        foreach ( var child in children )
//        {
//            if ( child.sortingLayerID == 0 )
//            {
//                continue;
//            }
//            if ( spritename.Equals("") )
//            {
//                spritename = child.sprite.name;
//            }
//            if ( !spritename.Equals(child.sprite.name) )
//            {
//                return false;
//            }
//        }
//        return true;
//    }

//    internal static HashSet<string> GetSortingLayerOrderNames(IEnumerable<SpriteRenderer> renderers)
//    {
//        var hs = new HashSet<string>();
//        foreach ( var renderer in renderers )
//        {
//            hs.Add(renderer.sortingLayerName + renderer.sortingOrder);
//        }
//        return hs;
//    }

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