using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace UI.Core.SpriteProcessing
{
	/// <summary>
	/// Stores sprite scaling information for a texture. Most sprites don't fill up the area of a texture and are
	/// surrounded by transparent pixels. This class keeps data required to scale and center a sprite on a UI image
	/// so that it gives the appearance of having no extraneous transparent pixels around it.
	/// </summary>
	public class TextureMetadata
	{
		// Largest texture I've seen is WinDoor which is 768x768. This should cover most cases. Default sprite metadata
		// is used if the texture is larger.
		private const int PixelCacheSize = 768;

		public static readonly Color32[] PixelCache = new Color32[PixelCacheSize * PixelCacheSize];

		private Dictionary<Vector2Int, SpriteMetadata> SpritesMetadata { get; } =
			new Dictionary<Vector2Int, SpriteMetadata>();

		public SpriteMetadata GetSpriteData(Sprite sprite)
		{
			var rect = sprite.textureRect;
			var key = new Vector2Int((int)rect.x, (int)rect.y);

			SpritesMetadata.TryGetValue(key, out var spriteData);
			if (spriteData.Scale <= 0)
			{
				spriteData = CreateSpriteData(sprite.texture, rect);
				SpritesMetadata.Add(key, spriteData);
			}

			return spriteData;
		}

		private SpriteMetadata CreateSpriteData(Texture2D texture, in Rect spriteRect)
		{
			if (texture.isReadable == false)
			{
				Logger.LogWarning(
					$"Texture \"{texture.name}\" is not read enabled. Using default sprite metadata",
					Category.UI);
				return SpriteMetadata.Default;
			}
			var texWidth = texture.width;
			var texHeight = texture.height;
			var pixelCache = PixelCache;
			if (texWidth * texHeight > pixelCache.Length)
			{
				return SpriteMetadata.Default;
			}

			// Using GetPixelData and native arrays to avoid garbage created from GetPixels32. Indexing a native array
			// is slow though, so do a relatively quick copy to the pixel cache. Could avoid copying and the need for
			// a cache with raw pointers (requires unsafe).
			var pixels = texture.GetPixelData<Color32>(0);
			NativeArray<Color32>.Copy(pixels, PixelCache, pixels.Length);
			return SpriteMetadata.Create(texWidth, texHeight, spriteRect, pixelCache);
		}
	}
}
