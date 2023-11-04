using SecureStuff;
using UI.Core.SpriteProcessing;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Core
{
	public static class ImageExtensions
	{
		/// <summary>
		/// Modifies an image's pivot and scaling based on the non-transparent center and borders of a sprite.
		/// </summary>
		/// <param name="image">The UI Image to scale</param>
		/// <param name="sprite">The sprite to get scaling data from</param>
		/// <returns>The SpriteMetadata used to scale the image with</returns>
		public static SpriteMetadata ApplySpriteScaling(this Image image, Sprite sprite)
		{
			if (image == null || sprite == null) return SpriteMetadata.Default;

			var texData = TextureMetadataCache.GetTextureMetadataFor(sprite);

			if (texData == null) return SpriteMetadata.Default;

			var spriteData = texData.GetSpriteData(sprite);
			var imageTransform = (RectTransform)image.transform;
			var imageRect = imageTransform.rect;
			var adjustedSize = imageRect.width / sprite.textureRect.width;
			var newPivot = spriteData.Offset * adjustedSize;
			var localPos = imageTransform.localPosition;

			newPivot.x = 0.5f - newPivot.x / imageRect.width;
			newPivot.y = 0.5f + newPivot.y / imageRect.height;
			imageTransform.pivot = newPivot;
			imageTransform.localScale = new Vector3(spriteData.Scale, spriteData.Scale, 1);
			imageTransform.localPosition = localPos;

			return spriteData;
		}
	}
}
