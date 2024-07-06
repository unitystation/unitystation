using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using InGameGizmos;
using Logs;
using Mirror;
using Objects.Shuttles;
using Shuttles;
using TileManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;


/// <summary>
/// Behavior which allows an entire matrix to move and rotate (and be synced over the network).
/// This behavior must go on a gameobject that is the parent of the gameobject that has the actual Matrix component.
/// </summary>
public class NetworkedMatrixMove : NetworkBehaviour
{
	[SyncVar(hook = nameof(SynchronisePosition))]
	public Vector3 SynchronisedPosition;

	[SyncVar(hook = nameof(SynchroniseRotation))]
	public Vector3 SynchronisedRotation;

	[SyncVar(hook = nameof(SynchroniseSpin))]
	public float SynchronisedSpin;

	[SyncVar(hook = nameof(SynchroniseMass))]
	public float SynchronisedMass;

	[SyncVar(hook = nameof(SynchroniseVelocity))]
	public Vector3 SynchronisedVelocity;

	[SyncVar(hook = nameof(SynchronisePivotPoint))]
	public Vector3 SynchronisedPivotPoint;

	public Transform TargetTransform;


	//TODO Look at Commented out code <<<<<8

	//TODO 90► snap updates For layer and rotatable
	//TODO  fuelGauge??

	//---------------- Not so important -------------------

	//TODO Cable placement can't handle moving matrices


	public List<Thruster> ConnectedThrusters = new List<Thruster>();
	public List<ShuttleConnector> ConnectedShuttleConnectors = new List<ShuttleConnector>();


	public bool HasConnectedShuttle => ConnectedShuttleConnectors.Any(x => x.ConnectedToConnector != null);

	public bool Safety = false;


	[Range(0.0f, 5f)] public float Drag = 0.001f;
	[Range(0.0f, 5f)] public float DragTorque = 0.05f;

	[Range(0.0f, 1f)] public float TileAlignmentSpeed = 0.5f;

	[Range(0.0f, 5f)] public float LowSpeedDrag = 0.3f;


	public float LowSpeedDragThreshold = 1f;

	public float HighSpeedDrag = 0.03f;

	public float HighSpeedDragMinimumThreshold = 65f;

	public float HighSpeedDrag100Threshold = 80f;

	[Range(0.0f, 1f)] public float SpinneyTurnVelocityBent = 0.75f;


	public bool IsNotPilotable = true;

	public Vector3 ForwardsDirection
	{
		get
		{
			Vector3 ForwardsVictor = Vector3.zero;
			if (ShuttleConsuls.Count == 0)
			{
				ForwardsVictor = this.TargetTransform.localToWorldMatrix.MultiplyVector(Vector3.up);
			}
			else
			{
				var Direction = ShuttleConsuls[0].Rotatable.CurrentDirection.ToOpposite();
				var VectorDirection = Direction.ToLocalVector3();
				ForwardsVictor = this.TargetTransform.localToWorldMatrix.MultiplyVector(VectorDirection);
			}


			return ForwardsVictor;
		}
	}

	public float Mass
	{
		get
		{
			if (isServer == false)
			{
				return SynchronisedMass;
			}

			lock (MetaTileMap.MassAndCentreLock)
			{
				return MetaTileMap.Mass;
			}
		}
	}

	public float SpinneyThreshold = 20f;

	public Vector3 CentreOfMass
	{
		get
		{
			lock (MetaTileMap.MassAndCentreLock)
			{
				return MetaTileMap.LocalCentreOfMass;
			}
		}
	}

	public float MoveCoolDown = 0;
	public float DragSpinneyCoolDown = 0;

	public bool ApplyDrag => (SpinneyMode == false || DragSpinneyCoolDown == 0);

	public bool SpinneyMode => WorldCurrentVelocity.magnitude >= SpinneyThreshold;

	public Vector3 WorldCurrentVelocity;

	public float CurrentTorque;

	public Vector3 currentLocalPivot;

	public Stopwatch ElapsedTimeSinceLastUpdate = new Stopwatch();

	public ObjectLayer ObjectLayer;

	public MetaTileMap MetaTileMap;

	public SpriteDataSO X;

	public GameGizmoSprite GameGizmoSprite;
	public GameGizmoSprite AIGameGizmoSprite;

	public List<GameGizmoSprite> MatrixBoundsGameGizmo = new List<GameGizmoSprite>();

	public bool Debug = false;

	public float rotationSpeed = 30f; //TODO Range depending on mass of shuttle? Adjust the rotation speed as needed

	public float ShuttleNonSpinneyModeRounding = 30f;

	//NOTE This is not in respect to the Orientation of the Front of the shuttle or the direction of the Shuttle consul, Just to do with the rotation of target transform
	public OrientationEnum CurrentOrientation => TargetTransform.eulerAngles.z.Angle360ToOrientationEnum();

	[SerializeField] private OrientationEnum targetOrientation = OrientationEnum.Default;

	//NOTE This is not in respect to the Orientation of the Front of the shuttle or the direction of the Shuttle consul, Just to do with the rotation of target transform
	public OrientationEnum TargetOrientation
	{
		get { return targetOrientation; }
		set
		{
			if (MoveCoolDown > 0) return;
			StartOrientation = CurrentOrientation;
			targetOrientation = value;
		}
	}

	//NOTE This is not in respect to the Orientation of the Front of the shuttle or the direction of the Shuttle consul, Just to do with the rotation of target transform
	public OrientationEnum StartOrientation = OrientationEnum.Default;

	public bool IsMoving = false;

	public event Action OnStartMovement;
	public event Action OnStopMovement;
	public event Action OnRotate;
	public event Action OnRotate90;


	public bool RCSModeActive = false; //TODO Check With other stuff

	public PlayerScript playerControllingRcs;

	public bool UpdateHandled = false;

	public HashSet<NetworkedMatrixMove> TheReusingSet = new HashSet<NetworkedMatrixMove>();

