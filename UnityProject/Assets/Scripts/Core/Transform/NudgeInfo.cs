using UnityEngine;

/// <summary>
/// Describes info required for a nudge
/// </summary>
public struct NudgeInfo
{
	/// Null object, means that there's no nudge in progress
	public static readonly NudgeInfo NoNudge =
		new NudgeInfo{ OriginPos = TransformState.HiddenPos, Trajectory = TransformState.HiddenPos };
	public Vector3 OriginPos;
//	public Vector3 TargetPos;
	public float SpinMultiplier;
	public float InitialSpeed;
	public Vector3 Trajectory;

	public override string ToString() {
		return Equals(NoNudge) ? "[No nudge]" :
			$"[{nameof( OriginPos )}: {OriginPos}, " +
			$"{nameof( SpinMultiplier )}: {SpinMultiplier}, {nameof( InitialSpeed )}: {InitialSpeed}]";
	}

	public bool Equals( NudgeInfo other ) {
		return OriginPos.Equals( other.OriginPos ) && Trajectory.Equals( other.Trajectory );
	}

	public override bool Equals( object obj ) {
		if ( ReferenceEquals( null, obj ) ) {
			return false;
		}

		return obj is NudgeInfo && Equals( ( NudgeInfo ) obj );
	}

	public override int GetHashCode() {
		unchecked {
			return ( OriginPos.GetHashCode() * 397 ) ^ Trajectory.GetHashCode();
		}
	}
}