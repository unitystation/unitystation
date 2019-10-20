
using UnityEngine;

/// <summary>
/// Extensions for GameObject specifically for unitystation
/// </summary>
public static class GameObjectExtensions
{

	/// <summary>
	/// Creates garbage, use sparingly.
	///
	/// Get the tile-aligned (i.e. rounded to vector2int) world position of the specified object using RegisterTile,
	/// warning and defaulting to transform.position if it has no registertile
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
	/// warning and defaulting to transform.localposition if it has no registertile
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

}
