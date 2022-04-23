#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Tiles;
using UnityEditor;
using UnityEngine;


public static class PreviewSpriteBuilder
	{
		private const string previewPath = "Assets/Textures/TilePreviews";

		public static Sprite LoadSprite(Object obj)
		{
			return AssetDatabase.LoadAssetAtPath<Sprite>(GetSpritePath(obj));
		}

		public static void DeleteSprite(Object obj)
		{
			AssetDatabase.DeleteAsset(GetSpritePath(obj));
		}

		public static Sprite Create(GameObject gameObject)
		{
			if (gameObject == null)
			{
				return null;
			}

			IReadOnlyList<Sprite> sprites = GetObjectSprites(gameObject);

			return SaveSpriteToEditorPath(sprites, gameObject);
		}

		public static Sprite Create(MetaTile metaTile)
		{
			if (metaTile == null)
			{
				return null;
			}

			List<Sprite> sprites = new List<Sprite>();

			foreach (LayerTile tile in metaTile.GetTiles())
			{
				sprites.Add(tile.PreviewSprite);
			}

			return SaveSpriteToEditorPath(sprites, metaTile);
		}

		public static Sprite GetSpriteWithoutSaving(GameObject gameObject)
		{
			return MergeSprites(GetObjectSprites(gameObject));
		}

		private static IReadOnlyList<Sprite> GetObjectSprites(GameObject gameObject)
		{
			List<Sprite> sprites = new List<Sprite>();

			if (gameObject != null)
			{
				List<SpriteRenderer> renderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true).ToList();

				if (renderers.Count > 0)
				{
					renderers.Sort(RendererComparer.Compare);

					foreach (SpriteRenderer r in renderers)
					{
						sprites.Add(r.sprite);
					}
				}
			}

			return sprites;
		}

		private static Sprite MergeSprites(IReadOnlyList<Sprite> sprites)
		{
			if (sprites[0] == null) return null;

			Color[] colors = new Color[(int) (sprites[0].rect.width * sprites[0].rect.height)];
			foreach (Sprite s in sprites)
			{
				if (s == null) continue;
				Rect rect = s.rect;
				Color[] pixels = s.texture.GetPixels((int) rect.x, (int) rect.y, (int) rect.width, (int) rect.height);

				for (int i = 0; i < pixels.Length; i++)
				{
					Color px = pixels[i];

					if (px.a > 0)
					{
						colors[i] = colors[i] * (1 - px.a) + px * px.a;
					}
				}
			}

			Sprite old = sprites[0];
			Texture2D texture = new Texture2D((int) old.rect.width, (int) old.rect.height, old.texture.format, false);

			texture.SetPixels(colors);
			texture.Apply();

			return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f),
				old.pixelsPerUnit);
		}

		private static Sprite SaveSpriteToEditorPath(IReadOnlyList<Sprite> sprites, Object obj)
		{
			Sprite sprite = MergeSprites(sprites);

			string path = GetSpritePath(obj);

			string dir = Path.GetDirectoryName(path);

			if (dir == null)
			{
				return null;
			}

			Directory.CreateDirectory(dir);

			File.WriteAllBytes(path, sprite.texture.EncodeToPNG());

			AssetDatabase.Refresh();
			AssetDatabase.AddObjectToAsset(sprite, path);
			AssetDatabase.SaveAssets();

			TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

			if (textureImporter == null)
			{
				return null;
			}

			textureImporter.spritePixelsPerUnit = sprite.pixelsPerUnit;
			textureImporter.mipmapEnabled = false;
			textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
			textureImporter.filterMode = FilterMode.Point;
			textureImporter.isReadable = true;

			EditorUtility.SetDirty(textureImporter);
			textureImporter.SaveAndReimport();

			return AssetDatabase.LoadAssetAtPath<Sprite>(path);
		}


		private static string GetSpritePath(Object obj)
		{
			string assetPath = AssetDatabase.GetAssetPath(obj);
			assetPath = Path.ChangeExtension(assetPath, ".png");

			if (assetPath != null)
			{
				assetPath = assetPath.Replace("Assets", previewPath);
				assetPath = Regex.Replace(assetPath, "resources", "res", RegexOptions.IgnoreCase);
			}

			return assetPath;
		}
	}

#endif