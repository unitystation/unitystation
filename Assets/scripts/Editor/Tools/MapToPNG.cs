﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Matrix;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapToPNG : UnityEditor.Editor
{
    private static List<string> sortingLayerNames = GetSortingLayerNames();

    [MenuItem("Tools/Make Map PNG")]
    static void Map2PNG()
    {
        var nodesMapped = GetMappedNodes();
        var emcsMapped = GetEditModeControls();

        var mapTexture = new Texture2D(nodesMapped.GetLength(1) * 32, nodesMapped.GetLength(0) * 32);

        Color[] colors = new Color[nodesMapped.GetLength(1) * 32 * nodesMapped.GetLength(0) * 32];

        for (int y = 0; y < nodesMapped.GetLength(0); y++)
        {
            for (int x = 0; x < nodesMapped.GetLength(1); x++)
            {
                var n = nodesMapped[y, x];
                EditModeControl e = null;
                if ( emcsMapped.GetLength(0) > y && emcsMapped.GetLength(1) > x )
                {
                    e = emcsMapped[y, x];
                }

                if (n == null)
                    continue;
                var spriteRenderers = new List<SpriteRenderer>();

                var gameObjects = n.GetTiles();
                
                // +Items
                foreach ( var item in n.GetItems() )
                {
                    gameObjects.Add(item.gameObject);
                }
                
                //+Other EditModeControls (like wallmounts)
                if ( e != null )
                {
                    Debug.LogFormat("EditModeControl found for {0},{1}: {2}",x,y,e.gameObject.name);
                    gameObjects.Add( e.gameObject );
                }
                
                foreach (var t in gameObjects)
                {
                    foreach (var sr in t.GetComponentsInChildren<SpriteRenderer>())
                    {
                        if (!sr || !sr.sprite || sr.sortingLayerID == 0)
                            continue;

                        spriteRenderers.Add(sr);
                    }
                }

                spriteRenderers.Sort(CompareSpriteRenderer);

                foreach (var sr in spriteRenderers)
                {
                    var sprite = sr.sprite;

                    var pixels = sprite.texture.GetPixels((int) sprite.rect.x,
                        (int) sprite.rect.y,
                        (int) sprite.rect.width,
                        (int) sprite.rect.height);

                    var texWidth = sprite.rect.width;
                    var texHeight = sprite.rect.height;
                    var localX = sr.transform.localPosition.x;
                    var localY = sr.transform.localPosition.y;

                    var texX = (int) ((x + (1 - (texWidth / 32)) / 2 + localX) * 32);
                    var texY = (int) ((y + (1 - (texHeight / 32)) / 2 + localY) * 32);

                    for (int x1 = 0; x1 < texWidth; x1++)
                    {
                        for (int y1 = 0; y1 < texHeight; y1++)
                        {
                            var px = pixels[y1 * (int) sprite.rect.width + x1];

                            var i = (texY + y1) * nodesMapped.GetLength(1) * 32 + texX + x1;

                            if (px.a > 0)
                            {
                                colors[i] = colors[i] * (1 - px.a) + px * px.a;
                            }
                        }
                    }
                }
            }
        }

        mapTexture.SetPixels(0, 0, nodesMapped.GetLength(1) * 32, nodesMapped.GetLength(0) * 32, colors);
        mapTexture.Apply();

        byte[] bytes = mapTexture.EncodeToPNG();

        File.WriteAllBytes(Application.dataPath + "/../" + SceneManager.GetActiveScene().name + ".png", bytes);

        Debug.Log("Making Map Image Done");
    }

    private static EditModeControl[,] GetEditModeControls()
    {
        var x = new List<int>();
        var y = new List<int>();
        
        var allObjects = FindObjectsOfType<GameObject>() ;
        var emcs = new List<EditModeControl>();
        foreach(GameObject go in allObjects)
        {
            if ( go.activeInHierarchy )
            {
                var editModeControl = go.GetComponent<EditModeControl>();
                var ambientTile = go.GetComponent<AmbientTile>();
                var registerTile = go.GetComponent<RegisterTile>();
                var itemAttributes = go.GetComponent<ItemAttributes>();
                if ( editModeControl && !ambientTile && !registerTile && !itemAttributes )
                {
                    emcs.Add(editModeControl);
                    var localPositionX = (int) go.transform.position.x;
                    x.Add(localPositionX);
                    var localPositionY = (int) go.transform.position.y;
                    y.Add(localPositionY);
//                    x.Add((int) (k >> 32));
//                    y.Add((int) (k & int.MaxValue));
                    Debug.LogFormat("EMC {0} x={1} y={2}",go.name,localPositionX,localPositionY);
                }
            }
        }
        
        int minX = Mathf.Min(x.ToArray());
        int minY = Mathf.Min(y.ToArray());
        int maxX = Mathf.Max(x.ToArray());
        int maxY = Mathf.Max(y.ToArray());

        int width = maxX - minX;
        int height = maxY - minY;

        EditModeControl[,] emcsMapped = new EditModeControl[height + 1, width + 1];

        for (int i = 0; i < emcs.Count; i++)
        {
            emcsMapped[y[i] - minY, x[i] - minX] = emcs[i];
        }

        return emcsMapped;
    }

    private static MatrixNode[,] GetMappedNodes()
    {
        var keys = Matrix.Matrix.Nodes.keys;
        var values = Matrix.Matrix.Nodes.values;

        var x = new List<int>();
        var y = new List<int>();

        var nodes = new List<MatrixNode>();

        for (int i = 0; i < keys.Count; i++)
        {
            var k = keys[i];
            var v = values[i];

            if (v.GetTiles().Count > 0 || v.GetItems().Count > 0)
            {
                nodes.Add(v);
                var localPositionX = (int) (k >> 32);
                x.Add(localPositionX);
                var localPositionY = (int) (k & int.MaxValue);
                y.Add(localPositionY);
//                Debug.LogFormat("Node x={0} y={1}",localPositionX,localPositionY);
            }
        }

        int minX = Mathf.Min(x.ToArray());
        int minY = Mathf.Min(y.ToArray());
        int maxX = Mathf.Max(x.ToArray());
        int maxY = Mathf.Max(y.ToArray());

        int width = maxX - minX;
        int height = maxY - minY;

        MatrixNode[,] nodesMapped = new MatrixNode[height + 1, width + 1];

        for (int i = 0; i < nodes.Count; i++)
        {
            nodesMapped[y[i] - minY, x[i] - minX] = nodes[i];
        }

        return nodesMapped;
    }

    private static int CompareSpriteRenderer(SpriteRenderer x, SpriteRenderer y)
    {
        var x_index = sortingLayerNames.FindIndex(s => s.Equals(x.sortingLayerName));
        var y_index = sortingLayerNames.FindIndex(s => s.Equals(y.sortingLayerName));

        if (x_index == y_index)
        {
            return x.sortingOrder - y.sortingOrder;
        }
        return x_index - y_index;
    }


    private static List<string> GetSortingLayerNames()
    {
        var internalEditorUtilityType = typeof(InternalEditorUtility);
        PropertyInfo sortingLayersProperty =
            internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
        var sortingLayerNames = (string[]) sortingLayersProperty.GetValue(null, new object[0]);

        return new List<string>(sortingLayerNames);
    }
}