	public List<Thruster> TheReusingConnectedThrusters = new List<Thruster>();
	public bool RCSRequiresThrusters = true;
	public List<ShuttleConsole> ShuttleConsuls = new List<ShuttleConsole>();

	public float AITravelSpeed = 10;

	public Vector3 TravelToWorldPOS
	{
		get
		{
			if (travelToWorldPOSOverride != null)
			{
				return travelToWorldPOSOverride.Value;
			}

			if (TravelToObject != null)
			{
				return TravelToObject.transform.position;
			}

			return travelToWorldPOS;
		}
	}

	public GameObject TravelToObject;

	public Vector3? travelToWorldPOSOverride;

	public Vector3 travelToWorldPOS;

	public bool HasMoveToTarget = false;
	public bool ISMovingX = false;
	public OrientationEnum TargetFaceDirectionOverride;
	public bool FullAISpeed = false;
	public bool isMovingAroundMatrix = false;


	public Vector3? PointIsWithinMatrixPerimeterPoint;

	public int? MatrixMoveAroundCurrentTargetCorner = null;

	public MatrixInfo MovingAroundMatrix;

	public MatrixInfo IgnoreMatrix;
	public bool IgnorePotentialCollisions;


	public Vector3 CentreOfAIMovementWorld
	{
		get
		{
			if (CentreObjectOverride != null)
			{
				return CentreObjectOverride.transform.position;
			}
			else
			{
				return CentreOfMass.ToWorld(MetaTileMap.matrix);
			}
		}
	}

	public GameObject CentreObjectOverride;

	//Used to tell if rotatable need an update
	private OrientationEnum PreviousDirectionFacing;

	public void Awake()
	{
		if (TargetTransform == null)
		{
			TargetTransform = transform.parent;
		}

		if (this.GetComponent<MatrixSync>() == null)
		{
			Loggy.LogError($"Please remove this {this.name}");
			Destroy(this);
			return;
		}

		if (TargetTransform == null)
		{
			TargetTransform = transform.parent;
		}


		MetaTileMap = TargetTransform.GetComponentInChildren<MetaTileMap>();
		ObjectLayer = TargetTransform.GetComponentInChildren<ObjectLayer>();


		UpdateManager.Add(CallbackType.EARLY_UPDATE, UpdateLoop);
		ElapsedTimeSinceLastUpdate.Reset();
		ElapsedTimeSinceLastUpdate.Start();
		OnRotate?.Invoke();
		var FacedDirection = ForwardsDirection.ToOrientationEnum();
		if (PreviousDirectionFacing != FacedDirection)
		{
			PreviousDirectionFacing = FacedDirection;
			OnRotate90?.Invoke();
		}

		SetGizmoPosition(currentLocalPivot);
	}

	public void OnDestroy()
	{
		OnRotate = null;
		OnRotate90 = null;
		OnStartMovement = null;
		OnStopMovement = null;
		UpdateManager.Remove(CallbackType.EARLY_UPDATE, UpdateLoop);
		ElapsedTimeSinceLastUpdate.Stop();
	}

	public bool IsConnectedToShuttle(NetworkedMatrixMove NetMove)
	{
		TheReusingSet.Clear();
		var Matrixes = GetAllNetworkedMatrixMove(TheReusingSet);
		return Matrixes.Contains(NetMove);
	}

	[NaughtyAttributes.Button]
	public void StartUpdating()
	{
		UpdateManager.Add(CallbackType.EARLY_UPDATE, UpdateLoop);
		ElapsedTimeSinceLastUpdate.Reset();
		ElapsedTimeSinceLastUpdate.Start();
	}

	[NaughtyAttributes.Button]
	public void StopUpdating()
	{
		UpdateManager.Remove(CallbackType.EARLY_UPDATE, UpdateLoop);
		ElapsedTimeSinceLastUpdate.Stop();
	}

	public void SetGizmoPosition(Vector3 Position)
	{
		if (Debug)
		{
			if (GameGizmoSprite == null)
			{
				GameGizmoSprite =
					GameGizmomanager.AddNewSpriteStaticClient(ObjectLayer.gameObject, Position, Color.green, X);
			}

			GameGizmoSprite.Position = Position;
		}
		else if (GameGizmoSprite != null)
		{
			GameGizmoSprite.Remove();
			GameGizmoSprite = null;
		}
	}


	public HashSet<NetworkedMatrixMove> GetAllNetworkedMatrixMove(HashSet<NetworkedMatrixMove> ToUse)
	{
		if (ToUse.Contains(this)) return ToUse;

		ToUse.Add(this);
		foreach (var ConnectedShuttleConnector in ConnectedShuttleConnectors)
		{
			if (ConnectedShuttleConnector.ConnectedToConnector?.RelatedMove?.NetworkedMatrixMove == null) continue;
			ConnectedShuttleConnector.ConnectedToConnector.RelatedMove.NetworkedMatrixMove
				.GetAllNetworkedMatrixMove(ToUse);
		}

		return ToUse;
	}

	public Vector3 GetAllCentreOfMass(HashSet<NetworkedMatrixMove> ToUseMatrixMove)
	{
		float TotalMass = 0;
		Vector3 Positions = Vector3.zero;
		foreach (var MatrixMove in ToUseMatrixMove)
		{
			Positions += MatrixMove.CentreOfMass.ToWorld(MatrixMove.MetaTileMap.matrix) * MatrixMove.Mass;
			TotalMass += MatrixMove.Mass;
		}

		return Positions / TotalMass;
	}

	public float GetAllMass(HashSet<NetworkedMatrixMove> ToUseMatrixMove)
	{
		float mass = 0;
		foreach (var MatrixMove in ToUseMatrixMove)
		{
			mass += MatrixMove.Mass;
		}

		return mass;
	}

	public List<Thruster> GetThrusters(HashSet<NetworkedMatrixMove> ToUseMatrixMove, List<Thruster> thrusters)
	{
		foreach (var MatrixMove in ToUseMatrixMove)
		{
			thrusters.AddRange(MatrixMove.ConnectedThrusters);
		}

		return thrusters;
	}

