using System;
using System.Collections.Generic;
using System.IO;
using FullSerializer;
using Matrix;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
public class MapToJSON : Editor
{
//FIXME: look into z-position export (possible lightbulb size fix?)
    //check weird turbine on top-left shuttle
    //figure out what to do with the floors
    //and shuttle corners
    //try to combine local transforms and check if it helps the bottom shuttle fire closet
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

        for (int y = 0; y < nodesMapped.GetLength(0); y++)
        {
            for (int x = 0; x < nodesMapped.GetLength(1); x++)
            {
                var node = nodesMapped[y, x];

                if (node == null)
                    continue;

                var spriteRenderers = new List<SpriteRenderer>();
                
                foreach (var tile in node.GetTiles())
                {
                    var children = tile.GetComponentsInChildren<SpriteRenderer>();
                    if ( children == null || children.Length < 1 ) continue;
                    
                    var renderer0 = children[0];
                    if ( thisRendererSucks(renderer0) ) 
                        continue;
                    if ( (children.Length.Equals(1) || SpritesMatch(children)) && renderer0.sortingLayerID != 0 )
                    {
                        renderer0.name = renderer0.sprite.name;
                        spriteRenderers.Add(renderer0);
                    }
                    else
                    {
                        var tileconnects = 0;
                        foreach ( var child in children )
                        {
                            if ( thisRendererSucks(child) || child.sortingLayerID == 0 ) 
                                continue;
                            if ( child.GetComponent<TileConnect>() )
                            {
                                tileconnects++;
                                if ( tileconnects != 4 ) continue;
                                if ( tileconnects > 4 )
                                {
                                    Debug.LogWarningFormat("{0} — more than 4 tileconnects found!", child.name);
                                }
                                // grouping four tileconnect sprites into a single temporary thing
                                GameObject tcMergeGameObject = Instantiate(child.gameObject, tile.transform.position, Quaternion.identity, tile.transform);
                                tempGameObjects.Add(tcMergeGameObject);
                                var childClone = tcMergeGameObject.GetComponent<SpriteRenderer>();
                                var spriteName = childClone.sprite.name;
                                
                                if ( spriteName.Contains("_") )
                                {
                                    childClone.name = "tc_" + spriteName.Substring(0, spriteName.LastIndexOf("_", StringComparison.Ordinal));
                                }
                                spriteRenderers.Add(childClone);
                            }
                            else
                            {
                                child.name = child.sprite.name;
                                spriteRenderers.Add(child);
                            }
                            
                        }

                    }       
                }

                //not sure if it's required anymore
//                spriteRenderers.Sort(MapToPNG.CompareSpriteRenderer);

                foreach (var sr in spriteRenderers)
                {
                    var currentLayerName = GetSortingLayerName(sr);
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
                    var parentObject = sr.transform.parent.gameObject;
                    if ( parentObject )
                    {
                        instance.Name = parentObject.name;
                    }
                    var childTransform = sr.transform;
                    instance.ChildTransform = Matrix4x4.TRS(childTransform.localPosition, childTransform.localRotation, childTransform.localScale);
                    var parentTransform = sr.transform.parent.gameObject.transform;
                    instance.Transform = Matrix4x4.TRS(parentTransform.position, parentTransform.localRotation, parentTransform.localScale);
                    instance.OriginalSpriteName = sr.sprite.name;
                    instance.SpriteName = sr.name;
                    var assetPath = AssetDatabase.GetAssetPath(sr.sprite.GetInstanceID());
                    instance.IsLegacy = assetPath.Contains("textures") && !instance.SpriteName.StartsWith("tc_");
                    instance.SpriteSheet = assetPath.Replace("Assets/Resources/","").Replace("Assets/textures/","").Replace("Resources/","").Replace(".png","");
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
        File.WriteAllText(Application.dataPath + "/Resources/metadata/" + SceneManager.GetActiveScene().name + ".json", fsJsonPrinter.PrettyJson(data));

        //Cleanup
        foreach (var o in tempGameObjects)
        {
            DestroyImmediate(o);
        }
        
        Debug.Log("Export kinda finished");
        AssetDatabase.Refresh();

    }

    private static bool thisRendererSucks(SpriteRenderer spriteRenderer)
    {
        return !spriteRenderer || !spriteRenderer.sprite;
    }

    /// <summary>
    /// check whether all SR contain the same sprite
    /// </summary>
    private static bool SpritesMatch(SpriteRenderer[] children)
    {
        if ( children.Length < 2 )
        {
            return false;
        }
        string spritename = "";
        foreach ( var child in children )
        {
            if ( child.sortingLayerID == 0 )
            {
                continue;
            }
            if ( spritename.Equals("") )
            {
                spritename = child.sprite.name;
            }
            if ( !spritename.Equals(child.sprite.name) )
            {
                return false;
            }
        }
        return true;
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
        var separateLayerMarker = "WarningLine";
        var parentObj = renderer.transform.parent.gameObject;
        if (parentObj && parentObj.name.Contains(separateLayerMarker))
        {
            return renderer.sortingLayerName + 33;
        }
        return renderer.sortingLayerName + renderer.sortingOrder;
    }

    internal static string GetCleanLayerName(string dirtyName)
    {
        var lameTrimChars = new[] {'1','2','3','4','5','6','7','8','9','0','-'};
        return dirtyName.TrimEnd(lameTrimChars);
    }

    internal static int CompareSpriteLayer(string x, string y)
    {
        var sortingLayerNames = MapToPNG.GetSortingLayerNames();
        var xTrim = GetCleanLayerName(x);
        var yTrim = GetCleanLayerName(y);
        var x_index = sortingLayerNames.FindIndex(s => s.StartsWith(xTrim));
        var y_index = sortingLayerNames.FindIndex(s => s.StartsWith(yTrim));

        if (x_index == y_index)
        {
            return  GetLayerOffset(y) - GetLayerOffset(x);
        }
        return y_index - x_index;
    }

    internal static int GetLayerOffset(string dirtyName)
    {
        return int.Parse(dirtyName.Replace(GetCleanLayerName(dirtyName),""));
    }
}
#endif