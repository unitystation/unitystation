using System;
using System.Collections;
using System.Collections.Generic;
using Logs;
using SecureStuff;
using UnityEngine;

public class AutopilotShipMachine : MonoBehaviour
{
	[PlayModeOnly]
	public GuidanceBuoy CurrentTarget;

	[HideInInspector]
	public MatrixMove mm;


	public ShuttleConnector ShuttlesMainConnector;

	public bool MoveDirectionIn = false;

	[PlayModeOnly]
	public GuidanceBuoy MovedToAfterFinishingChain;

	[PlayModeOnly]
	public GuidanceBuoy StartOfChain;

	private GuidanceBuoy PreviouslyReached = null;

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	public virtual void Start()
	{
		mm = this.GetComponentInParent<MatrixMove>();
		mm.NetworkedMatrixMove.RCSRequiresThrusters = false;
		mm.NetworkedMatrixMove.SpinneyThreshold = 9999;
	}


	public void MoveToTargetBuoy(GuidanceBuoy Buoy)
	{
		StartOfChain = Buoy;
		if (CurrentTarget != null)
		{
			MovedToAfterFinishingChain = Buoy;
			MoveDirectionIn = false;
		}
		else
		{
			MoveToInternal(Buoy);
		}

	}

	protected void MoveToInternal(GuidanceBuoy pos)
	{
		ShuttlesMainConnector.Disconnect();
		CurrentTarget = pos;
		mm.NetworkedMatrixMove.HasMoveToTarget = true;
		//mm.NetworkedMatrixMove.IgnoreMatrix = pos.RegisterTile.Matrix.MatrixInfo;
		if (MoveDirectionIn)
		{
			if (CurrentTarget.In.UseConnectorAsCentreOfShuttle)
			{
				mm.NetworkedMatrixMove.CentreObjectOverride = ShuttlesMainConnector.gameObject;
			}
			else
			{
				mm.NetworkedMatrixMove.CentreObjectOverride = null;
			}

			mm.NetworkedMatrixMove.TargetFaceDirectionOverride = CurrentTarget.In.DesiredFaceDirection;
		}
		else
		{
			if (CurrentTarget.Out.UseConnectorAsCentreOfShuttle)
			{
				mm.NetworkedMatrixMove.CentreObjectOverride = ShuttlesMainConnector.gameObject;
			}
			else
			{
				mm.NetworkedMatrixMove.CentreObjectOverride = null;
			}

			mm.NetworkedMatrixMove.TargetFaceDirectionOverride = CurrentTarget.Out.DesiredFaceDirection;
		}

		mm.NetworkedMatrixMove.HasMoveToTarget = true;
		mm.NetworkedMatrixMove.TravelToWorldPOS = pos.transform.position.RoundToInt();
	}

	protected void Reached(GuidanceBuoy pos)
	{
		if (MoveDirectionIn)
		{
			if (CurrentTarget.In.IsEnd)
			{
				if (PreviouslyReached == pos) return;
				PreviouslyReached = pos;
				if (CurrentTarget.In.ConnectTo != null && ShuttlesMainConnector.ConnectedToConnector == null)
				{
					ShuttlesMainConnector.TryConnectAdjacent();
				}


				ReachedEndOfInBuoyChain(CurrentTarget, StartOfChain);
			}
			else
			{
				mm.NetworkedMatrixMove.IgnorePotentialCollisions = true;
				PreviouslyReached = pos;
				MoveToInternal(CurrentTarget.In.NextInLine);
			}
		}
		else
		{
			if (CurrentTarget.Out.IsEnd)
			{

				if (PreviouslyReached == pos) return;
				PreviouslyReached = pos;
				//public ShuttleConnector ConnectTo;
				Loggy.LogError("End of line!!");

				var Backup = CurrentTarget;
				CurrentTarget = null;
				mm.NetworkedMatrixMove.TargetFaceDirectionOverride = OrientationEnum.Default;
				mm.NetworkedMatrixMove.HasMoveToTarget = false;
				MoveDirectionIn = true;
				//mm.NetworkedMatrixMove.IgnoreMatrix = null;
				mm.NetworkedMatrixMove.IgnorePotentialCollisions = false;
				if (MovedToAfterFinishingChain != null)
				{
					MoveToInternal(MovedToAfterFinishingChain);
				}

				ReachedEndOfOutBuoyChain(Backup);

			}
			else
			{
				PreviouslyReached = pos;
				mm.NetworkedMatrixMove.IgnorePotentialCollisions = true;
				MoveToInternal(CurrentTarget.Out.NextInLine);
			}
		}
	}

	public virtual void ReachedEndOfOutBuoyChain(GuidanceBuoy GuidanceBuoy)
	{

	}

	public virtual void ReachedEndOfInBuoyChain(GuidanceBuoy GuidanceBuoy, GuidanceBuoy StartOfChain)
	{

	}

	public virtual void UpdateMe()
	{


		if (mm.NetworkedMatrixMove.IsMoving)
		{
			if ((mm.NetworkedMatrixMove.CentreOfAIMovementWorld - mm.NetworkedMatrixMove.TravelToWorldPOS).magnitude < 100)
			{
				mm.NetworkedMatrixMove.AITravelSpeed = 10;
			}
			else
			{
				mm.NetworkedMatrixMove.AITravelSpeed = 80;
			}

			return;
		}
		else
		{
			if (CurrentTarget == null) return;
			var Difference = (mm.NetworkedMatrixMove.CentreOfAIMovementWorld - CurrentTarget.transform.position).magnitude;

			if (Difference < 0.5f)
			{

				Reached(CurrentTarget);
			}
		}
	}
}
