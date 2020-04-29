using UnityEngine;

/// <summary>
/// Matrix Move Client
/// </summary>
public partial class MatrixMove
{
	private bool IsMovingClient => ServerState.IsMoving && ServerState.Speed > 0f;
	/// <summary>
	/// Does current transform rotation not yet match the client matrix state rotation, and thus this matrix's transform needs to
	/// be rotated to match the target?
	/// </summary>
	private bool NeedsRotationClient =>
		Quaternion.Angle(transform.rotation, InitialFacing.OffsetTo(ServerState.FacingDirection).Quaternion) != 0;

	private bool IsRotatingClient;

	//tracks status of initializing this matrix move
	private bool clientStarted;
	private bool receivedInitialState;
	private bool pendingInitialRotation;
	/// <summary>
	/// Has this matrix move finished receiving its initial state from the server and rotating into its correct
	/// position?
	/// </summary>
	public bool Initialized => clientStarted && receivedInitialState;

	private MatrixMoveNodes clientMoveNodes = new MatrixMoveNodes();

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
//		var oldState = clientState;
//		clientState = newState;
//		Logger.LogTraceFormat("{0} setting client / client target state from message {1}", Category.Matrix, this, newState);
//
//
//		if (!Equals(oldState.FacingDirection, newState.FacingDirection))
//		{
//			if (!receivedInitialState && !pendingInitialRotation)
//			{
//				pendingInitialRotation = true;
//			}
//			inProgressRotation = oldState.FacingDirection.OffsetTo(newState.FacingDirection);
//			Logger.LogTraceFormat("{0} starting rotation progress to {1}", Category.Matrix, this, newState.FacingDirection);
//			MatrixMoveEvents.OnRotate.Invoke(new MatrixRotationInfo(this, inProgressRotation.Value, NetworkSide.Client, RotationEvent.Start));
//		}
//
//		if (!oldState.IsMoving && newState.IsMoving)
//		{
//			MatrixMoveEvents.OnStartMovementClient.Invoke();
//		}
//
//		if (oldState.IsMoving && !newState.IsMoving)
//		{
//			MatrixMoveEvents.OnStopMovementClient.Invoke();
//		}
//
//		if ((int) oldState.Speed != (int) newState.Speed)
//		{
//			MatrixMoveEvents.OnSpeedChange.Invoke(oldState.Speed, newState.Speed);
//		}
//
//		if (!receivedInitialState && !pendingInitialRotation)
//		{
//			receivedInitialState = true;
//		}
	}

	private void CheckMovementClient()
	{
		if (NeedsRotationClient)
		{
			//rotate our transform to our new facing direction
			if (ServerState.RotationTime != 0)
			{
				//animate rotation
				transform.rotation =
					Quaternion.RotateTowards(transform.rotation,
						InitialFacing.OffsetTo(ServerState.FacingDirection).Quaternion,
						Time.deltaTime * ServerState.RotationTime);
			}
			else
			{
				//rotate instantly
				transform.rotation = InitialFacing.OffsetTo(ServerState.FacingDirection).Quaternion;
			}

			return;
		}
	}

	/// <summary>
	/// Performs the rotation / movement animation on all clients and server. Called every UpdateMe()
	/// </summary>
	private void AnimateMovement()
	{
//		if (Equals(clientState, MatrixState.Invalid))
//		{
//			return;
//		}
//
//		if (NeedsRotationClient)
//		{
//			//rotate our transform to our new facing direction
//			if (clientState.RotationTime != 0)
//			{
//				//animate rotation
//				transform.rotation =
//					Quaternion.RotateTowards(transform.rotation,
//						 InitialFacing.OffsetTo(clientState.FacingDirection).Quaternion,
//						Time.deltaTime * clientState.RotationTime);
//			}
//			else
//			{
//				//rotate instantly
//				transform.rotation = InitialFacing.OffsetTo(clientState.FacingDirection).Quaternion;
//			}
//		}
//		else if (IsMovingClient)
//		{
//			//Only move target if rotation is finished
//			//predict client state because we don't get constant updates when flying in one direction.
//			clientState.Position += (clientState.Speed * Time.deltaTime) * clientState.FlyingDirection.Vector;
//		}
//
//		//finish rotation (rotation event will be fired in lateupdate
//		if (!NeedsRotationClient && inProgressRotation != null)
//		{
//			// Finishes the job of Lerp and straightens the ship with exact angle value
//			transform.rotation = InitialFacing.OffsetTo(clientState.FacingDirection).Quaternion;
//		}
//
//		//Lerp
//		if (clientState.Position != transform.position)
//		{
//			float distance = Vector3.Distance(clientState.Position, transform.position);
//
//			//Teleport (Greater then 30 unity meters away from server target):
//			if (distance > 30f)
//			{
//				matrixPositionFilter.FilterPosition(transform, clientState.Position, clientState.FlyingDirection);
//				return;
//			}
//
//			transform.position = clientState.Position;
//
//			//If stopped then lerp to target (snap to grid)
//			if (!clientState.IsMoving )
//			{
//				if ( clientState.Position == transform.position )
//				{
//					MatrixMoveEvents.OnFullStopClient.Invoke();
//				}
//				if ( distance > 0f )
//				{
//					//TODO: Why is this needed? Seems weird.
//					matrixPositionFilter.SetPosition(transform.position);
//					return;
//				}
//			}
//
//			matrixPositionFilter.FilterPosition(transform, transform.position, clientState.FlyingDirection);
//		}
	}
}
