using System.Collections.Generic;
using Logs;
using SecureStuff;
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
		private Dictionary<Vector2Int, SpriteMetadata> SpritesMetadata { get; } =
			new Dictionary<Vector2Int, SpriteMetadata>();

		public SpriteMetadata GetSpriteData(Sprite sprite)
		{
			var rect = sprite.textureRect;
			var key = new Vector2Int((int)rect.x, (int)rect.y);

			SpritesMetadata.TryGetValue(key, out var spriteData);
			if (spriteData.Scale <= 0)
			{
				spriteData = CreateSpriteData(sprite.texture, ref rect);
				SpritesMetadata.Add(key, spriteData);
			}

			return spriteData;
		}

		private SpriteMetadata CreateSpriteData(Texture2D texture, ref Rect spriteRect)
		{
			if (texture.isReadable == false)
			{
				Loggy.LogWarning(
					$"Texture \"{texture.name}\" is not read enabled. Using default sprite metadata",
					Category.Sprites);
				return SpriteMetadata.Default;
			}

			return SpriteMetadata.Create(texture, ref spriteRect);
		}
	}
}
