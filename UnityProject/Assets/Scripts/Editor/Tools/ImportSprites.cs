using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ImportSprites : MonoBehaviour
{
	[MenuItem("Assets/Sprites/Slice Spritesheet", false, 1000)]
	public static void ImportObjects()
	{
		foreach (Object obj in Selection.objects)
		{
			ImportObject(obj);
		}
	}

	private static void ImportObject(Object obj)
	{
		string path = AssetDatabase.GetAssetPath(obj);

		TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

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
		Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		List<SpriteMetaData> newData = new List<SpriteMetaData>();

		int index = 0;
		string name = Path.GetFileNameWithoutExtension(path);

		for (int y = texture.height; y > 0; y -= sliceHeight)
		{
			for (int x = 0; x < texture.width; x += sliceWidth)
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