	public void TurnOffAllThrusters()
	{
		foreach (var Thruster in ConnectedThrusters)
		{
			Thruster.SetTargetMolesUsed(Thruster.MaxMolesUseda * 0);
		}
	}

	private void InternalSetThrusterStrength(Thruster.ThrusterDirectionClassification Direction, float Multiplier)
	{
		if (SpinneyMode || Direction == Thruster.ThrusterDirectionClassification.Up ||
		    Direction == Thruster.ThrusterDirectionClassification.Down)
		{
			foreach (var Thruster in ConnectedThrusters)
			{
				if (Thruster.ThisThrusterDirectionClassification == Direction)
				{
					Thruster.SetTargetMolesUsed(Thruster.MaxMolesUseda * Multiplier);
				}
			}
		}
		else
		{
			foreach (var Thruster in ConnectedThrusters)
			{
				if (Thruster.ThisThrusterDirectionClassification != Thruster.ThrusterDirectionClassification.Up &&
				    Thruster.ThisThrusterDirectionClassification != Thruster.ThrusterDirectionClassification.Down)
				{
					Thruster.SetTargetMolesUsed(Thruster.MaxMolesUseda * 0);
				}
			}


			if (Multiplier < 0.9f) return;
			var CurrentOrientation = TargetTransform.eulerAngles.z.Angle360ToOrientationEnum();

			if (Direction == Thruster.ThrusterDirectionClassification.Right)
			{
				switch (CurrentOrientation)
				{
					case OrientationEnum.Up_By0:
						TargetOrientation = OrientationEnum.Right_By270;
						break;
					case OrientationEnum.Right_By270:
						TargetOrientation = OrientationEnum.Down_By180;
						break;
					case OrientationEnum.Down_By180:
						TargetOrientation = OrientationEnum.Left_By90;
						break;
					case OrientationEnum.Left_By90:
						TargetOrientation = OrientationEnum.Up_By0;
						break;
				}
			}
			else
			{
				switch (CurrentOrientation)
				{
					case OrientationEnum.Up_By0:
						TargetOrientation = OrientationEnum.Left_By90;
						break;
					case OrientationEnum.Left_By90:
						TargetOrientation = OrientationEnum.Down_By180;
						break;
					case OrientationEnum.Down_By180:
						TargetOrientation = OrientationEnum.Right_By270;
						break;
					case OrientationEnum.Right_By270:
						TargetOrientation = OrientationEnum.Up_By0;
						break;
				}
			}
		}
	}

	public void SetThrusterStrength(Thruster.ThrusterDirectionClassification Direction, float Multiplier)
	{
		TheReusingSet.Clear();
		var Matrixes = GetAllNetworkedMatrixMove(TheReusingSet);
		foreach (var move in Matrixes)
		{
			move.InternalSetThrusterStrength(Direction, Multiplier);
		}
	}

	public void AddConnector(ShuttleConnector ShuttleConnector)
	{
		if (ConnectedShuttleConnectors.Contains(ShuttleConnector) == false)
		{
			ConnectedShuttleConnectors.Add(ShuttleConnector);
		}
	}


	public void RemoveConnector(ShuttleConnector ShuttleConnector)
	{
		if (ConnectedShuttleConnectors.Contains(ShuttleConnector))
		{
			ConnectedShuttleConnectors.Remove(ShuttleConnector);
		}
	}


	public void AddThruster(Thruster Thruster)
	{
		if (ConnectedThrusters.Contains(Thruster) == false)
		{
			ConnectedThrusters.Add(Thruster);
		}
	}


	public void RemoveThruster(Thruster Thruster)
	{
		if (ConnectedThrusters.Contains(Thruster))
		{
			ConnectedThrusters.Remove(Thruster);
		}
	}

	public void RcsMove(Orientation GlobalMoveDirection)
	{
		RcsMove(GlobalMoveDirection.LocalVector.ToOrientationEnum());
	}

	public void RcsMove(OrientationEnum GlobalMoveDirection)
	{
		if (IsMoving) return;

		bool HasThrusterDirection = false;
		TheReusingSet.Clear();
		var Matrixes = GetAllNetworkedMatrixMove(TheReusingSet);
		var Thrusters = GetThrusters(Matrixes, TheReusingConnectedThrusters);
		if (RCSRequiresThrusters)
		{
			foreach (var Thruster in Thrusters)
			{
				if (((Vector3) Thruster.Rotatable.WorldDirection).ToOrientationEnum() ==
				    GlobalMoveDirection.ToOpposite())
				{
					HasThrusterDirection = true;
					break;
				}
			}
		}
		else
		{
			HasThrusterDirection = true;
		}


		if (HasThrusterDirection)
		{


			foreach (var Matrix in Matrixes)
			{
				Matrix.WorldCurrentVelocity += GlobalMoveDirection.ToLocalVector3();
			}
		}
	}


