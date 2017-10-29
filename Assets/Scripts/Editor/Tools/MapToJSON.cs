using System;
using System.Collections.Generic;
using System.IO;
using FullSerializer;
using Matrix;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapToJSON : Editor
{

    [MenuItem("Tools/Export map (JSON)")]
    static void Map2JSON()
    {
        // for smart stuff(doors, windoors): create new Tile with transform, no sprite and source GO (?)
        // 
        // export: (grilles, airlocks, firelocks, disposal, solars, wallmounts(incl.lights), furniture(chairs, beds, tables))
        /*  {
         *       ColliderType = Grid (all but wallmounts), none (wallmounts)
         */

        var nodesMapped = MapToPNG.GetMappedNodes();
        var tilemapLayers = new Dictionary<string, TilemapLayer>();
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

                spriteRenderers.Sort(MapToPNG.CompareSpriteRenderer);

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
                    instance.Transform = Matrix4x4.TRS(parentTransform.localPosition, parentTransform.localRotation, parentTransform.localScale);
                    instance.OriginalSpriteName = sr.sprite.name;
                    instance.SpriteName = sr.name;
                    instance.SpriteSheet = AssetDatabase.GetAssetPath(sr.sprite.GetInstanceID()).Replace("Assets/Resources/","").Replace("Assets/textures/","").Replace("Resources/","").Replace(".png","");
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
        File.WriteAllText(Application.dataPath + "/../" + SceneManager.GetActiveScene().name + ".json", fsJsonPrinter.PrettyJson(data));

        //Cleanup
        foreach (var o in tempGameObjects)
        {
            Destroy(o);
        }
        
        Debug.Log("Export kinda finished");
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
        return renderer.sortingLayerName + renderer.sortingOrder;
    }

}