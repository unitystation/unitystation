using System;
using System.IO;
using Tiles;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class PaletteGenerator
{

	// Palettes are defined here.
	// Each array in this list should have the first string be what the palette should be named, then every string
	// after defines a folder in the Prefabs folder that should be added. Every subfolder under the defined will be
	// added. Multiple folders are able to be defined to further customize the palette- the kitchen, for example,
	// might want all foods, tables, and kitchen machines.
	private static string[][] _palettes =
	{
		new [] { "Botany", "Items/Botany", "Objects/Botany" },
		new [] { "Kitchen", "Items/Food", "Objects/Kitchen" },
		new [] { "Tools", "Items/Tools" }
	};

	// Palettes are GameObjects that have two pieces: the parent GameObject that defines a grid, and a child
	// GameObject (Layer1) that has a tilemap component.
	// Code adapted from connect.unity.com/o/programmatically-generate-a-tilemap-palette
	[MenuItem("Tools/Generate Tile Palettes")]
	public static void GenerateTileMapPalette()
	{
		var basePath = Application.dataPath + "/Resources/Prefabs/";

		var generatedFolder = Path.GetDirectoryName($"{ Application.dataPath }/Tilemaps/Palettes/Generated/");
		if (generatedFolder != null)
		{
			Directory.CreateDirectory(generatedFolder);
		}

		foreach (var palette in _palettes)
		{
			var title = palette[0];
			string subPath;
			string assetPath;
			string filePath;

			// Create palette asset
			var go = new GameObject();
			go.AddComponent<Grid>();

			var layerOne = new GameObject
			{
				name = "Layer1"
			};
			layerOne.transform.SetParent(go.transform);
			var tilemap = layerOne.AddComponent<Tilemap>();
			var tilemapRenderer = layerOne.AddComponent<TilemapRenderer>();
			tilemapRenderer.enabled = false;

			var y = 0;
			for (var i = 1; i < palette.Length; i++)
			{
				var x = 0;
				var folder = palette[i];
				var files = Directory.GetFiles(basePath + folder, "*.prefab", SearchOption.AllDirectories);
				foreach (var file in files)
				{
					var name = Path.GetFileNameWithoutExtension(file);

					subPath = $"Tilemaps/Resources/Tiles/Generated/{ title }/{ name }";
					assetPath = $"Assets/{ subPath }";
					filePath = $"{ Application.dataPath }/{ subPath }/{ name }.asset";

					if (File.Exists(filePath))
					{
						File.Delete(filePath);
					}

					x++;
				}

				y--;
			}

			var paletteInstance = ScriptableObject.CreateInstance<GridPalette>();
			paletteInstance.name = "Palette Settings";

			subPath = $"/Tilemaps/Palettes/Generated/{ title }.prefab";
			assetPath = $"Assets{ subPath }";
			filePath = $"{ Application.dataPath }{ subPath }";

			if (File.Exists(filePath))
			{
				AssetDatabase.DeleteAsset(assetPath);
				AssetDatabase.SaveAssets();
			}

			var prefab = PrefabUtility.SaveAsPrefabAsset(go, assetPath);
			AssetDatabase.AddObjectToAsset(paletteInstance, prefab);
			AssetDatabase.SaveAssets();

			GameObject.DestroyImmediate(go);
		}
	}
}
