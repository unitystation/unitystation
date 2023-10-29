
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

	/// <summary>
	/// Removes all children of this given GameObject.
	/// </summary>
	public static void DeleteAllChildren(this GameObject gameObject)
	{
		for (int i = 0; i < gameObject.transform.childCount; i++)
		{
			Object.Destroy(gameObject.transform.GetChild(i).gameObject);
		}
	}

	/// <summary>
	/// Finds all items/gameObjects with specific components that are nearby a target that aren't usually easily detected using physics functions.
	/// WARNING: This process is extremely slow, and goes over multiple frames (Can take up to 3 seconds depending on how large the scene is).
	/// This function will never stall the game/server unless you repeatedly call coroutine multiple times and end up creating a lot of GC.
	/// </summary>
	/// <param name="maximumDistance">The maximum distance between the component and the target (1 Tile is 1.25)</param>
	/// <param name="target">The target gameObject, usually the player.</param>
	/// <param name="result">What to do with this information once its all been processed?</param>
	/// <param name="matrix">What matrix do you want to scan?</param>
	/// <typeparam name="T">The component you're looking for</typeparam>
	/// <returns>A list of components nearby components to target.</returns>
	public static IEnumerator FindAllComponentsNearestToTarget<T>(this GameObject target, float maximumDistance, System.Action<List<T>> result, MatrixInfo matrix = null)
	{
		if (result == null)
		{
			Loggy.LogError("[MatrixManager/FindAllComponentsNearestToTarget()] - Cannot start coroutine without a result effect.");
			yield break;
		}
#if UNITY_EDITOR
		var stopwatch = new Stopwatch();
		stopwatch.Start();
#endif
		if (matrix == null) matrix = MatrixManager.MainStationMatrix;
		var currentIndex = 0;
		var maximumIndexes = 20;
		List<T> components = new List<T>();
		var things = matrix.Objects.GetComponentsInChildren<T>();
		foreach (var stationObject in things)
		{
			if (currentIndex >= maximumIndexes)
			{
				currentIndex = 0;
				yield return WaitFor.EndOfFrame;
			}
			var obj = stationObject as Component;
			if (Vector3.Distance(obj.gameObject.AssumedWorldPosServer(), target.AssumedWorldPosServer()) > maximumDistance)
			{
				continue;
			}
			else
			{
				components.Add(stationObject);
			}
			currentIndex++;
		}
		result.Invoke(components);
#if UNITY_EDITOR
		stopwatch.Stop();
		Loggy.Log($"[GameObject/FindAllComponentsNearestToTarget<T>()] - Operation took {stopwatch.Elapsed.Milliseconds}ms");
#endif
	}
}
