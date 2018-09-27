using UnityEngine;
using UnityEngine.Events;

/// Should be removed when common ancestor for PlayerSync and CustomNetTransform will be created
public interface IPushable {
	bool Push( Vector2Int direction );
	bool PredictivePush( Vector2Int direction );
	Vector3IntEvent OnUpdateRecieved();
	Vector3IntEvent OnTileReached();
	Vector3IntEvent OnClientTileReached();
//	Transform ServerTransform();
}

public class Vector3IntEvent : UnityEvent<Vector3Int> {}