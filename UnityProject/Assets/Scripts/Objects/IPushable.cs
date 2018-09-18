using UnityEngine;
using UnityEngine.Events;

/// Should be removed when common ancestor for PlayerSync and CustomNetTransform will be created
public interface IPushable {
	void Push( Vector2Int direction );
	void PredictivePush( Vector2Int direction );
	UnityEvent OnUpdateRecieved();
	Vector3IntEvent OnTileReached();
//	Transform ServerTransform();
}

public class Vector3IntEvent : UnityEvent<Vector3Int> {
}