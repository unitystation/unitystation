using System;
using System.Collections.Generic;
using UnityEngine;

namespace TileManagement
{
	public struct BetterBounds : IEquatable<BetterBounds>
	{
		public Vector3 Minimum;
		public Vector3 Maximum;

		public Vector3 Min => Minimum;
		public Vector3 Max => Maximum;

		public Vector3 min => Minimum;
		public Vector3 max => Maximum;

		public float yMax => Maximum.y;
		public float yMin => Minimum.y;
		public float xMax => Maximum.x;
		public float xMin => Minimum.x;

		public Vector3 size => Maximum - Minimum;

		public Vector3 center => (Minimum + Maximum) / 2;

		public bool Contains(Vector3 Point)
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


		public void ExpandToPoint2D(Vector3 Point)
		{
			Minimum = Vector3.Min(Minimum, Point);
			Maximum = Vector3.Max(Maximum, Point);
		}

		public List<Vector3Int> allPositionsWithin()
		{
			List<Vector3Int> Returning = new List<Vector3Int>();

			var stop = Mathf.RoundToInt(Maximum.x);
			var stop2 = Mathf.RoundToInt(Maximum.y);

			for (int x = Mathf.RoundToInt(Minimum.x); x <= stop; x++)
			{
				for (int y = Mathf.RoundToInt(Minimum.y); y <= stop2; y++)
				{
					Returning.Add(new Vector3Int(x, y, 0));
				}
			}

			return Returning;
		}


		public bool Intersects(BetterBounds OppositeMatrix, out BetterBounds Overlap)
		{
			var bottomLeft = OppositeMatrix.min;
			var bottomRight = new Vector3(OppositeMatrix.xMax, OppositeMatrix.yMin, 0);
			var topLeft = new Vector3(OppositeMatrix.xMin, OppositeMatrix.yMax, 0);
			var topRight = OppositeMatrix.max;


			var ThisbottomLeft = min;
			var ThisbottomRight = new Vector3(xMax, yMin, 0);
			var ThistopLeft = new Vector3(xMin, yMax, 0);
			var ThistopRight = max;

			Overlap = new BetterBounds();


			if (Contains(bottomLeft) || Contains(bottomRight) || Contains(topLeft) || Contains(topRight)
			    || OppositeMatrix.Contains(ThisbottomLeft) || OppositeMatrix.Contains(ThisbottomRight) ||
			    OppositeMatrix.Contains(ThistopLeft) || OppositeMatrix.Contains(ThistopRight))
			{
				Overlap.Minimum = Vector3.Max(Minimum, OppositeMatrix.Minimum);
				Overlap.Maximum = Vector3.Min(Maximum, OppositeMatrix.Maximum);
				return true;
			}


			return false;
		}

		public bool Equals(BetterBounds other)
		{
			return Maximum == other.Maximum && Minimum == other.Minimum;
		}
	}
}