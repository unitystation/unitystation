using UnityEngine;

/// <summary>
/// Matrix Move Client
/// </summary>
public partial class MatrixMove
{
	//client-only values
	public MatrixState ClientState => clientState;
	private bool IsMovingClient => clientState.IsMoving && clientState.Speed > 0f;
	/// <summary>
	/// Does current transform rotation not yet match the client matrix state rotation, and thus this matrix's transform needs to
	/// be rotated to match the target?
	/// </summary>
	private bool NeedsRotationClient =>
		Quaternion.Angle(transform.rotation, InitialFacing.OffsetTo(clientState.FacingDirection).Quaternion) != 0;
	///client's transform, can get dirty/predictive
	private MatrixState clientState = MatrixState.Invalid;
	//tracks status of initializing this matrix move
	private bool clientStarted;
	private bool receivedInitialState;
	private bool pendingInitialRotation;
	/// <summary>
	/// Has this matrix move finished receiving its initial state from the server and rotating into its correct
	/// position?
	/// </summary>
	public bool Initialized => clientStarted && receivedInitialState;

	public override void OnStartClient()
	{
		SyncPivot(pivot, pivot);
		SyncInitialPosition(initialPosition, initialPosition);
		clientStarted = true;
	}

	private void SyncInitialPosition(Vector3 oldPos, Vector3 initialPos)
	{
		initialPosition = initialPos.RoundToInt();
	}

	private void SyncPivot(Vector3 oldPivot, Vector3 pivot)
	{
		this.pivot = pivot.RoundToInt();
	}

	/// Called when MatrixMoveMessage is received
	public void UpdateClientState(MatrixState newState)
	{
		var oldState = clientState;
		clientState = newState;
		Logger.LogTraceFormat("{0} setting client / client target state from message {1}", Category.Matrix, this, newState);


		if (!Equals(oldState.FacingDirection, newState.FacingDirection))
		{
			if (!receivedInitialState && !pendingInitialRotation)
			{
				pendingInitialRotation = true;
			}
			inProgressRotation = oldState.FacingDirection.OffsetTo(newState.FacingDirection);
			Logger.LogTraceFormat("{0} starting rotation progress to {1}", Category.Matrix, this, newState.FacingDirection);
			MatrixMoveEvents.OnRotate.Invoke(new MatrixRotationInfo(this, inProgressRotation.Value, NetworkSide.Client, RotationEvent.Start));
		}

		if (!oldState.IsMoving && newState.IsMoving)
		{
			MatrixMoveEvents.OnStartMovementClient.Invoke();
		}

		if (oldState.IsMoving && !newState.IsMoving)
		{
			MatrixMoveEvents.OnStopMovementClient.Invoke();
		}

		if ((int) oldState.Speed != (int) newState.Speed)
		{
			MatrixMoveEvents.OnSpeedChange.Invoke(oldState.Speed, newState.Speed);
		}

		if (!receivedInitialState && !pendingInitialRotation)
		{
			receivedInitialState = true;
		}
	}

	//For Rcs Movement
	public void ReceivePlayerMoveAction(PlayerAction moveActions)
	{
		if (moveActions.Direction() != Vector2Int.zero)
		{
			RcsMovementMessage.Send(moveActions.Direction(), netId);
		}
	}
}
