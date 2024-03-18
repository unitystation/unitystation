using System;
using System.Collections.Generic;
using Mirror.Discovery;
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


		public Vector3 GetClosestPerimeterPoint(Vector3 Point)
		{
			float? SmallestDistance = Mathf.Abs(Point.x - Minimum.x);
			Vector3 EntryPoint = Vector3.zero;

			if (SmallestDistance > Mathf.Abs(Point.x - Minimum.x))
			{
				SmallestDistance = Mathf.Abs(Point.x - Minimum.x);
				EntryPoint = new Vector3(Minimum.x, Point.y);
			}


			if (SmallestDistance > Mathf.Abs(Point.y - Minimum.y))
			{
				SmallestDistance = Mathf.Abs(Point.y - Minimum.y);
				EntryPoint = new Vector3(Point.x,Minimum.y );
			}

			if (SmallestDistance > Mathf.Abs(Point.x - Maximum.x))
			{
				SmallestDistance = Mathf.Abs(Point.x - Maximum.x);
				EntryPoint = new Vector3(Maximum.x, Point.y);
			}

			if (SmallestDistance > Mathf.Abs(Point.y - Maximum.y))
			{
				SmallestDistance = Mathf.Abs(Point.y - Maximum.y);
				EntryPoint = new Vector3(Point.x,Maximum.y );
			}

			return EntryPoint;
		}

		public Vector3 GetCorner(int i)
		{
			if (i == 0)
			{
				return Minimum;
			}
			else if (i == 1)
			{
				return new Vector3(Minimum.x,Maximum.y,0);
			}
			else if (i == 2)
			{
				return Maximum;
			}
			else if (i == 3)
			{
				return new Vector3( Maximum.x,Minimum.y,0);
			}

			return Maximum;
		}

		public IEnumerable<Vector3> Corners()
		{
			int max = 4;
			for (int i = 0; i < max; i++)
			{
				if (i == 0)
				{
					yield return GetCorner(i);
				}
				else if (i == 1)
				{
					yield return GetCorner(i);
				}
				else if (i == 2)
				{
					yield return GetCorner(i);
				}
				else if (i == 3)
				{
					yield return GetCorner(i);
				}
			}
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


		public BetterBounds ExpandAllDirectionsBy(float AddAmount)
		{
			var CustomMinimum = Minimum;
			CustomMinimum -= new Vector3(AddAmount, AddAmount, 0);

			var CustomMaximum = Maximum;
			CustomMaximum += new Vector3(AddAmount, AddAmount, 0);

			return new BetterBounds()
			{
				Minimum = CustomMinimum,
				Maximum = CustomMaximum,
			};
		}
	}
}