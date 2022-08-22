using UnityEngine;

/// <summary>
/// Describes info required for a throw
/// </summary>
public struct ThrowInfo
{
	/// Null object, means that there's no throw in progress
	public static readonly ThrowInfo NoThrow =
		new ThrowInfo{ OriginWorldPos = TransformState.HiddenPos, WorldTrajectory = TransformState.HiddenPos };
	public Vector3 OriginWorldPos;
	/// <summary>
	/// Trajectory in world space, vector pointing from origin to the targeted position of the throw.
	/// </summary>
	public Vector3 WorldTrajectory;
	public GameObject ThrownBy;
	public BodyPartType Aim;
	public float InitialSpeed;


	public override string ToString() {
		return Equals(NoThrow) ? "[No throw]" :
			$"[{nameof( OriginWorldPos )}: {OriginWorldPos}, {nameof( WorldTrajectory )}: {WorldTrajectory}, {nameof( ThrownBy )}: {ThrownBy}, " +
			$"{nameof( Aim )}: {Aim}, {nameof( InitialSpeed )}: {InitialSpeed}]";
	}

	public bool Equals( ThrowInfo other ) {
		return OriginWorldPos.Equals( other.OriginWorldPos ) && WorldTrajectory.Equals( other.WorldTrajectory );
	}

	public override bool Equals( object obj ) {
		if ( ReferenceEquals( null, obj ) ) {
			return false;
		}

		return obj is ThrowInfo && Equals( ( ThrowInfo ) obj );
	}

	public override int GetHashCode() {
		unchecked {
			return ( OriginWorldPos.GetHashCode() * 397 ) ^ WorldTrajectory.GetHashCode();
		}
	}
}