	public void UpdateLoop()
	{
		ElapsedTimeSinceLastUpdate.Stop();
		float DeltaTimeSeconds = (float) ElapsedTimeSinceLastUpdate.Elapsed.TotalSeconds;
		ElapsedTimeSinceLastUpdate.Reset(); //TODO Editor pausing??
		ElapsedTimeSinceLastUpdate.Start();

		if (UpdateHandled)
		{
			UpdateHandled = false;
			return;
		}

		//bug note will bug out with shuttle connectors
		MonitorAutopilot();

		TheReusingSet.Clear();
		var Matrixes = GetAllNetworkedMatrixMove(TheReusingSet);
		TheReusingConnectedThrusters.Clear();
		var Thrusters = GetThrusters(Matrixes, TheReusingConnectedThrusters);
		var AllMass = GetAllMass(Matrixes);
		var WoldCentreOfMass = GetAllCentreOfMass(Matrixes);

		if (AllMass == 0) return;

		var AllRCSModeActive = Matrixes.Any(x => x.RCSModeActive);
		Vector3 WorldPivot = Vector2.zero;


		float sumThrust = 0;

		if (SpinneyMode)
		{
			foreach (Thruster Thruster in Thrusters)
			{
				var ThrusterMagnitude = Thruster.WorldThrustDirectionAndMagnitude.magnitude;

				if (Mathf.Abs(ThrusterMagnitude) > 0 && Mathf.Abs(sumThrust + ThrusterMagnitude) > float.Epsilon)
				{
					var ScalerThrusterMagnitude =
						ThrusterMagnitude
						/
						(ThrusterMagnitude + sumThrust);

					WorldPivot = Vector2.Lerp(WorldPivot, Thruster.transform.position, ScalerThrusterMagnitude);
				}

				sumThrust += ThrusterMagnitude;
			}


			var MassMagnitude = AllMass; //Because your mass doesn't like being moved and counterbalances it

			/*
			if (Mathf.Abs(MassMagnitude) > 0 && Mathf.Abs(sumThrust + MassMagnitude) > float.Epsilon)
			{
				var ScalerThrusterMagnitude =
					MassMagnitude
					/
					(MassMagnitude + sumThrust);


				WorldPivot = Vector2.Lerp(WorldPivot, WoldCentreOfMass, ScalerThrusterMagnitude);
			}
			*/

			float sumTorques = 0;


			foreach (Thruster Thruster in Thrusters)
			{
				// Calculate the torque using the cross product to consider the position
				float torque = Vector3.Cross(WorldPivot - (Vector3) Thruster.transform.position,
					(Vector3) Thruster.WorldThrustDirectionAndMagnitude).z;
				sumTorques += torque;
			}


			sumTorques *= DeltaTimeSeconds;

			if (Mathf.Abs(sumTorques) > 0 && Mathf.Abs(CurrentTorque + sumTorques) > float.Epsilon)
			{
				var ScalerSumTorques =
					sumTorques
					/
					(sumTorques + CurrentTorque);


				currentLocalPivot = Vector2.Lerp(currentLocalPivot, WorldPivot.ToLocal(MetaTileMap.matrix),
					ScalerSumTorques);
			}

			var PivotDifference = (WoldCentreOfMass - currentLocalPivot.ToWorld(MetaTileMap.matrix));
			var MomentumStrength = PivotDifference.magnitude * CurrentTorque * DeltaTimeSeconds;

			CurrentTorque += sumTorques / AllMass;

			if (PivotDifference.magnitude > 0 && MomentumStrength > 0)
			{
				var TorquesDifference = sumTorques / AllMass;

				var ScalerMomentumStrength =
					MomentumStrength
					/
					(MomentumStrength + TorquesDifference);

				currentLocalPivot = Vector2.Lerp(currentLocalPivot, WoldCentreOfMass.ToLocal(MetaTileMap.matrix),
					ScalerMomentumStrength);


				//TODO Balance the WorldCurrentVelocity added Because it doesn't seem to be strong enough whenTwo shuttle split apart meybe 2x Faster?

				WorldCurrentVelocity += (new Vector3(-PivotDifference.y, PivotDifference.x, 0).normalized *
				                         ((ScalerMomentumStrength) * (MomentumStrength / AllMass)));
			}
		}
		else
		{
			currentLocalPivot = WoldCentreOfMass.ToLocal(MetaTileMap.matrix);
			if (HasMoveToTarget)
			{
				currentLocalPivot = CentreOfAIMovementWorld.ToLocal(MetaTileMap.matrix);
			}
		}


		if (MoveCoolDown == 0)
		{
			Vector3 OverallthrustDirection = Vector3.zero;

			foreach (var thruster in Thrusters)
			{
				// Calculate the vector from center of mass to force position
				Vector3 r = (Vector3) thruster.transform.position - WoldCentreOfMass;

				// Calculate the component of force along the line connecting force position to center of mass
				Vector3 forceComponent = Vector3.Dot(thruster.WorldThrustDirectionAndMagnitude, r) / r.sqrMagnitude * r;
				forceComponent.z = 0;
				OverallthrustDirection -= forceComponent;
			}

			WorldCurrentVelocity += (OverallthrustDirection * DeltaTimeSeconds) / AllMass;
		}
		else
		{
			WorldCurrentVelocity *= 0;
			MoveCoolDown -= DeltaTimeSeconds;
			if (MoveCoolDown < 0)
			{
				MoveCoolDown = 0;
			}
		}


		bool DoUpdateLocalPosition = false;

		if (WorldCurrentVelocity.magnitude > 0 && ApplyDrag)
		{
			DoUpdateLocalPosition = true;
			WorldCurrentVelocity = ApplyDragTo(WorldCurrentVelocity, Drag, DeltaTimeSeconds);
		}

		if (WorldCurrentVelocity.magnitude > 0 && WorldCurrentVelocity.magnitude < LowSpeedDragThreshold)
		{
			DoUpdateLocalPosition = true;
			WorldCurrentVelocity = ApplyDragTo(WorldCurrentVelocity, LowSpeedDrag, DeltaTimeSeconds);
		}


		if (Mathf.Abs(WorldCurrentVelocity.x) > HighSpeedDragMinimumThreshold && ApplyDrag)
		{
			var MomentumDifference = Mathf.Abs(WorldCurrentVelocity.x) - HighSpeedDragMinimumThreshold;
			var DragMultiplier = MomentumDifference / (HighSpeedDrag100Threshold - HighSpeedDragMinimumThreshold);
			WorldCurrentVelocity.x =
				ApplyDragTo(WorldCurrentVelocity.x, (HighSpeedDrag * DragMultiplier), DeltaTimeSeconds);
		}


		if (Mathf.Abs(WorldCurrentVelocity.y) > HighSpeedDragMinimumThreshold && ApplyDrag)
		{
			var MomentumDifference = Mathf.Abs(WorldCurrentVelocity.y) - HighSpeedDragMinimumThreshold;
			var DragMultiplier = MomentumDifference / (HighSpeedDrag100Threshold - HighSpeedDragMinimumThreshold);
			WorldCurrentVelocity.y =
				ApplyDragTo(WorldCurrentVelocity.y, (HighSpeedDrag * DragMultiplier), DeltaTimeSeconds);
		}


		if (SpinneyMode == false && TargetOrientation == OrientationEnum.Default)
		{
			if (Mathf.Abs(WorldCurrentVelocity.x) < 0.50f)
			{
				var Position = TargetTransform.position;
				if (WorldCurrentVelocity.x > 0f)
				{
					Position.x += 0.45f;
				}
				else
				{
					Position.x -= 0.45f;
				}

				Position.x = Mathf.Round(Position.x);

				SetTransformPosition(
					Vector3.Lerp(TargetTransform.position, Position, 2 * TileAlignmentSpeed * DeltaTimeSeconds), false,
					Matrixes);
			}

			if (Mathf.Abs(WorldCurrentVelocity.y) < 0.50f)
			{
				var Position = TargetTransform.position;
				if (WorldCurrentVelocity.y > 0)
				{
					Position.y += 0.45f;
				}
				else
				{
					Position.y -= 0.45f;
				}

				Position.y = Mathf.Round(Position.y);

				SetTransformPosition(
					Vector3.Lerp(TargetTransform.position, Position, 2 * TileAlignmentSpeed * DeltaTimeSeconds), false,
					Matrixes);
			}
		}


		if (Mathf.Abs(CurrentTorque) > 0 && ApplyDrag)
		{
			DoUpdateLocalPosition = true;
			CurrentTorque = ApplyDragTo(CurrentTorque, DragTorque, DeltaTimeSeconds);
		}

		if (SpinneyMode == false && AllRCSModeActive == false && TargetFaceDirectionOverride == OrientationEnum.Default)
		{
			var dotProduct = Vector3.Dot(WorldCurrentVelocity.normalized, ForwardsDirection.normalized);
			WorldCurrentVelocity = ForwardsDirection * (dotProduct * WorldCurrentVelocity.magnitude);
		}


		SetTransformPosition(TargetTransform.position + (Vector3)
			((Vector3) (WorldCurrentVelocity) * DeltaTimeSeconds), false, Matrixes);

		if (DragSpinneyCoolDown > 0)
		{
			DragSpinneyCoolDown -= DeltaTimeSeconds;
			if (DragSpinneyCoolDown < 0)
			{
				DragSpinneyCoolDown = 0;
			}
		}

		if (SpinneyMode)
		{
			TargetOrientation = OrientationEnum.Default;
			var KeepMomentum =
				this.TargetTransform.worldToLocalMatrix.MultiplyVector(WorldCurrentVelocity * SpinneyTurnVelocityBent);
			WorldCurrentVelocity -= SpinneyTurnVelocityBent * WorldCurrentVelocity;
			TransformUpdateRotate(ObjectLayer.transform.TransformPoint(currentLocalPivot),
				CurrentTorque * DeltaTimeSeconds, false, Matrixes);
			WorldCurrentVelocity += this.TargetTransform.localToWorldMatrix.MultiplyVector(KeepMomentum);
			CheckCollisions();
		}
		else if (TargetOrientation != OrientationEnum.Default)
		{
			CurrentTorque = 0;
			// Calculate the rotation step based on the rotation speed and time.deltaTime
			float step = rotationSpeed * DeltaTimeSeconds;

			// Calculate the difference between the target rotation and current rotation
			float angleDifference = Mathf.DeltaAngle(TargetTransform.eulerAngles.z,
				TargetOrientation.ToQuaternion().eulerAngles.z);

			// Determine the rotation direction (clockwise or anticlockwise)
			int direction = (angleDifference < 0) ? -1 : 1;


			var KeepMomentum = this.TargetTransform.worldToLocalMatrix.MultiplyVector(WorldCurrentVelocity);


			// If the difference is small, snap to the target rotation
			if (Mathf.Abs(angleDifference) < step)
			{
				TransformUpdateRotate(ObjectLayer.transform.TransformPoint(currentLocalPivot).RoundToInt(),
					angleDifference, false, Matrixes);
				TargetOrientation = OrientationEnum.Default;
			}
			else
			{
				// Rotate the object around the pivot using transform.RotateAround
				TransformUpdateRotate(ObjectLayer.transform.TransformPoint(currentLocalPivot).RoundToInt(),
					direction * rotationSpeed * DeltaTimeSeconds, false, Matrixes);
			}

			if (HasMoveToTarget == false)
			{
				WorldCurrentVelocity = this.TargetTransform.localToWorldMatrix.MultiplyVector(KeepMomentum);
			}
		}
		else
		{
			CurrentTorque = 0;
			// Get the current rotation of the object
			float currentRotation = TargetTransform.eulerAngles.z;

			// Round the current rotation to the nearest 90 degrees to determine the cardinal direction
			float roundedRotation = Mathf.Round(currentRotation / 90) * 90;

			// Determine the target rotation based on the cardinal direction
			float targetRotation = roundedRotation;

			// Calculate the rotation step based on the rotation speed
			float step = ShuttleNonSpinneyModeRounding * DeltaTimeSeconds;

			// Calculate the difference between the target rotation and current rotation
			float angleDifference = Mathf.DeltaAngle(TargetTransform.eulerAngles.z, targetRotation);

			// Determine the rotation direction (clockwise or anticlockwise)
			int direction = (angleDifference < 0) ? -1 : 1;

			// If the difference is small, snap to the target rotation
			if (Mathf.Abs(angleDifference) < step)
			{
				TransformSetEuler(new Vector3(0, 0, targetRotation), false, Matrixes);
			}
			else
			{
				// Rotate the object around the pivot using transform.RotateAround
				TransformUpdateRotate(ObjectLayer.transform.TransformPoint(currentLocalPivot),
					direction * ShuttleNonSpinneyModeRounding * DeltaTimeSeconds, false, Matrixes);
			}
		}

		if (DoUpdateLocalPosition)
		{
			foreach (var Matrixe in Matrixes)
			{
				Matrixe.UpdateLocalAndWorldConversion();
			}
		}

		if (WorldCurrentVelocity.magnitude > 0.001f || Mathf.Abs(CurrentTorque) > 0.001f ||
		    TargetOrientation != OrientationEnum.Default)
		{
			if (IsMoving == false)
			{
				foreach (var Matrixe in Matrixes)
				{
					Matrixe.OnStartMovement?.Invoke();
					Matrixe.IsMoving = true;
				}
			}

			IsMoving = true;
		}
		else
		{
			if (IsMoving == true)
			{
				foreach (var Matrixe in Matrixes.ToList())
				{
					Matrixe.OnStopMovement?.Invoke();
					Matrixe.IsMoving = false;
				}
			}

			IsMoving = false;
		}

		foreach (var Matrixe in Matrixes)
		{
			Matrixe.WorldCurrentVelocity = WorldCurrentVelocity;
			Matrixe.CurrentTorque = CurrentTorque;
			Matrixe.UpdateSyncVars();
			Matrixe.SetGizmoPosition(currentLocalPivot.ToWorld(MetaTileMap.matrix).ToLocal(Matrixe.MetaTileMap.matrix));

			if (Matrixe != this)
			{
				Matrixe.UpdateHandled = true;
			}
		}
	}

