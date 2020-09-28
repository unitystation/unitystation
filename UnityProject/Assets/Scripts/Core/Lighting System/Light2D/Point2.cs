using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Light2D
{
    /// <summary>
    /// Class is almost same as Vector2, but using int data type instead of float.
    /// </summary>
    [Serializable]
    public struct Point2 : IEquatable<Point2>
    {
        public int x, y;

        public Point2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(Point2 other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Point2 && Equals((Point2)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (x * 397) ^ y;
            }
        }

        public static bool operator ==(Point2 left, Point2 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Point2 left, Point2 right)
        {
            return !left.Equals(right);
        }

        public static implicit operator Vector2(Point2 p)
        {
            return new Vector2(p.x, p.y);
        }

        public static implicit operator Vector3(Point2 p)
        {
            return new Vector2(p.x, p.y);
        }

        public static Point2 Floor(Vector2 v)
        {
            return new Point2((int)v.x, (int)v.y);
        }

        public static Point2 Round(Vector2 v)
        {
            return new Point2(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
        }

        public static Point2 Floor(float x, float y)
        {
            return new Point2((int)x, (int)y);
        }

        public static Point2 Round(float x, float y)
        {
            return new Point2(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
        }

        public static Point2 operator +(Point2 first, Point2 second)
        {
            return new Point2(first.x + second.x, first.y + second.y);
        }

        public static Point2 operator -(Point2 first, Point2 second)
        {
            return new Point2(first.x - second.x, first.y - second.y);
        }

        public static Vector2 operator +(Point2 first, Vector2 second)
        {
            return new Vector2(first.x + second.x, first.y + second.y);
        }

        public static Vector2 operator -(Point2 first, Vector2 second)
        {
            return new Vector2(first.x - second.x, first.y - second.y);
        }

        public static Point2 operator *(Point2 p, int mul)
        {
            return new Point2(p.x * mul, p.y * mul);
        }

        public static Point2 operator /(Point2 p, int div)
        {
            return new Point2(p.x / div, p.y / div);
        }

        public static Vector2 operator *(Point2 p, float mul)
        {
            return new Vector2(p.x * mul, p.y * mul);
        }

        public static Vector2 operator /(Point2 p, float div)
        {
            return new Vector2(p.x / div, p.y / div);
        }

        public static Point2 one
        {
            get { return new Point2(1, 1); }
        }

        public static Point2 zero
        {
            get { return new Point2(0, 0); }
        }

        public override string ToString()
        {
            return "(" + x + ", " + y + ")";
        }
    }
}
