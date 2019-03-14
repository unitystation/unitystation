using UnityEngine;

/// <summary>
/// Describes info required for a nudge
/// </summary>
public struct NudgeInfo
{
	/// Null object, means that there's no nudge in progress
	public static readonly NudgeInfo NoNudge =
		new NudgeInfo{ OriginPos = TransformState.HiddenPos, TargetPos = TransformState.HiddenPos };
	public Vector3 OriginPos;
	public Vector3 TargetPos;
	public float SpinMultiplier;
	public float InitialSpeed;
	public SpinMode SpinMode;
	public Vector3 Trajectory => TargetPos - OriginPos;

	public override string ToString() {
		return Equals(NoNudge) ? "[No nudge]" :
			$"[{nameof( OriginPos )}: {OriginPos}, {nameof( TargetPos )}: {TargetPos}, " +
			$"{nameof( SpinMultiplier )}: {SpinMultiplier}, {nameof( InitialSpeed )}: {InitialSpeed}, {nameof( SpinMode )}: {SpinMode}]";
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