	public Vector3 ApplyDragTo(Vector3 CurrentMomentum, float Drag, float deltaTimeSeconds)
	{
		CurrentMomentum -= (CurrentMomentum * (Drag * deltaTimeSeconds));
		return CurrentMomentum;
	}

	public float ApplyDragTo(float CurrentMomentum, float Drag, float deltaTimeSeconds)
	{
		CurrentMomentum -= (CurrentMomentum * (Drag * deltaTimeSeconds));
		return CurrentMomentum;
	}

	public void UpdateLocalAndWorldConversion()
	{
		MetaTileMap.UpdateTransformMatrix();
	}


	public void UpdateSyncVars()
	{
		if (isServer == false) return;
		if (Mathf.Approximately(SynchronisedSpin, CurrentTorque))
		{
			SynchroniseSpin(SynchronisedSpin, CurrentTorque);
		}

		if (Mathf.Approximately(SynchronisedMass, Mass))
		{
			SynchroniseMass(SynchronisedMass, Mass);
		}

		if (SynchronisedVelocity != WorldCurrentVelocity)
		{
			SynchroniseVelocity(SynchronisedVelocity, WorldCurrentVelocity);
		}

		if (SynchronisedPivotPoint != currentLocalPivot)
		{
			SynchronisePivotPoint(SynchronisedPivotPoint, currentLocalPivot);
		}


		if (SynchronisedPosition != TargetTransform.position)
		{
			SynchronisePosition(SynchronisedPosition, TargetTransform.position);
		}

		if (SynchronisedRotation != TargetTransform.rotation.eulerAngles)
		{
			SynchroniseRotation(SynchronisedRotation, TargetTransform.rotation.eulerAngles);
		}
	}

