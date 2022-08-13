using System;
using UnityEngine;

/// <summary>
/// Encapsulates the state of a matrix's motion / facing
/// </summary>
public struct MatrixState
{
	[NonSerialized] public bool Inform;
	public bool IsMoving;
	public float Speed;
	public int RotationTime; //in frames?

	public Vector3 Position;

	/// <summary>
	/// Direction we are facing. Not always the same as flying direction, as some shuttles
	/// can back up.
	/// </summary>
	public Orientation FacingDirection;

	/// <summary>
	/// Current flying direction. Note this may not always match the rotation of the ship, as shuttles
	/// can back up.
	/// </summary>
	public Orientation FlyingDirection;

	/// <summary>
	/// Gets the rotation offset this state represents from the matrix move's initial mapped
	/// facing.
	/// </summary>
	/// <param name="matrixMove"></param>
	public RotationOffset FacingOffsetFromInitial(MatrixMove matrixMove)
	{
		return matrixMove.InitialFacing.OffsetTo(FacingDirection);
	}

	public static readonly MatrixState Invalid = new MatrixState {Position = TransformState.HiddenPos};

	public override bool Equals(object obj)
	{
		return obj is MatrixState other && Equals(other);
	}

	public bool Equals(MatrixState other)
	{
		return Position.Equals(other.Position) && FacingDirection.Equals(other.FacingDirection) && FlyingDirection.Equals(other.FlyingDirection);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = Position.GetHashCode();
			hashCode = (hashCode * 397) ^ FacingDirection.GetHashCode();
			hashCode = (hashCode * 397) ^ FlyingDirection.GetHashCode();
			return hashCode;
		}
	}


	public override string ToString()
	{
		return $"{nameof(Inform)}: {Inform}, {nameof(IsMoving)}: {IsMoving}, {nameof(Speed)}: {Speed}, " +
		       $"{nameof(RotationTime)}: {RotationTime}, {nameof(Position)}: {Position}, {nameof(FacingDirection)}: " +
		       $"{FacingDirection}, {nameof(FlyingDirection)}: {FlyingDirection}";
	}
}