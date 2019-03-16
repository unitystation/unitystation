using UnityEngine;

/// <summary>
/// Describes info required for a throw
/// </summary>
public struct ThrowInfo
{
	/// Null object, means that there's no throw in progress
	public static readonly ThrowInfo NoThrow =
		new ThrowInfo{ OriginPos = TransformState.HiddenPos, TargetPos = TransformState.HiddenPos };
	public Vector3 OriginPos;
	public Vector3 TargetPos;
	public GameObject ThrownBy;
	public BodyPartType Aim;
	public float InitialSpeed;
	public SpinMode SpinMode;
	public Vector3 Trajectory => TargetPos - OriginPos;

	public override string ToString() {
		return Equals(NoThrow) ? "[No throw]" :
			$"[{nameof( OriginPos )}: {OriginPos}, {nameof( TargetPos )}: {TargetPos}, {nameof( ThrownBy )}: {ThrownBy}, " +
			$"{nameof( Aim )}: {Aim}, {nameof( InitialSpeed )}: {InitialSpeed}, {nameof( SpinMode )}: {SpinMode}]";
	}

	public bool Equals( ThrowInfo other ) {
		return OriginPos.Equals( other.OriginPos ) && TargetPos.Equals( other.TargetPos );
	}

	public override bool Equals( object obj ) {
		if ( ReferenceEquals( null, obj ) ) {
			return false;
		}

		return obj is ThrowInfo && Equals( ( ThrowInfo ) obj );
	}

	public override int GetHashCode() {
		unchecked {
			return ( OriginPos.GetHashCode() * 397 ) ^ TargetPos.GetHashCode();
		}
	}
}