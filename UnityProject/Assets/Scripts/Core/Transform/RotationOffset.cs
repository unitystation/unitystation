using System.Collections.Generic;
using Core;
using UnityEngine;

/// <summary>
/// Represents an offset from an object's initial orientation. Does not represent the ACTUAL facing of an
/// object. RotationOffset is represented as a number of degrees clockwise from the initial orientation.
/// RotationOffset can only ever be in 90 degree increments, there is no diagonal / in-between.
/// </summary>
public struct RotationOffset
{
	/// <summary>
	/// Same as initial orientation
	/// </summary>
	public static readonly RotationOffset Same = new RotationOffset(0);

	/// <summary>
	/// 90 degrees right from initial orientation
	/// </summary>
	public static readonly RotationOffset Right = new RotationOffset(90);

	/// <summary>
	/// facing opposite of initial orientation
	/// </summary>
	public static readonly RotationOffset Backwards = new RotationOffset(180);

	/// <summary>
	/// 90 degrees left from initial orientation
	/// </summary>
	public static readonly RotationOffset Left = new RotationOffset(270);

	private static readonly List<RotationOffset> sequence = new List<RotationOffset> {Same, Left, Backwards, Right};

	/// <summary>
	/// Degrees of clockwise rotation from initial orientation, will be between 0 and 360
	/// </summary>
	public readonly int Degree;

	/// <summary>
	/// Quaternion whose amount of rotation matches the current rotation (clockwise around the z axis)
	/// </summary>
	public Quaternion Quaternion => Quaternion.Euler(0, 0, -DegreeBetween(Same, this));

	/// <summary>
	/// Quaternion whose amount of rotation matches the current rotation (counterclockwise around the z axis)
	/// </summary>
	public Quaternion QuaternionInverted => Quaternion.Euler(0, 0, -DegreeBetween(this, Same));

	/// <summary>
	/// Returns the degree of rotation between 2 rotation offsets. Order matters.
	/// </summary>
	/// <param name="from">offset from</param>
	/// <param name="to">offset to</param>
	/// <returns>positive difference between offsets if from has a greater clockwise degree than
	/// to, otherwise negative. Value will always be between -360 and 360</returns>
	public static int DegreeBetween(RotationOffset from, RotationOffset to)
	{
		int beforeDegree = from.Degree;
		int afterDegree = to.Degree;
		if (from.Degree == 0 && to.Degree == 270)
		{
			beforeDegree = 360;
		}

		if (from.Degree == 270 && to.Degree == 0)
		{
			afterDegree = 360;
		}

		return afterDegree - beforeDegree;
	}

	private RotationOffset(int degree)
	{
		this.Degree = degree;
	}

	///Calculate the mouse click angle in relation to player(for facingDirection on PlayerSprites)
	private static float Angle(Vector2 dir)
	{
		float angle = Vector2.Angle(Vector2.up, dir);

		if (dir.x < 0)
		{
			angle = 360 - angle;
		}

		return angle;
	}

	/// <summary>
	/// Get an orientation based on the specified number of degrees, rounding to the nearest 90 degrees.
	/// </summary>
	/// <param name="degree">degree of clockwise rotation</param>
	/// <returns>one of Backwards, Same, Right, or Left, whichever is closest to the specified
	/// degree</returns>
	public static RotationOffset From(float degree)
	{
		var orientation = Backwards;
		if (degree >= 315f && degree <= 360f || degree >= 0f && degree <= 45f)
		{
			orientation = Same;
		}

		if (degree > 45f && degree <= 135f)
		{
			orientation = Right;
		}

		if (degree > 135f && degree <= 225f)
		{
			orientation = Backwards;
		}

		if (degree > 225f && degree < 315f)
		{
			orientation = Left;
		}

		return orientation;
	}

	/// <summary>
	/// RotationOffset matching the difference between vector and (0,1), i.e.
	/// a rotationoffset which describes how much vector is rotated away from straight up.
	/// Rounded to the nearest 90 degrees.
	/// For example if vector is right (1,0), this will return rotationoffset.Right
	/// </summary>
	/// <param name="vector">vector</param>
	/// <returns>vector's rotation offset from straight up</returns>
	public static RotationOffset From(Vector2 vector)
	{
		return From(Angle(vector));
	}

	/// <summary>
	/// RotationOffset matching the difference between vector and (0,1), i.e.
	/// a rotationoffset which describes how much vector is rotated away from straight up.
	/// Rounded to the nearest 90 degrees.
	/// For example if vector is right (1,0), this will return rotationoffset.Right
	/// </summary>
	/// <param name="vector">vector</param>
	/// <returns>vector's rotation offset from straight up</returns>
	public static RotationOffset From(Vector3 vector)
	{
		return From((Vector2) vector);
	}


	/// <summary>
	/// Gets a rotation offset matching the quaternion's clockwise rotation about the z axis.
	/// </summary>
	/// <param name="quaternion"></param>
	/// <returns></returns>
	public static RotationOffset From(Quaternion quaternion)
	{
		var angle = NormalizeAngle(quaternion.eulerAngles.z);
		return From(angle);
	}

	private static float NormalizeAngle (float angle)
	{
		while (angle>360)
			angle -= 360;
		while (angle<0)
			angle += 360;
		return angle;
	}

	/// <summary>
	/// Get the offset which would be 90 degrees clockwise from current rotation offset.
	/// </summary>
	/// <returns>the offset which would be 90 degrees clockwise from current rotation offset.</returns>
	public RotationOffset Counterclockwise()
	{
		int index = sequence.IndexOf(this);
		if (index + 1 >= sequence.Count || index == -1)
		{
			return sequence[0];
		}

		return sequence[index + 1];
	}

	/// <summary>
	/// Get the offset which would be 90 degrees counter-clockwise from current rotation offset.
	/// </summary>
	/// <returns>the offset which would be 90 degrees counter-clockwise from current rotation offset.</returns>
	public RotationOffset Clockwise()
	{
		int index = sequence.IndexOf(this);
		if (index <= 0)
		{
			return sequence[sequence.Count - 1];
		}

		return sequence[index - 1];
	}

	//Overriding == / != operators for Vector-esque ease of use
	public static bool operator ==(RotationOffset obj1, RotationOffset obj2)
	{
		return obj1.Equals(obj2);
	}

	public static bool operator !=(RotationOffset obj1, RotationOffset obj2)
	{
		return !obj1.Equals(obj2);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj))
			return false;
		return obj is RotationOffset && Equals((RotationOffset) obj);
	}

	public override int GetHashCode()
	{
		return Degree;
	}

	public bool Equals(RotationOffset other)
	{
		return Degree == other.Degree;
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
		else if (this == Backwards)
		{
			return "Backwards";
		}
		else
		{
			return "Same";
		}
	}

	/// <summary>
	/// Add additional rotation to this offset
	/// </summary>
	/// <param name="fromCurrent">rotation to add</param>
	/// <returns>a new offset based on the combination of this offset and rotationToAdd.
	/// For example, if we are rotationoffset.left and rotationToAdd is right, will return
	/// rotationoffset.same</returns>
	public RotationOffset Rotate(RotationOffset rotationToAdd)
	{
		//rotate away from up by this offset, then apply rotationToAdd, then measure how far we
		//are now from up
		return Orientation.Up.OffsetTo(Orientation.Up.Rotate(this)
			.Rotate(rotationToAdd));
	}

}