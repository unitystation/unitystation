using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MapToJSON : Editor
{

    [MenuItem("Tools/Export map (JSON)")]
    static void Map2JSON()
    {
        //TODO: for each layer(GO or SR+OrderInLayer): get mapped nodes
        // then detect dumb stuff: floors(RTile: Floor) and walls(RTile: Wall)
        // for dumb stuff: create new Tile at its coords with:
        //     - source GO's transform
        //     - no gameObject?
        //     - sprite: floor - just the sprite; walls - figure out spritesheet and select zero sprite
        // for smart stuff(doors, windoors): create new Tile with transform, no sprite and source GO (?)
        // 
        // export: (grilles, airlocks, firelocks, disposal, solars, wallmounts(incl.lights), furniture(chairs, beds, tables))
        /*  {
         *  "name/layer": "floor"
         *  (int[2,] -> Vector3Int[]) "positions":
         *    [
         *      { 1, 1 },
         *      { 1, 2 }
         *    ]
         *  (UniTile[]) "tiles":
         *    [ 
         *      {
         *       Name = GO.name
         *       Transform = Matrix4x4.TRS (GO localoffset, GO rotation, GO scale)
         *       (if only one child SR found) ChildTransform = Matrix4x4.TRS (SR localoffset, SR rotation, SR scale)
         *       Sprite = SR.sprite
         *       ColliderType = Grid (all but wallmounts), none (wallmounts)
         *      },
         *      { ...
         *      }
         *    ]
         *  }
         */

        var nodesMapped = MapToPNG.GetMappedNodes();
        var tilemapLayers = new Dictionary<string, TilemapLayer>();

//        var mapTexture = new Texture2D(nodesMapped.GetLength(1) * 32, nodesMapped.GetLength(0) * 32);
//        Color[] colors = new Color[nodesMapped.GetLength(1) * 32 * nodesMapped.GetLength(0) * 32];

        for (int y = 0; y < nodesMapped.GetLength(0); y++)
        {
            for (int x = 0; x < nodesMapped.GetLength(1); x++)
            {
                var node = nodesMapped[y, x];

                if (node == null)
                    continue;

                var spriteRenderers = new List<SpriteRenderer>();
//                var gameObjects = new List<GameObject>();

                foreach (var tile in node.GetTiles())
                {
                    var children = tile.GetComponentsInChildren<SpriteRenderer>();
                    if ( children == null || children.Length < 1 ) continue;
                    
                    var spriteRenderer = children[0];
                    if ( !spriteRenderer || !spriteRenderer.sprite || spriteRenderer.sortingLayerID == 0 ) 
                        continue;
                    if ( children.Length.Equals(1) )
                    {
                        spriteRenderers.Add(spriteRenderer);
                    }
                    else
                    {
                        Debug.LogFormat("Several SR children detected, this one's called {0}, mimicking it", spriteRenderer.sprite.name);
                        GameObject singleChild = new GameObject(tile.name+"_singleChild");
                        singleChild.transform.parent = tile.transform;
                        var singleSr = singleChild.AddComponent<SpriteRenderer>();
                        singleSr.name = spriteRenderer.name;
                        singleSr.sprite = spriteRenderer.sprite;
                        singleSr.sortingLayerName = spriteRenderer.sortingLayerName;
                        singleSr.sortingLayerID = spriteRenderer.sortingLayerID;
                        singleSr.sortingOrder = spriteRenderer.sortingOrder;
                        spriteRenderers.Add(singleSr);
                    }       
                }

                spriteRenderers.Sort(MapToPNG.CompareSpriteRenderer);

                foreach (var sr in spriteRenderers)
                {
                    //todo determine all sortinglayers and add each SR to corresponding slayer
                    var currentLayerName = GetSortingLayerName(sr);
                    TilemapLayer tilemapLayer;
                    //following code is smelly
                    if ( tilemapLayers.ContainsKey(currentLayerName) )
                    {
                        tilemapLayer = tilemapLayers[currentLayerName];
                    }
                    else
                    {
                        tilemapLayer = new TilemapLayer(currentLayerName);
                        tilemapLayers[currentLayerName] = tilemapLayer;
                    }
                    if ( tilemapLayer == null )
                    {
                        continue;
                    }
                    UniTile instance = CreateInstance<UniTile>();
                    var parentObject = sr.gameObject;
                    instance.name = parentObject.name;
                    var childTransform = sr.transform;
                    instance.ChildTransform = Matrix4x4.TRS(childTransform.localPosition, childTransform.localRotation, childTransform.localScale);
                    var parentTransform = sr.transform.parent.gameObject.transform;
                    instance.transform = Matrix4x4.TRS(parentTransform.localPosition, parentTransform.localRotation, parentTransform.localScale);
                    instance.sprite = sr.sprite;
                    tilemapLayer.Add(x, y, instance);

                }
            }
        }

        foreach ( var layer in tilemapLayers )
        {
            Debug.Log(layer.Value);
        }

//        mapTexture.SetPixels(0, 0, nodesMapped.GetLength(1) * 32, nodesMapped.GetLength(0) * 32, colors);
//        mapTexture.Apply();
//        byte[] bytes = mapTexture.EncodeToPNG();
//        new fsSerializer().TrySerialize()
//        File.WriteAllBytes(Application.dataPath + "/../" + SceneManager.GetActiveScene().name + ".png", bytes);

        Debug.Log("Export kinda finished");
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