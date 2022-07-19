using UnityEngine;
using UnityEngine.Events;
using Objects;

public class Vector3Event : UnityEvent<Vector3> { }
public class Vector3IntEvent : UnityEvent<Vector3Int> { }
public class DualVector3IntEvent : UnityEvent<Vector3Int, Vector3Int> { }

/// <summary>
/// Collision event that's invoked when a tile snapped object (player/machine) flies into something at high enough speed
/// In order to apply damage to both flying object and whatever there is on next tile
/// </summary>
public class CollisionEvent : UnityEvent<CollisionInfo> { }