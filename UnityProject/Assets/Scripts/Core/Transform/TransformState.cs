using System;
using UnityEngine;

/// Current state of transform, server modifies these and sends to clients.
/// Clients can modify these as well for prediction
public struct TransformState {
	public bool Active => LocalPosition != HiddenPos;
	///Don't set directly, use Speed instead.
	///public in order to be serialized :\
	public float speed;
	public float Speed {
		get { return speed; }
		set { speed = value < 0 ? 0 : value; }
	}

	///Direction of throw in world space
	public Vector2 WorldImpulse;

	/// Server-only active throw information
	[NonSerialized]
	public ThrowInfo ActiveThrow;

	public int MatrixId;

	/// Local position on current matrix
	public Vector3 LocalPosition;
	/// World position, more expensive to use
	public Vector3 WorldPosition
	{
		get
		{
			if ( !Active )
			{
				return HiddenPos;
			}

			return MatrixManager.LocalToWorld( LocalPosition, MatrixManager.Get( MatrixId ) );
		}
		set {
			if (value == HiddenPos) {
				LocalPosition = HiddenPos;
			}
			else
			{
				LocalPosition = MatrixManager.WorldToLocal( value, MatrixManager.Get( MatrixId ) );
			}
		}
	}

	/// Flag means that this update is a pull follow update,
	/// So that puller could ignore them
	public bool IsFollowUpdate;

	/// <summary>
	/// Degrees of rotation about the Z axis caused by spinning, using unity's convention for euler angles where positive = CCW.
	/// This is only used for spinning objects such as thrown ones or ones drifting in space and as such should not
	/// be used for determining actual facing. Only affects the transform.localRotation of an object (rotation relative
	/// to parent transform).
	/// </summary>
	public float SpinRotation;
	/// Spin direction and speed, if it should spin
	public sbyte SpinFactor;

	/// Means that this object is hidden
	public static readonly Vector3Int HiddenPos = new Vector3Int(0, 0, -100);
	/// Should only be used for uninitialized transform states, should NOT be used for anything else.
	public static readonly TransformState Uninitialized =
		new TransformState{ LocalPosition = HiddenPos, ActiveThrow = ThrowInfo.NoThrow, MatrixId = -1};


	/// <summary>
	/// Check if this represents the uninitialized state TransformState.Uninitialized
	/// </summary>
	/// <returns>true iff this is TransformState.Uninitialized</returns>
	public bool IsUninitialized => MatrixId == -1;

	public override string ToString()
	{
		if (Equals(Uninitialized))
		{
			return "[Uninitialized]";
		}
		else if (LocalPosition == HiddenPos)
		{
			return  $"[{nameof( LocalPosition )}: Hidden, {nameof( WorldPosition )}: Hidden, " +
			        $"{nameof( Speed )}: {Speed}, {nameof( WorldImpulse )}: {WorldImpulse}, {nameof( SpinRotation )}: {SpinRotation}, " +
			        $"{nameof( SpinFactor )}: {SpinFactor}, {nameof( IsFollowUpdate )}: {IsFollowUpdate}, {nameof( MatrixId )}: {MatrixId}]";
		}
		else
		{
			return  $"[{nameof( LocalPosition )}: {(Vector2)LocalPosition}, {nameof( WorldPosition )}: {(Vector2)WorldPosition}, " +
			        $"{nameof( Speed )}: {Speed}, {nameof( WorldImpulse )}: {WorldImpulse}, {nameof( SpinRotation )}: {SpinRotation}, " +
			        $"{nameof( SpinFactor )}: {SpinFactor}, {nameof( IsFollowUpdate )}: {IsFollowUpdate}, {nameof( MatrixId )}: {MatrixId}]";
		}

	}
}