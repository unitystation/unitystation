
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Extensions for GameObject specifically for unitystation
/// </summary>
public static class GameObjectExtensions
{

	/// <summary>
	/// Creates garbage, use sparingly.
	///
	/// Get the tile-aligned (i.e. rounded to vector2int) world position of the specified object using RegisterTile,
	/// warning and defaulting to transform.position if it has no registertile.
	///
	/// Note this wil lreturn hiddenPos if the object is at hiddenpos, such as if it is inside something.
	/// If you want to know where it would be in the world based on what it's inside, use
	/// AssumedWorldPos instead of this
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public static Vector2Int TileWorldPosition(this GameObject obj)
	{
		var regTile = obj.GetComponent<RegisterTile>();
		if (regTile == null)
		{
			Logger.LogWarning("Attempting to get world position of object {0} which has no RegisterTile. " +
			                  "Transform.position will be used instead, which may cause unexpected behavior.");
			return obj.transform.position.To2Int();
		}
		else
		{
			return regTile.WorldPosition.To2Int();
		}
	}

	/// <summary>
	/// Creates garbage, use sparingly.
	///
	/// Get the tile-aligned (i.e. rounded to vector2int) local position of the specified object using RegisterTile,
	/// warning and defaulting to transform.localposition if it has no registertile.
	///
	/// If you want to know where it would be in the world based on what it's inside, use
	/// AssumedWorldPos instead of this
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public static Vector2Int TileLocalPosition(this GameObject obj)
	{
		var regTile = obj.GetComponent<RegisterTile>();
		if (regTile == null)
		{
			Logger.LogWarning("Attempting to get local position of object {0} which has no RegisterTile. " +
			                  "Transform.localposition will be used instead, which may cause unexpected behavior.");
			return obj.transform.localPosition.To2Int();
		}
		else
		{
			return regTile.LocalPosition.To2Int();
		}
	}

	/// <summary>
	/// Sets the given image's sprites using this object's main and secondary sprites
	/// </summary>
	/// <param name="obj"></param>
	/// <param name="image"></param>
	/// <param name="secondaryImage"></param>
	/// <param name="tertiaryImage"></param>
	public static void PopulateImageSprites(this GameObject obj, Image image, Image secondaryImage, Image tertiaryImage = null)
	{
		var sprites = obj.GetComponentsInChildren<SpriteRenderer>();
		if (sprites != null && sprites.Length > 0 && sprites[0].sprite != null)
		{
			image.enabled = true;
			image.sprite = sprites[0].sprite;
			if (sprites.Length > 1 && sprites[1].sprite != null)
			{
				secondaryImage.enabled = true;
				secondaryImage.sprite = sprites[1].sprite;
			}
			else
			{
				secondaryImage.enabled = false;
			}

			if (tertiaryImage == null)
			{
				return;
			}

			if (sprites.Length > 2 && sprites[2].sprite != null)
			{
				tertiaryImage.enabled = true;
				tertiaryImage.sprite = sprites[2].sprite;
			}
			else
			{
				tertiaryImage.enabled = false;
			}
		}
		else
		{
			image.enabled = false;
			secondaryImage.enabled = false;

			if (tertiaryImage != null)
			{
				tertiaryImage.enabled = false;
			}
		}
	}
}
