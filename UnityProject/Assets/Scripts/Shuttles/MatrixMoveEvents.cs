
using UnityEngine.Events;

/// <summary>
/// Encapsulates the different events that can be subscribed to in MatrixMove
/// </summary>
public class MatrixMoveEvents
{

	public readonly UnityEvent OnStartMovementClient = new UnityEvent();
	public readonly UnityEvent OnStopMovementClient = new UnityEvent();
	public readonly UnityEvent OnFullStopClient = new UnityEvent();
	public readonly UnityEvent OnStartEnginesServer = new UnityEvent();
	public readonly UnityEvent OnStopEnginesServer = new UnityEvent();
	public readonly OrientationEvent OnRotate = new OrientationEvent();
	public readonly DualFloatEvent OnSpeedChange = new DualFloatEvent();
}


public class OrientationEvent : UnityEvent<MatrixRotationInfo>
{
}
public class DualFloatEvent : UnityEvent<float,float> {}