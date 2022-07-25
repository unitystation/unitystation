using System;
using System.Collections.Generic;
using UnityEngine;

namespace TileManagement
{
	public struct BetterBoundsInt : IEquatable<BetterBoundsInt>
	{
		public Vector3Int Minimum;
		public Vector3Int Maximum;

		public Vector3Int Min => Minimum;
		public Vector3Int Max => Maximum;

		public Vector3Int min => Minimum;
		public Vector3Int max => Maximum;

		public int yMax => Maximum.y;
		public int yMin => Minimum.y;
		public int xMax => Maximum.x;
		public int xMin => Minimum.x;

		public Vector3Int size => Maximum - Minimum;

		public Vector3 center => (Minimum + Maximum).To3() / 2f;

		public bool Contains(Vector3Int Point)
		{
			if (Point.x >= Minimum.x && Point.x <= Maximum.x)
			{
				if (Point.y >= Minimum.y && Point.y <= Maximum.y)
				{
					return true;
				}
			}

			return false;
		}


		public void ExpandToPoint2D(Vector3Int Point)
		{
			Minimum = Vector3Int.Min(Minimum, Point);
			Maximum = Vector3Int.Max(Maximum, Point);
		}

		public List<Vector3Int> allPositionsWithin()
		{
			var stop = Mathf.RoundToInt(Maximum.x);
			var stop2 = Mathf.RoundToInt(Maximum.y);

			List<Vector3Int> returning = new List<Vector3Int>(stop * stop2);

			for (int x = Mathf.RoundToInt(Minimum.x); x <= stop; x++)
			{
				for (int y = Mathf.RoundToInt(Minimum.y); y <= stop2; y++)
				{
					returning.Add(new Vector3Int(x, y, 0));
				}
			}

			return returning;
		}

		public bool Equals(BetterBoundsInt other)
		{
			return Maximum == other.Maximum && Minimum == other.Minimum;
		}
	}
}