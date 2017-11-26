using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ImportSprites : MonoBehaviour
{
    [MenuItem("Assets/Sprites/Slice Spritesheet", false, 1000)]
    public static void ImportObjects()
    {
        foreach (var obj in Selection.objects)
        {
            ImportObject(obj);
        }
    }

    private static void ImportObject(Object obj)
    {
        var path = AssetDatabase.GetAssetPath(obj);

        var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

        if (textureImporter == null)
        {
            return;
        }

        textureImporter.spritePixelsPerUnit = 32;
        textureImporter.mipmapEnabled = false;
        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        textureImporter.filterMode = FilterMode.Point;
        textureImporter.isReadable = true;

        EditorUtility.SetDirty(textureImporter);
        textureImporter.SaveAndReimport();


        const int sliceWidth = 32;
        const int sliceHeight = 32;

        SpliceSpriteSheet(path, sliceWidth, sliceHeight, textureImporter);
    }

    private static void SpliceSpriteSheet(string path, int sliceWidth, int sliceHeight, TextureImporter textureImporter)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        List<SpriteMetaData> newData = new List<SpriteMetaData>();

        var index = 0;
        var name = Path.GetFileNameWithoutExtension(path);

        for (var y = texture.height; y > 0; y -= sliceHeight)
        {
            for (var x = 0; x < texture.width; x += sliceWidth)
            {
                newData.Add(new SpriteMetaData
                {
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = 9,
                    name = name + "_" + index,
                    rect = new Rect(x, y - sliceHeight, sliceWidth, sliceHeight)
                });

                index++;
            }
        }

        textureImporter.spritesheet = newData.ToArray();
        textureImporter.spriteImportMode = SpriteImportMode.Single;
        textureImporter.spriteImportMode = SpriteImportMode.Multiple;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }
}