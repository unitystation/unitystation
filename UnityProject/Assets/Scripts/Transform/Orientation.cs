using System.Collections.Generic;
using UnityEngine;
/// Designed to meet all of your 4-positional needs.
/// Intended to replace Vector2 for showing direction and allows  
public struct Orientation
{
	public static readonly Orientation
		Up = new Orientation( 0 ),
		Right = new Orientation( 90 ),
		Down = new Orientation( 180 ),
		Left = new Orientation( 270 );

	private static readonly List<Orientation> sequence = new List<Orientation> {Up, Left, Down, Right};
	public readonly int Degree;
	public Quaternion Euler => Quaternion.Euler( 0, 0, DegreeBetween( Up, this ) );
	public Quaternion EulerInverted => Quaternion.Euler( 0, 0, DegreeBetween( this, Up ) );

	/// Degree between two Orientations. Order matters
	public static int DegreeBetween( Orientation before, Orientation after ) {
		int beforeDegree = before.Degree;
		int afterDegree = after.Degree;
		if ( before.Degree == 0 && after.Degree == 270 ) {
			beforeDegree = 360;
		}
		if ( before.Degree == 270 && after.Degree == 0 ) {
			afterDegree = 360;
		}
		return afterDegree - beforeDegree;
	}

	private Orientation( int degree ) {
		Degree = degree;
	}

	///Vector2Int representation of current orientation
	public Vector2Int Vector => Vector2Int.RoundToInt( Quaternion.Euler( 0, 0, Degree ) * Vector2.up );

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
	/// Orientation from degree
	public static Orientation From( float degree ) {
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
	/// Orientation from Vector2/Vector2Int
	public static Orientation From( Vector2 vector ) {
		return From( Angle(vector) );
	}
	/// Orientation from Vector3/Vector3Int
	public static Orientation From( Vector3 vector ) {
		return From( (Vector2) vector );
	}
	/// Next orientation in sequence (clockwise)
	public Orientation Next() {
		int index = sequence.IndexOf( this );
		if ( index + 1 >= sequence.Count || index == -1 ) {
			return sequence[0];
		}
		return sequence[index + 1];
	}
	/// Previous orientation in sequence (counter-clockwise)
	public Orientation Previous() {
		int index = sequence.IndexOf( this );
		if ( index <= 0 ) {
			return sequence[sequence.Count - 1];
		}
		return sequence[index - 1];
	}
	//Overriding == / != operators for Vector-esque ease of use
	public static bool operator ==( Orientation obj1, Orientation obj2 ) {
		return obj1.Equals( obj2 );
	}
	public static bool operator !=( Orientation obj1, Orientation obj2 ) {
		return !obj1.Equals( obj2 );
	}
	public override bool Equals( object obj ) {
		if ( ReferenceEquals( null, obj ) )
			return false;
		return obj is Orientation && Equals( (Orientation) obj );
	}
	public override int GetHashCode() {
		return Degree;
	}
	public bool Equals( Orientation other ) {
		return Degree == other.Degree;
	}
	public override string ToString() {
		return $"{Degree}";
	}
}
