using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace SecureStuff
{
	public readonly struct SpriteMetadata
	{
		public static readonly SpriteMetadata Default = new SpriteMetadata(Vector2.zero, 1f);

		public Vector2 Offset { get; }

		public float Scale { get; }

		private SpriteMetadata(Vector2 offset, float scale)
		{
			Offset = offset;
			Scale = scale;
		}

		public static unsafe SpriteMetadata Create(Texture2D texture, ref Rect spriteRect)
		{
			// Using GetPixelData and native arrays to avoid garbage created from GetPixels32. Indexing a native array
			// is slow though, so access the pointers directly.
			var pixels = texture.GetPixelData<Color32>(0);
			var texWidth = texture.width;
			int spriteX = (int)spriteRect.x;
			int spriteY = (int)spriteRect.y;
			int xMax = (int)spriteRect.xMax;
			int yMax = (int)spriteRect.yMax;
			var left = xMax;
			var top = spriteY;
			Color32* arrayPtr = (Color32*)pixels.GetUnsafeReadOnlyPtr();

			// Start in the top left. Check left to right, top to bottom (note: texture pixel data is stored bottom to top),
			// to find the top most and left most pixels. Loop through the x dimension using the left most non-transparent
			// pixel as the bound.
			for (int y = yMax; y > spriteY && left > spriteX; --y)
			{
				Color32* pixelPtr = arrayPtr + ((y - 1) * texWidth + spriteX);

				for (int x = spriteX; x < left; ++x, ++pixelPtr)
				{
					if (pixelPtr->a <= 0) continue;

					left = x;
					if (y > top)
					{
						top = y;
					}
				}
			}

			var right = spriteX;
			var bottom = yMax;

			// Now start in the bottom right to find the furthest bottom and right pixels. Loop through the x dimension
			// using the right most non-transparent pixel as the bound.
			for (int y = spriteY; y < top && right < xMax; ++y)
			{
				Color32* pixelPtr = arrayPtr + (y * texWidth + xMax - 1);

				for (int x = xMax; x > right; --x, --pixelPtr)
				{
					if (pixelPtr->a <= 0) continue;

					right = x;
					if (y < bottom)
					{
						bottom = y;
					}
				}
			}

			int spriteWidth = (int)spriteRect.width;
			int spriteHeight = (int)spriteRect.height;
			var center = new Vector2(spriteX + spriteWidth / 2f, spriteY + spriteHeight / 2f);
			var offsetX = (left - center.x + right - center.x) / 2;
			var offsetY = (top - center.y + bottom - center.y) / 2;
			var realSize = new Vector2(right - left, top - bottom);
			var offset = new Vector2(-offsetX, offsetY);
			var scale = realSize.x > realSize.y ? spriteWidth / realSize.x : spriteHeight / realSize.y;
			return new SpriteMetadata(offset, Math.Max(1, scale));
		}
	}
}