	public void SynchronisePosition(Vector3 OldPosition, Vector3 NewPosition)
	{
		SynchronisedPosition = NewPosition;
		SetTransformPosition(NewPosition);
	}


	public void SynchroniseSpin(float OldSpin, float NewSpin)
	{
		SynchronisedSpin = NewSpin;
		CurrentTorque = NewSpin;
	}


	public void SynchroniseMass(float OldMass, float NewMass)
	{
		SynchronisedMass = NewMass;
	}


	public void SynchroniseVelocity(Vector3 OldVelocity, Vector3 NewVelocity)
	{
		SynchronisedVelocity = NewVelocity;
		WorldCurrentVelocity = NewVelocity;
	}

	public void SynchronisePivotPoint(Vector3 OldPivotPoint, Vector3 NewPivotPoint)
	{
		SynchronisedPivotPoint = NewPivotPoint;
		currentLocalPivot = NewPivotPoint;
	}

	public void SynchroniseRotation(Vector3 OldRotation, Vector3 NewRotation)
	{
		SynchronisedRotation = NewRotation;
		TargetTransform.rotation = Quaternion.Euler(NewRotation);
		UpdateLocalAndWorldConversion();
	}


	public void SetTransformPosition(Vector3 NewPosition, bool UpdateConversion = true,
		HashSet<NetworkedMatrixMove> Matrixs = null)
	{
		if (Matrixs != null)
		{
			foreach (var matrix in Matrixs)
			{
				if (matrix == this) continue;
				var Offset = TargetTransform.position - matrix.TargetTransform.position;
				matrix.SetTransformPosition(NewPosition - Offset, UpdateConversion);
			}
		}


		TargetTransform.position = NewPosition;
		if (UpdateConversion)
		{
			UpdateLocalAndWorldConversion();
		}
	}

	public void TransformUpdateRotate(Vector3 RotateAround, float By, bool UpdateConversion = true,
		HashSet<NetworkedMatrixMove> Matrixs = null)
	{
		if (Matrixs != null)
		{
			foreach (var matrix in Matrixs)
			{
				if (matrix == this) continue;
				matrix.TransformUpdateRotate(RotateAround, By, UpdateConversion);
			}
		}

		Vector3 axis = new Vector3(0, 0, 1);
		TargetTransform.RotateAround(RotateAround, axis, By);

		if (Mathf.Abs(By) > 0)
		{
			OnRotate?.Invoke();
		}

		var facedDirection = ForwardsDirection.ToOrientationEnum();
		if (PreviousDirectionFacing != facedDirection)
		{
			PreviousDirectionFacing = facedDirection;
			OnRotate90?.Invoke();
		}

		if (UpdateConversion)
		{
			UpdateLocalAndWorldConversion();
		}
	}

	public void TransformSetEuler(Vector3 Euler, bool UpdateConversion = true,
		HashSet<NetworkedMatrixMove> Matrixs = null)
	{
		var setQuaternion = new Quaternion();
		setQuaternion.eulerAngles = Euler;
		TransformSetQuaternion(setQuaternion, UpdateConversion);
	}

