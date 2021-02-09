using System;
using UnityEngine;

/// <summary>
/// Holds player state information used for interpolation, such as player position, flight direction etc.
/// Gives client enough information to be able to smoothly interpolate the player position.
/// </summary>
public struct PlayerState : IEquatable<PlayerState>
{
	public bool Active => Position != TransformState.HiddenPos;

	///Don't set directly, use Speed instead.
	///public in order to be serialized :\
	public float speed;

	public float Speed
	{
		get => speed;
		set => speed = value < 0 ? 0 : value;
	}

	public int MoveNumber;
	public Vector3 Position;

	[NonSerialized] public Vector3 LastNonHiddenPosition;

	public Vector3 WorldPosition
	{
		get
		{
			if (!Active)
			{
				return TransformState.HiddenPos;
			}

			return MatrixManager.LocalToWorld(Position, MatrixManager.Get(MatrixId));
		}
		set
		{
			if (value == TransformState.HiddenPos)
			{
				Position = TransformState.HiddenPos;
			}
			else
			{
				Position = MatrixManager.WorldToLocal(value, MatrixManager.Get(MatrixId));
				LastNonHiddenPosition = value;
			}
		}
	}

	/// Flag means that this update is a pull follow update,
	/// So that puller could ignore them
	public bool IsFollowUpdate;

	public bool NoLerp;

	///Direction of flying in world position coordinates
	public Vector2 WorldImpulse;

	/// <summary>
	/// Direction of flying in local position coordinates
	/// </summary>
	/// <param name="forPlayer">player for which the local impulse should be calculated</param>
	public Vector2 LocalImpulse(PlayerSync forPlayer)
	{
		if (forPlayer.transform.parent == null) return Vector2.zero;
		return Quaternion.Inverse(forPlayer.transform.parent.rotation) * WorldImpulse;
	}

	///Flag for clients to reset their queue when received
	public bool ResetClientQueue;

	/// Flag for server to ensure that clients receive that flight update:
	/// Only important flight updates (ones with impulse) are being sent out by server (usually start only)
	[NonSerialized] public bool ImportantFlightUpdate;

	public int MatrixId;

	/// Means that this player is hidden
	public static readonly PlayerState HiddenState =
		new PlayerState {Position = TransformState.HiddenPos, MatrixId = 0};

	public override string ToString()
	{
		return
			Equals(HiddenState)
				? "[Hidden]"
				: $"[Move #{MoveNumber}, localPos:{(Vector2) Position}, worldPos:{(Vector2) WorldPosition} {nameof(NoLerp)}:{NoLerp}, {nameof(WorldImpulse)}:{WorldImpulse}, " +
				  $"reset: {ResetClientQueue}, flight: {ImportantFlightUpdate}, follow: {IsFollowUpdate}, matrix #{MatrixId}]";
	}

	public bool Equals(PlayerState other)
	{
		return speed.Equals(other.speed) && MoveNumber == other.MoveNumber && Position.Equals(other.Position)
		       && LastNonHiddenPosition.Equals(other.LastNonHiddenPosition) && IsFollowUpdate == other.IsFollowUpdate
		       && NoLerp == other.NoLerp && WorldImpulse.Equals(other.WorldImpulse) && ResetClientQueue == other.ResetClientQueue
		       && ImportantFlightUpdate == other.ImportantFlightUpdate && MatrixId == other.MatrixId;
	}

	public override bool Equals(object obj)
	{
		return obj is PlayerState other && Equals(other);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = speed.GetHashCode();
			hashCode = (hashCode * 397) ^ MoveNumber;
			hashCode = (hashCode * 397) ^ Position.GetHashCode();
			hashCode = (hashCode * 397) ^ LastNonHiddenPosition.GetHashCode();
			hashCode = (hashCode * 397) ^ IsFollowUpdate.GetHashCode();
			hashCode = (hashCode * 397) ^ NoLerp.GetHashCode();
			hashCode = (hashCode * 397) ^ WorldImpulse.GetHashCode();
			hashCode = (hashCode * 397) ^ ResetClientQueue.GetHashCode();
			hashCode = (hashCode * 397) ^ ImportantFlightUpdate.GetHashCode();
			hashCode = (hashCode * 397) ^ MatrixId;
			return hashCode;
		}
	}
}