using System.Collections.Generic;
using Logs;
using UnityEngine;

namespace UI.Core.SpriteProcessing
{
	public static class TextureMetadataCache
	{
		#if UNITY_EDITOR
		private static Dictionary<Texture2D, TextureMetadata> cache;

		[RuntimeInitializeOnLoadMethod]
		private static void ResetTexturesMetadata()
		{
			cache = new Dictionary<Texture2D, TextureMetadata>();
		}
		#else
		private static readonly Dictionary<Texture2D, TextureMetadata> cache =
			new Dictionary<Texture2D, TextureMetadata>();
		#endif

		public static TextureMetadata GetTextureMetadataFor(Sprite sprite)
		{
			if (sprite == null) return null;

			var texture = sprite.texture;

			if (texture == null)
			{
				Loggy.LogWarning($"No texture found for sprite \"{sprite.name}\". Unable to create sprite metadata.",
					Category.Sprites);
				return null;
			}

			cache.TryGetValue(texture, out var scaler);
			if (scaler == null)
			{
				scaler = new TextureMetadata();
				cache.Add(texture, scaler);
			}
			return scaler;
		}
	}
}