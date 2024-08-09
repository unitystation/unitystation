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

		public readonly bool Contains(Vector3 Point)
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



		public BetterBounds(Vector3 pointA, Vector3 pointB)
		{
			Minimum = new Vector3(Mathf.Min(pointA.x, pointB.x), Mathf.Min(pointA.y, pointB.y));
			Maximum = new Vector3(Mathf.Max(pointA.x, pointB.x), Mathf.Max(pointA.y, pointB.y));
		}


		public readonly bool LineIntersectsRect(Vector2 p1, Vector2 p2)
		{
			var Tmin = min;
			var Tmax = max;

			var Xmin_ymax = new Vector2(xMin, yMax);
			var xMax_yMin = new Vector2(xMax, yMin);

			return LineIntersectsLine(p1, p2, Tmin, Xmin_ymax) ||
			       LineIntersectsLine(p1, p2, Tmin, xMax_yMin) ||
			       LineIntersectsLine(p1, p2, Tmax, Xmin_ymax) ||
			       LineIntersectsLine(p1, p2, Tmax, xMax_yMin) ||
			       (FindPoint( p1) && FindPoint(p2));
		}

		private readonly bool FindPoint(Vector2 Point)
		{
			if (Point.x > min.x && Point.x < max.x &&
			    Point.y > min.y && Point.y < max.y)
				return true;

			return false;
		}


		private static bool LineIntersectsLine(Vector2 l1p1, Vector2 l1p2, Vector2 l2p1, Vector2 l2p2)
		{
			float q = (l1p1.y - l2p1.y) * (l2p2.x - l2p1.x) - (l1p1.x - l2p1.x) * (l2p2.y - l2p1.y);
			float d = (l1p2.x - l1p1.x) * (l2p2.y - l2p1.y) - (l1p2.y - l1p1.y) * (l2p2.x - l2p1.x);

			if (d == 0)
			{
				return false;
			}

			float r = q / d;

			q = (l1p1.y - l2p1.y) * (l1p2.x - l1p1.x) - (l1p1.x - l2p1.x) * (l1p2.y - l1p1.y);
			float s = q / d;

			if (r < 0 || r > 1 || s < 0 || s > 1)
			{
				return false;
			}

			return true;
		}


		public Vector3 GetClosestPerimeterPoint(Vector3 Point)
		{
			float SmallestDistance = 99999999999;
			Vector3 EntryPoint = Minimum;


			if (Contains(Point))
			{

				var Vector = new Vector3(Minimum.x, Point.y);
				var distance = (Vector - Point).magnitude;
				if (SmallestDistance > distance)
				{
					SmallestDistance = distance;
					EntryPoint = Vector;
				}

				Vector = new Vector3(Point.x, Minimum.y);
				distance = (Vector - Point).magnitude;
				if (SmallestDistance > distance)
				{
					SmallestDistance = distance;
					EntryPoint = Vector;
				}

				Vector = new Vector3(Maximum.x, Point.y);
				distance = (new Vector3(Maximum.x, Point.y) - Point).magnitude;
				if (SmallestDistance > distance)
				{
					SmallestDistance = distance;
					EntryPoint = Vector;
				}

				Vector = new Vector3(Point.x, Maximum.y);
				distance = (new Vector3(Point.x, Maximum.y) - Point).magnitude;
				if (SmallestDistance > distance)
				{
					SmallestDistance = distance;
					EntryPoint = Vector;
				}

				return EntryPoint;
			}
			else
			{

				var Vector = new Vector3(
					Minimum.x,
					Mathf.Min(Mathf.Max(Point.y, Minimum.y), Maximum.y));
				var distance = (Vector - Point).magnitude;

				if (SmallestDistance > distance)
				{
					SmallestDistance = distance;
					EntryPoint = Vector;
				}


				Vector = new Vector3(
					Mathf.Min(Mathf.Max(Point.x, Minimum.x), Maximum.x),
					Minimum.y);
				distance = (Vector - Point).magnitude;
				if (SmallestDistance > distance)
				{
					SmallestDistance = distance;
					EntryPoint = Vector;
				}


				Vector = new Vector3(
					Maximum.x,
					Mathf.Min(Mathf.Max(Point.y, Minimum.y), Maximum.y));
				distance = (Vector - Point).magnitude;
				if (SmallestDistance > distance)
				{
					SmallestDistance = distance;
					EntryPoint = Vector;
				}


				Vector = new Vector3(
					Mathf.Min(Mathf.Max(Point.x, Minimum.x),  Maximum.x),
					Maximum.y);
				distance = (new Vector3(Maximum.x, Point.y) - Point).magnitude;
				if (SmallestDistance > distance)
				{
					SmallestDistance = distance;
					EntryPoint = Vector;
				}

				return EntryPoint;
			}

		}

		public readonly BetterBounds ConvertToLocal(MatrixInfo Matrix)
		{
			return new BetterBounds(Minimum.ToLocal(Matrix), Maximum.ToLocal(Matrix));
		}

		public readonly BetterBounds ConvertToWorld(MatrixInfo Matrix)
		{
			return new BetterBounds(Minimum.ToWorld(Matrix), Maximum.ToWorld(Matrix));
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