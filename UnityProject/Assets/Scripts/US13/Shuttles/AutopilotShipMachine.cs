using System.Collections;
using SecureStuff;
using UnityEngine;
using US13.Core.Transform;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Tilemaps.Behaviours.Meta;
using Util;

namespace US13.Shuttles
{
	public class AutopilotShipMachine : MonoBehaviour, IInitialiseSystem
	{
		public int Priority => priority;

		public int priority = 90;

		[PlayModeOnly]
		public GuidanceBuoy CurrentTarget;

		[HideInInspector]
		public MatrixMove mm;


		public ShuttleConnector ShuttlesMainConnector;

		[PlayModeOnly]
		public bool MoveDirectionIn = false;

		[PlayModeOnly]
		public GuidanceBuoy MovedToAfterFinishingChain;

		[PlayModeOnly]
		public GuidanceBuoy StartOfChain;

		private GuidanceBuoy PreviouslyReached = null;

		[PlayModeOnly]
		public bool HasPositionTarget = false;

		[PlayModeOnly]
		public Vector3 CurrentTargetPosition;


		public GuidanceBuoy InItAsIfDockedToBuoy;

		private MatrixSystemManager MatrixSystemManager;

		[PlayModeOnly] public OrientationEnum DirectionOverride = OrientationEnum.Default;
		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer == false) return;
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer == false) return;
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}



		/// <summary>
		/// Travel to a world position instead of a guidance buoy.
		/// Server only.
		/// </summary>
		public void MoveToPosition(Vector3 worldPosition, bool ignoreCollisions = true)
		{
			if (CustomNetworkManager.IsServer == false) return;
			ShuttlesMainConnector?.Disconnect();

			//Drop any buoy chain state so Reached() / MoveToTargetBuoy don't interfere
			PreviouslyReached = null;
			CurrentTarget = null;
			StartOfChain = null;
			MovedToAfterFinishingChain = null;
			MoveDirectionIn = false;

			var rounded = worldPosition.RoundToInt();
			HasPositionTarget = true;
			CurrentTargetPosition = rounded;

			mm.NetworkedMatrixMove.HasMoveToTarget = true;
			mm.NetworkedMatrixMove.CentreObjectOverride = null;
			mm.NetworkedMatrixMove.TargetFaceDirectionOverride = OrientationEnum.Default;
			mm.NetworkedMatrixMove.IgnorePotentialCollisions = ignoreCollisions;

			mm.NetworkedMatrixMove.SetAITravelToPosition(rounded, null);
		}

		public virtual void Start()
		{
			MatrixSystemManager = GetComponentInParent<MatrixSystemManager>();
			MatrixSystemManager.Register(this);
			mm = this.GetComponentInParent<MatrixMove>();
			mm.NetworkedMatrixMove.RCSRequiresThrusters = false;
			mm.NetworkedMatrixMove.SpinneyThreshold = 9999;
			mm.NetworkedMatrixMove.rotationSpeed = 90;
			mm.NetworkedMatrixMove.ShuttleNonSpinneyModeRounding = 90;
		}

		public void Initialize()
		{
			if (InItAsIfDockedToBuoy != null)
			{
				MoveDirectionIn = true;
				InItAsIfDockedTo(InItAsIfDockedToBuoy);
			}
		}

		public void InItAsIfDockedTo(GuidanceBuoy Buoy)
		{
			if (CustomNetworkManager.IsServer == false) return;
			StartOfChain = Buoy;
			CurrentTarget = Buoy;
			while (CurrentTarget.In.NextInLine != null)
			{
				CurrentTarget = CurrentTarget.In.NextInLine;
			}

			if (mm.NetworkedMatrixMove.IgnoreMatrixs.Contains(CurrentTarget.metaTileMap.matrix.MatrixInfo) == false)
			{
				mm.NetworkedMatrixMove.IgnoreMatrixs.Add(CurrentTarget.metaTileMap.matrix.MatrixInfo);
			}


			MoveToInternal(CurrentTarget);
		}


		public void MoveToTargetBuoy(GuidanceBuoy Buoy)
		{
			if (CustomNetworkManager.IsServer == false) return;
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
			if (CustomNetworkManager.IsServer == false) return;
			ShuttlesMainConnector?.Disconnect();
			CurrentTarget = pos;
			mm.NetworkedMatrixMove.HasMoveToTarget = true;
			//mm.NetworkedMatrixMove.IgnoreMatrix = pos.RegisterTile.Matrix.MatrixInfo;
			if (MoveDirectionIn)
			{
				if (CurrentTarget.In.UseConnectorAsCentreOfShuttle)
				{
					mm.NetworkedMatrixMove.CentreObjectOverride = ShuttlesMainConnector?.gameObject;
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
						mm.NetworkedMatrixMove.TargetFaceDirectionOverride = CurrentTarget.MatrixMove.NetworkedMatrixMove.CurrentOrientation.AddDirectionsTogether(CurrentTarget.In.DesiredFaceDirection);
					}
					else
					{
						mm.NetworkedMatrixMove.TargetFaceDirectionOverride =  CurrentTarget.MatrixMove.NetworkedMatrixMove.CurrentOrientation.AddDirectionsTogether(DirectionOverride);
					}
				}
			}
			else
			{
				if (CurrentTarget.Out.UseConnectorAsCentreOfShuttle)
				{
					mm.NetworkedMatrixMove.CentreObjectOverride = ShuttlesMainConnector?.gameObject;
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
						mm.NetworkedMatrixMove.TargetFaceDirectionOverride = CurrentTarget.MatrixMove.NetworkedMatrixMove.CurrentOrientation.AddDirectionsTogether( CurrentTarget.Out.DesiredFaceDirection);
					}
					else
					{
						mm.NetworkedMatrixMove.TargetFaceDirectionOverride =  CurrentTarget.MatrixMove.NetworkedMatrixMove.CurrentOrientation.AddDirectionsTogether(DirectionOverride);
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
				if (CurrentTarget.In.NextInLine == null)
				{
					if (PreviouslyReached == pos) return;
					PreviouslyReached = pos;
					if (CurrentTarget.In.ConnectTo != null && ShuttlesMainConnector.ConnectedToConnector == null)
					{
						ShuttlesMainConnector.TryConnectAdjacent();
					}
					mm.NetworkedMatrixMove.IgnoreMatrixs.Clear();
					mm.NetworkedMatrixMove.IgnorePotentialCollisions = true;
					ReachedEndOfInBuoyChain(CurrentTarget, StartOfChain);
				}
				else
				{
					mm.NetworkedMatrixMove.IgnoreMatrixs.Clear();
					mm.NetworkedMatrixMove.IgnorePotentialCollisions = true;
					PreviouslyReached = pos;
					MoveToInternal(CurrentTarget.In.NextInLine);
				}
			}
			else
			{
				if (CurrentTarget.Out.NextInLine == null)
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

					mm.NetworkedMatrixMove.ClosestCashed = null;

					mm.NetworkedMatrixMove.IgnoreMatrixs.Clear();
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

		protected void ReachedPosition()
		{
			HasPositionTarget = false;
			mm.NetworkedMatrixMove.HasMoveToTarget = false;
			mm.NetworkedMatrixMove.IgnorePotentialCollisions = false;
			mm.NetworkedMatrixMove.ClosestCashed = null;
			mm.NetworkedMatrixMove.IgnoreMatrixs.Clear();

			ReachedEndOfPositionTravel(CurrentTargetPosition);
		}

		public virtual void ReachedEndOfOutBuoyChain(GuidanceBuoy GuidanceBuoy)
		{

		}

		public virtual void ReachedEndOfInBuoyChain(GuidanceBuoy GuidanceBuoy, GuidanceBuoy StartOfChain)
		{

		}

		public virtual void ReachedEndOfPositionTravel(Vector3 position)
		{

		}
		public virtual void UpdateMe()
		{
			if (mm.NetworkedMatrixMove.IsMoving == false)
			{
				if (HasPositionTarget)
				{
					var PositionDifference = (mm.NetworkedMatrixMove.CentreOfAIMovementWorld.RoundToInt() - CurrentTargetPosition).magnitude;

					if (PositionDifference < 0.5f)
					{
						ReachedPosition();
					}
					return;
				}

				if (CurrentTarget == null) return;
				var Difference = (mm.NetworkedMatrixMove.CentreOfAIMovementWorld.RoundToInt() - CurrentTarget.transform.position).magnitude;

				if (Difference < 0.5f)
				{
					Reached(CurrentTarget);
				}
			}
		}
	}
}
