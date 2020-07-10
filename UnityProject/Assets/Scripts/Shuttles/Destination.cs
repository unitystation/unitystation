using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Struct for shuttle parking info - where and with what orientation should it park
/// </summary>
public struct Destination
{
	public Vector3 Position;
	public Orientation Orientation;
	//public bool ApproachReversed;

	/// <summary>
	/// Null object
	/// </summary>
	public static Destination Invalid = new Destination
	{
		Position = TransformState.HiddenPos,
		Orientation = Orientation.Up
	};

	#region generated shit

	private sealed class PositionOrientationEqualityComparer : IEqualityComparer<Destination>
	{
		public bool Equals( Destination x, Destination y )
		{
			return x.Position.Equals( y.Position ) && x.Orientation.Equals( y.Orientation );
		}

		public int GetHashCode( Destination obj )
		{
			unchecked
			{
				return ( obj.Position.GetHashCode() * 397 ) ^ obj.Orientation.GetHashCode();
			}
		}
	}

	public static IEqualityComparer<Destination> PositionOrientationComparer { get; } = new PositionOrientationEqualityComparer();

	public bool Equals( Destination other )
	{
		return Position.Equals( other.Position ) && Orientation.Equals( other.Orientation );
	}

	public override bool Equals( object obj )
	{
		return obj is Destination other && Equals( other );
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return ( Position.GetHashCode() * 397 ) ^ Orientation.GetHashCode();
		}
	}

	public static bool operator ==( Destination left, Destination right )
	{
		return left.Equals( right );
	}

	public static bool operator !=( Destination left, Destination right )
	{
		return !left.Equals( right );
	}

	#endregion
}