	public void TransformSetQuaternion(Quaternion SetTO, bool UpdateConversion = true,
		HashSet<NetworkedMatrixMove> Matrixs = null)
	{
		if (Matrixs != null)
		{
			foreach (var matrix in Matrixs)
			{
				if (matrix == this) continue;
				var Offset = Quaternion.Inverse(TargetTransform.rotation) * matrix.TargetTransform.rotation;
				matrix.TransformSetQuaternion(SetTO * Offset, UpdateConversion);
			}
		}

		var difference = TargetTransform.rotation.eulerAngles.z - SetTO.eulerAngles.z;

		TargetTransform.rotation = SetTO;

		if (Mathf.Abs(difference) > 0)
		{
			OnRotate?.Invoke();
		}

		var FacedDirection = ForwardsDirection.ToOrientationEnum();
		if (PreviousDirectionFacing != FacedDirection)
		{
			PreviousDirectionFacing = FacedDirection;
			OnRotate90?.Invoke();
		}


		if (UpdateConversion)
		{
			UpdateLocalAndWorldConversion();
		}
	}


	#region ShuttleCollision

	public void CheckCollisions()
	{
		if (SpinneyMode == false) return;

		if (Safety == false) return;
		//Basically the air movement
		var thisBigBound = MetaTileMap.matrix.MatrixInfo.WorldBounds.ExpandAllDirectionsBy(10);

		foreach (var Matrix in MatrixManager.Instance.ActiveMatrices)
		{
			if ((Matrix.Value.WorldBounds.center - CentreOfAIMovementWorld).magnitude > 1000) continue;
			if (Matrix.Value == MatrixManager.Instance.spaceMatrix.MatrixInfo) continue;
			if (Matrix.Value == MetaTileMap.matrix.MatrixInfo) continue;
			if (TheReusingSet.Contains(Matrix.Value.MatrixMove.NetworkedMatrixMove)) continue;

			var OtherBigBound = Matrix.Value.WorldBounds.ExpandAllDirectionsBy(10);

			if (thisBigBound.Intersects(OtherBigBound, out var Overlap))
			{
				WorldCurrentVelocity = WorldCurrentVelocity.normalized * (SpinneyThreshold - 1);
			}
		}
	}

	#endregion

	#region AIMOVE

	public void MonitorAutopilot()
	{
		if (HasMoveToTarget == false) return;


		CheckMatrixRoute();

		//Loggy.LogError("code here");
		var Different = TravelToWorldPOS - CentreOfAIMovementWorld.RoundToInt();

		if (Mathf.Abs(Different.x) < 1.5f && Mathf.Abs(Different.y) < 1.5f)
		{
			if (FullAISpeed)
			{
				WorldCurrentVelocity = Vector3.zero;
				FullAISpeed = false;
			}

			//RCS
			RCSModeActive = true;

			if (Different.magnitude > 0.5f)
			{
				RcsMove(Different.normalized.ToOrientationEnum());
			}
			else
			{
				//Loggy.LogError("HERE!!");
			}
		}
		else
		{
			FullAISpeed = true;
			RCSModeActive = false;
			if (ISMovingX)
			{
				if (Mathf.Abs(Different.x) > 1)
				{
					if (Different.x > 0)
					{
						WorldCurrentVelocity = new Vector3(AITravelSpeed, 0, 0);
					}
					else
					{
						WorldCurrentVelocity = new Vector3(-AITravelSpeed, 0, 0);
					}

					if (TargetOrientation == OrientationEnum.Default)
					{
						// var Direction = ShuttleConsuls[0].Rotatable.CurrentDirection.ToOpposite();
						//
						// var FordsDirectionZ = Direction.ToLocalVector3().ToOrientationEnum().ToQuaternion().eulerAngles.z;
						//
						//
						// var CopyVelocity = WorldCurrentVelocity.normalized;
						// var WantingToFaceZ = CopyVelocity.ToOrientationEnum().ToQuaternion().eulerAngles.z;
						//
						// var Difference =FordsDirectionZ - WantingToFaceZ;
						//
						// var MovingDirection = (Difference - WantingToFaceZ).Angle360ToOrientationEnum();

						var OrientationZ = TargetTransform.rotation.eulerAngles.z;

						float DesiredDirection = 0;
						if (TargetFaceDirectionOverride == OrientationEnum.Default)
						{
							DesiredDirection = WorldCurrentVelocity.normalized.ToOrientationEnum().ToQuaternion()
								.eulerAngles.z;
						}
						else
						{
							DesiredDirection = TargetFaceDirectionOverride.ToQuaternion().eulerAngles.z;
						}


						var CurrentForwards = ForwardsDirection.ToOrientationEnum().ToQuaternion().eulerAngles.z;

						var MovingDirection =
							(OrientationZ + (DesiredDirection - CurrentForwards)).Angle360ToOrientationEnum();

						var Orientation = OrientationZ.Angle360ToOrientationEnum();
						if (Orientation != MovingDirection)
						{
							TargetOrientation = MovingDirection;
						}
					}
					else
					{
						WorldCurrentVelocity = new Vector3(0, 0, 0);
					}
				}
				else
				{
					if (ISMovingX)
					{
						WorldCurrentVelocity = new Vector3(0, 0, 0);
					}

					ISMovingX = false;
				}
			}

			if (ISMovingX == false)
			{
				if (Mathf.Abs(Different.y) > 1)
				{
					if (Different.y > 0)
					{
						WorldCurrentVelocity = new Vector3(0, AITravelSpeed, 0);
					}
					else
					{
						WorldCurrentVelocity = new Vector3(0, -AITravelSpeed, 0);
					}

					if (TargetOrientation == OrientationEnum.Default)
					{
						var OrientationZ = TargetTransform.rotation.eulerAngles.z;

						float DesiredDirection = 0;

						if (TargetFaceDirectionOverride == OrientationEnum.Default)
						{
							DesiredDirection = WorldCurrentVelocity.normalized.ToOrientationEnum().ToQuaternion()
								.eulerAngles.z;
						}
						else
						{
							DesiredDirection = TargetFaceDirectionOverride.ToQuaternion().eulerAngles.z;
						}

						var CurrentForwards = ForwardsDirection.ToOrientationEnum().ToQuaternion().eulerAngles.z;
						var MovingDirection = (OrientationZ + (DesiredDirection - CurrentForwards))
							.Angle360ToOrientationEnum();

						var Orientation = OrientationZ.Angle360ToOrientationEnum();

						if (Orientation != MovingDirection)
						{
							TargetOrientation = MovingDirection;
						}
					}
					else
					{
						WorldCurrentVelocity = new Vector3(0, 0, 0);
					}
				}
				else
				{
					if (ISMovingX == false)
					{
						WorldCurrentVelocity = new Vector3(0, 0, 0);
					}

					ISMovingX = true;
				}
			}
		}
	}

