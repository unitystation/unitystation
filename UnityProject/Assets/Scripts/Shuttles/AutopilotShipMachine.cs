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

	public OrientationEnum DirectionOverride = OrientationEnum.Default;
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
		mm.NetworkedMatrixMove.rotationSpeed = 90;
		mm.NetworkedMatrixMove.ShuttleNonSpinneyModeRounding = 90;


	}


	public void MoveToTargetBuoy(GuidanceBuoy Buoy)
	{
		PreviouslyReached = null;
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

			if (CurrentTarget.In.DesiredFaceDirection == OrientationEnum.Default)
			{
				mm.NetworkedMatrixMove.TargetFaceDirectionOverride = OrientationEnum.Default;
			}
			else
			{
				if (DirectionOverride == OrientationEnum.Default)
				{
					mm.NetworkedMatrixMove.TargetFaceDirectionOverride = CurrentTarget.In.DesiredFaceDirection;
				}
				else
				{
					mm.NetworkedMatrixMove.TargetFaceDirectionOverride = DirectionOverride;
				}
			}
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

			if (CurrentTarget.Out.DesiredFaceDirection == OrientationEnum.Default)
			{
				mm.NetworkedMatrixMove.TargetFaceDirectionOverride = OrientationEnum.Default;
			}
			else
			{
				if (DirectionOverride == OrientationEnum.Default)
				{
					mm.NetworkedMatrixMove.TargetFaceDirectionOverride = CurrentTarget.Out.DesiredFaceDirection;
				}
				else
				{
					mm.NetworkedMatrixMove.TargetFaceDirectionOverride = DirectionOverride;
				}
			}
		}

		mm.NetworkedMatrixMove.HasMoveToTarget = true;
		mm.NetworkedMatrixMove.SetAITravelToPosition(pos.transform.position.RoundToInt(), pos.gameObject);
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

				var Backup = CurrentTarget;
				CurrentTarget = null;
				mm.NetworkedMatrixMove.TargetFaceDirectionOverride = OrientationEnum.Default;
				DirectionOverride = OrientationEnum.Default;
				mm.NetworkedMatrixMove.HasMoveToTarget = false;
				MoveDirectionIn = true;
				mm.NetworkedMatrixMove.IgnorePotentialCollisions = false;
				mm.NetworkedMatrixMove.IgnoreMatrix = null;
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
			mm.NetworkedMatrixMove.AITravelSpeed =
				(mm.NetworkedMatrixMove.CentreOfAIMovementWorld - mm.NetworkedMatrixMove.TravelToWorldPOS).magnitude < 100 ? 20 : 80;
		}
		else
		{
			if (CurrentTarget == null) return;
			var Difference = (mm.NetworkedMatrixMove.CentreOfAIMovementWorld.RoundToInt() - CurrentTarget.transform.position).magnitude;

			if (Difference < 0.5f)
			{
				Reached(CurrentTarget);
			}
		}
	}
}
