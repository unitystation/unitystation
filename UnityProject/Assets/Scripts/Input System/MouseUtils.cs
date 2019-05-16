using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Utilities related to mouse / pointer interactions, such as figuring out what is under the mouse
/// </summary>
public static class MouseUtils
{

	/// <summary>
	/// Gets the renderers under the mouse, ordered so that highest item comes first
	/// </summary>
	/// <param name="layerMask">layers to check for hits in</param>
	/// <param name="gameObjectFilter"> optional filter to apply to each game object before sorting, the final list will only
	/// include spriterenderers whose game objects match the condition</param>
	/// <returns>the ordered sprite renderers that were under the mouse, top first</returns>
	public static IOrderedEnumerable<SpriteRenderer> GetOrderedObjectsUnderMouse(LayerMask layerMask, Func<GameObject,bool> gameObjectFilter = null)
	{
		var result = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(CommonInput.mousePosition), Vector2.zero, 10f,
				layerMask)
			//get the hit game object
			.Select(hit => hit.collider.transform.gameObject);
		if (gameObjectFilter != null)
		{
			//apply the GO filter
			result = result.Where(gameObjectFilter);
		}


		return result
			//check for a pixel hit
			.Select(go => MouseUtils.IsPixelHit(go.transform))
			.Where(r => r != null)
			//order by sort layer
			.OrderByDescending(r => SortingLayer.GetLayerValueFromID(r.sortingLayerID))
			//then by sort order
			.ThenByDescending(renderer => renderer.sortingOrder);
	}

	/// <summary>
	/// Checks if there is a non transparent pixel in a renderer in this transform under the mouse
	/// </summary>
	/// <param name="transform">transform to check</param>
	/// <param name="recentTouches">Dictionary from touch position to the pixel color and time the touch occurred,
	/// will be updated with the hit information if a hit occurs.</param>
	/// <returns>sprite renderer that was hit if there is a hit</returns>
	public static SpriteRenderer IsPixelHit(Transform transform, Dictionary<Vector2, Tuple<Color, float>> recentTouches = null)
	{
		SpriteRenderer[] spriteRenderers = transform.GetComponentsInChildren<SpriteRenderer>(false);

		//check order in layer for what should be triggered first
		//each item ontop of a table should have a higher order in layer
		SpriteRenderer[] bySortingOrder = spriteRenderers.OrderByDescending(sRenderer => sRenderer.sortingOrder).ToArray();

		for (var i = 0; i < bySortingOrder.Length; i++)
		{
			SpriteRenderer spriteRenderer = bySortingOrder[i];
			Sprite sprite = spriteRenderer.sprite;

			if (spriteRenderer.enabled && sprite && spriteRenderer.color.a > 0)
			{
				MouseUtils.GetSpritePixelColorUnderMousePointer(spriteRenderer, out Color pixelColor);
				if (pixelColor.a > 0)
				{
					var mousePos = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);
					if (recentTouches != null)
					{
						if (recentTouches.ContainsKey(mousePos))
						{
							recentTouches.Remove(mousePos);
						}

						recentTouches.Add(mousePos, new Tuple<Color, float>(pixelColor, Time.time));
					}

					return spriteRenderer;
				}
			}
		}

		return null;
	}
	/// <summary>
	/// Gets the sprite pixel of this sprite renderer that is under the mouse position
	/// </summary>
	/// <param name="spriteRenderer">renderer to check</param>
	/// <param name="color">color at that position</param>
	/// <returns>true if there is an intersection with the renderer</returns>
	public static bool GetSpritePixelColorUnderMousePointer(SpriteRenderer spriteRenderer, out Color color)
	{
		color = new Color();
		Camera cam = Camera.main;
		Vector2 mousePos = CommonInput.mousePosition;
		Vector2 viewportPos = cam.ScreenToViewportPoint(mousePos);
		if (viewportPos.x < 0.0f || viewportPos.x > 1.0f || viewportPos.y < 0.0f || viewportPos.y > 1.0f) return false; // out of viewport bounds
																														// Cast a ray from viewport point into world
		Ray ray = cam.ViewportPointToRay(viewportPos);

		// Check for intersection with sprite and get the color
		return IntersectsSprite(spriteRenderer, ray, out color);
	}

	private static bool IntersectsSprite(SpriteRenderer spriteRenderer, Ray ray, out Color color)
	{
		color = new Color();
		if (spriteRenderer == null) return false;
		Sprite sprite = spriteRenderer.sprite;
		if (sprite == null) return false;
		Texture2D texture = sprite.texture;
		if (texture == null) return false;
		// Check atlas packing mode
		if (sprite.packed && sprite.packingMode == SpritePackingMode.Tight)
		{
			// Cannot use textureRect on tightly packed sprites
			Logger.LogError("SpritePackingMode.Tight atlas packing is not supported!", Category.Graphics);
			// TODO: support tightly packed sprites
			return false;
		}
		// Craete a plane so it has the same orientation as the sprite transform
		Plane plane = new Plane(spriteRenderer.transform.forward, (Vector2)spriteRenderer.transform.position); //????????
																				 // Intersect the ray and the plane
		float rayIntersectDist; // the distance from the ray origin to the intersection point
		if (!plane.Raycast(ray, out rayIntersectDist)) return false; // no intersection
																	 // Convert world position to sprite position
																	 // worldToLocalMatrix.MultiplyPoint3x4 returns a value from based on the texture dimensions (+/- half texDimension / pixelsPerUnit) )
																	 // 0, 0 corresponds to the center of the TEXTURE ITSELF, not the center of the trimmed sprite textureRect
		Vector3 spritePos = spriteRenderer.worldToLocalMatrix.MultiplyPoint3x4(ray.origin + (ray.direction * rayIntersectDist));
		Rect textureRect = sprite.textureRect;
		float pixelsPerUnit = sprite.pixelsPerUnit;
		float halfRealTexWidth = sprite.rect.width * 0.5f;
		float halfRealTexHeight = sprite.rect.height * 0.5f;

		int texPosX = (int)(sprite.rect.x + (spritePos.x * pixelsPerUnit + halfRealTexWidth));
		int texPosY = (int)(sprite.rect.y + (spritePos.y * pixelsPerUnit + halfRealTexHeight));

		// Check if pixel is within texture
		if (texPosX < 0 || texPosX < textureRect.x || texPosX >= Mathf.FloorToInt(textureRect.xMax)) return false;
		if (texPosY < 0 || texPosY < textureRect.y || texPosY >= Mathf.FloorToInt(textureRect.yMax)) return false;
		// Get pixel color
		color = texture.GetPixel(texPosX, texPosY);
		return true;
	}

}
