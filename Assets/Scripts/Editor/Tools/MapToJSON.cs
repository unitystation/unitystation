using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MapToJSON : Editor
{

    [MenuItem("Tools/Export map (JSON)")]
    static void Map2JSON()
    {
        var nodesMapped = MapToPNG.GetMappedNodes();

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

                foreach (var tile in node.GetTiles())
                {
                    foreach (var spriteRenderer in tile.GetComponentsInChildren<SpriteRenderer>())
                    {
                        if (!spriteRenderer || !spriteRenderer.sprite || spriteRenderer.sortingLayerID == 0)
                            continue;

                        spriteRenderers.Add(spriteRenderer);
                    }
                }

                spriteRenderers.Sort(MapToPNG.CompareSpriteRenderer);

                foreach (var sr in spriteRenderers)
                {
//                    var sprite = sr.sprite;

//                    var pixels = sprite.texture.GetPixels((int) sprite.rect.x,
//                        (int) sprite.rect.y,
//                        (int) sprite.rect.width,
//                        (int) sprite.rect.height);

//                    var texWidth = sprite.rect.width;
//                    var texHeight = sprite.rect.height;
//                    var localX = sr.transform.localPosition.x;
//                    var localY = sr.transform.localPosition.y;

//                    var texX = (int) ((x + (1 - (texWidth / 32)) / 2 + localX) * 32);
//                    var texY = (int) ((y + (1 - (texHeight / 32)) / 2 + localY) * 32);

//                    for (int x1 = 0; x1 < texWidth; x1++)
//                    {
//                        for (int y1 = 0; y1 < texHeight; y1++)
//                        {
//                            var px = pixels[y1 * (int) sprite.rect.width + x1];
//                            var i = (texY + y1) * nodesMapped.GetLength(1) * 32 + texX + x1;
//                            if (px.a > 0)
//                            {
//                                colors[i] = colors[i] * (1 - px.a) + px * px.a;
//                            }
//                        }
//                    }
                }
            }
        }

//        mapTexture.SetPixels(0, 0, nodesMapped.GetLength(1) * 32, nodesMapped.GetLength(0) * 32, colors);
//        mapTexture.Apply();

//        byte[] bytes = mapTexture.EncodeToPNG();
//        new fsSerializer().TrySerialize()

//        File.WriteAllBytes(Application.dataPath + "/../" + SceneManager.GetActiveScene().name + ".png", bytes);

        Debug.Log("Export finished");
    }
}