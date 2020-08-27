using System;
using UnityEngine;
using UnityEngine.Events;

/// Should be removed when common ancestor for PlayerSync and CustomNetTransform will be created
public interface IPushable {
	/// Push this thing in following direction
	/// <param name="followMode">flag used when object is following its puller
	/// (turns on tile snapping and removes player collision check)</param>
	/// <returns>true if push was successful</returns>
	bool Push( Vector2Int direction, float speed = Single.NaN, bool followMode = false, bool ignorePassable = false);
	bool PredictivePush( Vector2Int target, float speed = Single.NaN, bool followMode = false);

	/// <summary>
	/// Push this in provided direction if it's under no gravity (or on a frictionless floor)
	/// </summary>
	/// <param name="direction"></param>
	/// <param name="speed"></param>
	void NewtonianMove(Vector2Int direction, float speed = Single.NaN);
	/// Notify players about up-to-date state
	void NotifyPlayers();
	//I think this is valid server side only
	bool VisibleState { get; set; }
	Vector3IntEvent OnUpdateRecieved();
	DualVector3IntEvent OnStartMove();
	DualVector3IntEvent OnClientStartMove();
	Vector3IntEvent OnTileReached();
	Vector3IntEvent OnClientTileReached();
	CollisionEvent OnHighSpeedCollision();
	/// When you need to break pulling of this object
	UnityEvent OnPullInterrupt();
	bool CanPredictPush { get; }
	bool IsMovingClient { get; }
	bool IsMovingServer { get; }
	Vector2 ServerImpulse { get; }
	float SpeedServer { get; }
	float SpeedClient { get; }

	bool IsTileSnap { get; }

	void Nudge( NudgeInfo info );
	ItemSize Size { get; }

	/// Try stopping object if it's flying
	void Stop();

	/// ServerState WorldPosition because registerTile doesn't cut it
	Vector3Int ServerPosition { get; }
	Vector3Int ServerLocalPosition { get; }
	Vector3Int ClientPosition { get; }
	Vector3Int ClientLocalPosition { get; }
	Vector3Int TrustedPosition { get; }
	Vector3Int TrustedLocalPosition { get; }
	Vector3Int LastNonHiddenPosition { get; }

	/// Rollback predictive push on client using last good position
	void RollbackPrediction();

	/// <summary>
	/// Invoked on client-side when this object is beginning following / being pulled by another object
	/// </summary>
	void OnClientStartFollowing();

	/// <summary>
	/// Invoked on client-side when this object is stopping following / being pulled by another object
	/// </summary>
	void OnClientStopFollowing();

	void SetVisibleServer(bool visible);
}
public class Vector3Event : UnityEvent<Vector3> { }
public class Vector3IntEvent : UnityEvent<Vector3Int> {}
public class DualVector3IntEvent : UnityEvent<Vector3Int,Vector3Int> {}

/// <summary>
/// Collision event that's invoked when a tile snapped object (player/machine) flies into something at high enough speed
/// In order to apply damage to both flying object and whatever there is on next tile
/// </summary>
public class CollisionEvent : UnityEvent<CollisionInfo> {}
