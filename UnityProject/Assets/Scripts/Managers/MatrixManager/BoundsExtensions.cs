using TileManagement;
using UnityEngine;

/// <summary>
/// Bounds/Rect-related extensions
/// </summary>
public static class BoundsExtensions
{
	public static bool BoundsIntersect(this MatrixInfo matrix, MatrixInfo otherMatrix, out BetterBounds intersection)
	{
		intersection = new BetterBounds();
		if (matrix == null || otherMatrix == null || matrix == otherMatrix)
		{
			return false;
		}

		var rect = matrix.WorldBounds;
		var Otherrect = otherMatrix.WorldBounds;
		return rect.Intersects(Otherrect, out intersection);
	}

	public static Rect ToRect(this Bounds bounds)
	{
		return new Rect(bounds.min, bounds.size);
	}
	public static RectInt ToRectInt(this Bounds bounds)
	{
		return new RectInt(bounds.min.RoundTo2Int(), bounds.size.RoundTo2Int());
	}

	private static readonly Vector3Int ZOneVector3Int = new Vector3Int(0,0,1);
	private static readonly Vector3Int Vector3IntOneCut = new Vector3Int(1,1,0);

	public static BoundsInt ToBoundsInt(this Rect rect)
	{ //Bounds are 3d and require Z of at least 1 to work, so it's Z 0 -> 1 here
		return new BoundsInt(rect.min.RoundToInt(), rect.size.RoundToInt() + ZOneVector3Int);
	}

	/// <summary>
	/// Extend/shrink bounds on x,y sides by integer amount
	/// </summary>
	public static Bounds Extend(this Bounds bounds, int amount)
	{
		var min = bounds.min - (Vector3IntOneCut * amount);
		var max = bounds.max + (Vector3IntOneCut * amount);
		return new Bounds(min, max - min);
	}

	public static bool Intersects(this Rect r1, Rect r2, out Rect area)
	{
		area = Rect.zero;

		if (r2.Overlaps(r1))
		{
			area = new Rect();
			float x1 = Mathf.Min(r1.xMax, r2.xMax);
			float x2 = Mathf.Max(r1.xMin, r2.xMin);
			float y1 = Mathf.Min(r1.yMax, r2.yMax);
			float y2 = Mathf.Max(r1.yMin, r2.yMin);
			area.x = Mathf.Min(x1, x2);
			area.y = Mathf.Min(y1, y2);
			area.width = Mathf.Max(0.0f, x1 - x2);
			area.height = Mathf.Max(0.0f, y1 - y2);

			return true;
		}

		return false;
	}

	public static bool Contains(BoundsInt bounds, Vector3Int position)
	{
		if (position.x >= bounds.xMin && position.y >= bounds.yMin &&
		    position.x <= bounds.xMax && position.y <= bounds.yMax)
		{
			return true;
		}
		return false;
	}

	public static bool Contains(Bounds bounds, Vector3Int position)
	{
		if (position.x >= bounds.min.x && position.y >= bounds.min.y &&
		    position.x <= bounds.max.x && position.y <= bounds.max.y)
		{
			return true;
		}
		return false;
	}

}