	public void SetMatrixCorners(BetterBounds Bounds)
	{
		if (Debug)
		{
			if (MatrixBoundsGameGizmo.Count == 0)
			{
				foreach (var Corner in Bounds.Corners())
				{
					MatrixBoundsGameGizmo.Add(GameGizmomanager.AddNewSpriteStaticClient(null, Corner, Color.red, X));
				}
			}

			var i = 0;
			foreach (var Corner in Bounds.Corners())
			{
				MatrixBoundsGameGizmo[i].Position = Corner;
				i++;
			}
		}
		else if (MatrixBoundsGameGizmo.Count != 0)
		{
			foreach (var Corner in MatrixBoundsGameGizmo)
			{
				Corner.Remove();
			}

			MatrixBoundsGameGizmo.Clear();
		}
	}

	public void SetAITravelToPosition(Vector3 Position, GameObject ObjectToTravelTo = null)
	{
		travelToWorldPOSOverride = null;
		travelToWorldPOS = Position;
		TravelToObject = ObjectToTravelTo;
		if (Debug)
		{
			if (AIGameGizmoSprite == null)
			{
				AIGameGizmoSprite = GameGizmomanager.AddNewSpriteStaticClient(null, Position, Color.blue, X);
			}

			AIGameGizmoSprite.Position = Position;
		}
		else if (AIGameGizmoSprite != null)
		{
			AIGameGizmoSprite.Remove();
			AIGameGizmoSprite = null;
		}
	}

	public void CheckMatrixRoute()
	{
		if (TargetOrientation != OrientationEnum.Default) return;

		if (IgnorePotentialCollisions) return;

		if (isMovingAroundMatrix)
		{
			var Difference = (CentreOfAIMovementWorld.RoundToInt() - TravelToWorldPOS).magnitude;

			if (Difference < 0.5f)
			{
				MatrixMoveAroundCurrentTargetCorner++;
				if (MatrixMoveAroundCurrentTargetCorner > 3)
				{
					MatrixMoveAroundCurrentTargetCorner = 0;
				}

				var Position = MovingAroundMatrix.Matrix.MatrixInfo.WorldBounds.ExpandAllDirectionsBy(50)
					.GetCorner(MatrixMoveAroundCurrentTargetCorner.Value).RoundToInt();
				Position.z = 0;
				travelToWorldPOSOverride = Position;
			}
			else
			{
				Vector3 currentPosition = CentreOfAIMovementWorld;


				bool Breakout = false;

				if ((PointIsWithinMatrixPerimeterPoint.Value - currentPosition).magnitude < 7)
				{
					Breakout = true;
					IgnoreMatrix = MovingAroundMatrix;
				}


				if (Breakout)
				{
					isMovingAroundMatrix = false;
					travelToWorldPOSOverride = null;
					IgnoreMatrix = MovingAroundMatrix;

					MatrixMoveAroundCurrentTargetCorner = null;
					MovingAroundMatrix = null;
					PointIsWithinMatrixPerimeterPoint = null;
					return;
				}
			}
		}
		else
		{
			var thisBigBound = MetaTileMap.matrix.MatrixInfo.WorldBounds.ExpandAllDirectionsBy(10);

			foreach (var Matrix in MatrixManager.Instance.ActiveMatrices)
			{
				if ((Matrix.Value.WorldBounds.center - CentreOfAIMovementWorld).magnitude > 1000) continue;
				if (Matrix.Value == MatrixManager.Instance.spaceMatrix.MatrixInfo) continue;
				if (Matrix.Value == MetaTileMap.matrix.MatrixInfo) continue;
				if (Matrix.Value == IgnoreMatrix) continue;
				if (Matrix.Value.Matrix.AIShuttleShouldAvoid == false) continue;


				var OtherBigBound = Matrix.Value.WorldBounds.ExpandAllDirectionsBy(10);

				if (thisBigBound.Intersects(OtherBigBound, out var Overlap))
				{
					//SO
					//now How to pick a corner to go to
					OtherBigBound = OtherBigBound.ExpandAllDirectionsBy(40);
					SetMatrixCorners(OtherBigBound);
					Vector3 Closest = OtherBigBound.Minimum;

					float BestDistance = (CentreOfAIMovementWorld - Closest).magnitude;
					MatrixMoveAroundCurrentTargetCorner = 0;
					foreach (var Corner in OtherBigBound.Corners())
					{
						float Distance = (CentreOfAIMovementWorld - Corner).magnitude;

						if (BestDistance > Distance)
						{
							BestDistance = Distance;
							Closest = Corner;
						}

						MatrixMoveAroundCurrentTargetCorner++;
					}

					PointIsWithinMatrixPerimeterPoint = OtherBigBound.GetClosestPerimeterPoint(TravelToWorldPOS);

					MovingAroundMatrix = Matrix.Value;
					var Position = Closest.RoundToInt();
					Position.z = 0;
					travelToWorldPOSOverride = Position;
					isMovingAroundMatrix = true;
				}
			}
		}
	}

	#endregion
}

public enum UIType
{
	Default = 0,
	Nanotrasen = 1,
	Syndicate = 2
};