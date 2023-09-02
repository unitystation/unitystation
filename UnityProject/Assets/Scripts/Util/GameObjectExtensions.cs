
using System.Collections.Generic;
using Logs;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Extensions for GameObject specifically for unitystation
/// </summary>
public static class GameObjectExtensions
{
	/// <summary>
	/// Returns the unity engine object or null if it has been destroyed. Useful for null coalescing or propagation.
	/// </summary>
	/// <param name="obj"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static T OrNull<T>(this T obj) where T : Object => obj ? obj : null;

	/// <summary>
	/// Tries to activate/deactivate the gameObject associated with this component.
	/// </summary>
	/// <param name="component"></param>
	/// <param name="active"></param>
	/// <typeparam name="T"></typeparam>
	public static void SetActive<T>(this T component, bool active) where T : Component =>
		component.OrNull()?.gameObject.SetActive(active);

	/// <summary>
	/// Returns the existing component stored in the reference. If the reference is null, it will get the component from
	/// the object, set the reference, and return it. Prefer Awake over this unless you need to be able get a component
	/// on an instantiated but not yet activated game object.
	/// </summary>
	/// <param name="obj">The object to get the component from if the component is null.</param>
	/// <param name="component">A reference to the component.</param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static T GetComponentByRef<T>(this Component obj, ref T component) where T : Component
	{
		if (component != null)
		{
			return component;
		}
		component = obj.GetComponent<T>();
		return component;
	}

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
			Loggy.LogWarning("Attempting to get world position of object {0} which has no RegisterTile. " +
			                  "Transform.position will be used instead, which may cause unexpected behavior.", Category.Matrix);
			return obj.transform.position.RoundTo2Int();
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
			Loggy.LogWarning("Attempting to get local position of object {0} which has no RegisterTile. " +
			                  "Transform.localposition will be used instead, which may cause unexpected behavior.", Category.Matrix);
			return obj.transform.localPosition.RoundTo2Int();
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

	/// <summary>
	/// performant, robust, alloc free way to check if the object is hidden (at hiddenpos), which occurs when it is pooled (despawned)
	/// or otherwise obscured from players (items in inventory / things in lockers).
	///
	/// Merely checking if its transform is at hiddenpos won't work, because objects may be moved / rotated
	/// when their parent matrix is moved (even hidden objects exist within a matrix)...instead we
	/// just rely on the z coordinate
	/// </summary>
	/// <param name="gameObject"></param>
	/// <returns></returns>
	public static bool IsAtHiddenPos(this GameObject gameObject)
	{
		// just to be sure, we give a little buffer in z coordinate, so -90 or lower will
		// be considered hidden (nothing in the game should be this low except hidden stuff)
		return gameObject.transform.position.z <= (TransformState.HiddenPos.z + 10);
	}
}
