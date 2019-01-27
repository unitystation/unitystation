using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents an absolute facing. Can only be in 4 cardinal directions.
///
/// Defined in terms of euler angle (rotation about the z axis, right is 0 and up is 90).
/// </summary>
public struct Orientation
{
	public static readonly Orientation Right = new Orientation(0);
	public static readonly Orientation Up = new Orientation(90);
	public static readonly Orientation Left = new Orientation(180);
	public static readonly Orientation Down = new Orientation(270);

	private static readonly List<Orientation> clockwiseOrientation = new List<Orientation> {Right, Down, Left, Up};

	/// <summary>
	/// Euler angle (rotation about the z axis, right is 0 and up is 90).
	/// </summary>
	public readonly int Degrees;

	private Orientation(int degree)
	{
		Degrees = degree;
	}

	/// <summary>
	/// Vector pointing in the same direction as the orientation.
	/// </summary>
	public Vector3 Vector => (Quaternion.Euler(0,0, Degrees) * Vector3Int.right).RoundToInt();

	/// <summary>
	/// Return the orientation that would be reached by rotating clockwise 90 degrees the given number of turns
	/// </summary>
	/// <param name="steps">number of times to rotate 90 degrees, negative for counter-clockwise</param>
	/// <returns>the orientation that would be reached by rotating 90 degrees the given number of turns</returns>
	public Orientation Rotate(int turns)
	{
		int index = clockwiseOrientation.IndexOf( this );
		int newIndex = ((index + turns) % clockwiseOrientation.Count + clockwiseOrientation.Count) % clockwiseOrientation.Count;
		return clockwiseOrientation[newIndex];
	}

	/// <summary>
	/// Return the rotation that would be reached by rotating according to the specified offset.
	///
	/// For example, if Orientation is Right and offset is Backwards, will return Orientation.Left
	/// </summary>
	/// <param name="offset">offset to rotate by</param>
	/// <returns>the rotation that would be reached by rotating according to the specified offset.</returns>
	public Orientation Rotate(RotationOffset offset)
	{
		return Rotate(-offset.Degree / 90);
	}


	public override string ToString()
	{
		if (this == Left)
		{
			return "Left";
		}
		else if (this == Right)
		{
			return "Right";
		}
		else if (this == Up)
		{
			return "Up";
		}
		else
		{
			return "Down";
		}
	}

	/// <summary>
	/// Gets the Rotationoffset that would offset this orientation to reach toOrientation.
	///
	/// For example if this is Up and toOrientation is Down, returns RotationOffset.Backwards
	/// </summary>
	/// <param name="toOrientation">orientation to which the offset should be determined</param>
	/// <returns>the rotationoffset</returns>
	public RotationOffset OffsetTo(Orientation toOrientation)
	{
		if (this == toOrientation)
		{
			return RotationOffset.Same;
		}
		if (this == toOrientation.Rotate(1))
		{
			return RotationOffset.Right;
		}
		else if (this == toOrientation.Rotate(2))
		{
			return RotationOffset.Backwards;
		}
		else
		{
			return RotationOffset.Left;
		}
	}

	private static float AngleFromUp(Vector2 dir)
	{
		float angle = Vector2.Angle(Vector2.up, dir);

		if (dir.x < 0)
		{
			angle = 360 - angle;
		}

		return angle;
	}

	/// <summary>
	/// Orientation pointing the same direction as the specified vector.
	/// For example if vector is right (1,0), this will return Orientation.Right
	/// </summary>
	/// <param name="direction">direction</param>
	/// <returns>orientation pointing in same direction as vector</returns>
	public static Orientation From( Vector2 direction )
	{
		float degree = AngleFromUp(direction);
		var orientation = Down;
		if ( degree >= 315f && degree <= 360f || degree >= 0f && degree <= 45f ) {
			orientation = Up;
		}
		if ( degree > 45f && degree <= 135f ) {
			orientation = Right;
		}
		if ( degree > 135f && degree <= 225f ) {
			orientation = Down;
		}
		if ( degree > 225f && degree < 315f ) {
			orientation = Left;
		}
		return orientation;
	}

	/// <summary>
	/// Gets an int representing how many 90 degree rotations would be needed to get from
	/// this orientation to target taking the shortest rotation path possible. Negative return indicates counter clockwise
	/// </summary>
	/// <param name="target">target orientation</param>
	/// <returns>number of 90 degree clockwise rotations to reach target, negative indicating counter clockwise</returns>
	public int RotationsTo(Orientation target)
	{
		if (this == target)
		{
			return 0;
		}
		if (this == target.Rotate(1))
		{
			return 1;
		}
		else if (this == target.Rotate(2))
		{
			return 2;
		}
		else
		{
			return -1;
		}
	}

	public static bool operator ==(Orientation obj1, Orientation obj2)
	{
		return obj1.Equals(obj2);
	}

	public static bool operator !=(Orientation obj1, Orientation obj2)
	{
		return !obj1.Equals(obj2);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj))
			return false;
		return obj is Orientation && Equals((Orientation) obj);
	}

	public bool Equals(Orientation other)
	{
		return Degrees == other.Degrees;
	}

	public override int GetHashCode()
	{
		return Mathf.RoundToInt(Degrees);
	}
}
