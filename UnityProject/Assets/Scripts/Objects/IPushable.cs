using System;
using UnityEngine;
using UnityEngine.Events;

/// Should be removed when common ancestor for PlayerSync and CustomNetTransform will be created
public interface IPushable {
	/// Push this thing in following direction
	/// <param name="followMode">flag used when object is following its puller
	/// (turns on tile snapping and removes player collision check)</param>
	/// <returns>true if push was successful</returns>
	bool Push( Vector2Int direction, float speed = Single.NaN, bool followMode = false );
	bool PredictivePush( Vector2Int direction );
	/// Rollback predictive push using last good position
	void NotifyPlayers();
	Vector3IntEvent OnUpdateRecieved();
	DualVector3IntEvent OnStartMove();
	Vector3IntEvent OnTileReached();
	Vector3IntEvent OnClientTileReached();
	/// When you need to break pulling of this object
	UnityEvent OnPullInterrupt();
	bool CanPredictPush { get; }
	float MoveSpeedServer { get; }
	/// Try stopping object if it's flying
	void Stop();
}

public class Vector3IntEvent : UnityEvent<Vector3Int> {}
public class DualVector3IntEvent : UnityEvent<Vector3Int,Vector3Int> {}