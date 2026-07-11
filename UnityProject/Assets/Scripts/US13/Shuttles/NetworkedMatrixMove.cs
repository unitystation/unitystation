using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Logs;
using Mirror;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.GameGizmos;
using US13.Core.Transform;
using US13.Managers;
using US13.Managers.MatrixManager;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Objects.Consoles;
using US13.Player;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Behaviours.Objects;
using Util;

namespace US13.Shuttles
{
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


		public List<Thruster> ConnectedThrusters = new();
		public List<ShuttleConnector> ConnectedShuttleConnectors = new();


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
					ForwardsVictor = TargetTransform.localToWorldMatrix.MultiplyVector(Vector3.up);
				}
				else
				{
					OrientationEnum Direction = ShuttleConsuls[0].Rotatable.CurrentDirection.ToOpposite();
					Vector3 VectorDirection = Direction.ToLocalVector3();
					ForwardsVictor = TargetTransform.localToWorldMatrix.MultiplyVector(VectorDirection);
				}


				return ForwardsVictor;
			}
		}

		public float Mass
		{
			get
			{
				if (isServer == false) return SynchronisedMass;

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

		public bool ApplyDrag => SpinneyMode == false || DragSpinneyCoolDown == 0;

		private bool SecretSpinneyMode = false;

		public bool Handbrake;

		public bool SpinneyMode
		{
			get
			{
				bool NewSpinney = WorldCurrentVelocity.magnitude >= SpinneyThreshold;

				if (NewSpinney != SecretSpinneyMode)
				{
					SecretSpinneyMode = NewSpinney;
					if (NewSpinney == false)
					{
						TheReusingSet.Clear();
						TheReusingSetVisited.Clear();
						HashSet<NetworkedMatrixMove> Matrixes =
							GetAllNetworkedMatrixMove(TheReusingSet, true, this, TheReusingSetVisited);
						foreach (NetworkedMatrixMove move in Matrixes)
						{
							move.InternalSetThrusterStrength(Thruster.ThrusterDirectionClassification.Right, 0);
							move.InternalSetThrusterStrength(Thruster.ThrusterDirectionClassification.Left, 0);
						}
					}
				}

				return NewSpinney;
			}
		}

		public Vector3 WorldCurrentVelocity;

		public float RCSDragMovement = 0.4f;
		public float AIRCSDragMovement = 0.1f;
		public float HandbrakeDrag = -0.5f;
		public float CurrentTorque;

		public Vector3 currentLocalPivot;

		public Stopwatch ElapsedTimeSinceLastUpdate = new();

		public ObjectLayer ObjectLayer;

		public MetaTileMap MetaTileMap;

		public SpriteDataSO X;

		public GameGizmoSprite GameGizmoSprite;
		public GameGizmoSprite AIGameGizmoSprite;

		public List<GameGizmoSprite> MatrixBoundsGameGizmo = new();

		public bool Debug = false;

		public float rotationSpeed = 30f; //TODO Range depending on mass of shuttle? Adjust the rotation speed as needed

		public float ShuttleNonSpinneyModeRounding = 30f;

		//NOTE This is not in respect to the Orientation of the Front of the shuttle or the direction of the Shuttle consul, Just to do with the rotation of target transform
		public OrientationEnum CurrentOrientation => TargetTransform.eulerAngles.z.Angle360ToOrientationEnum();

		[SerializeField] private OrientationEnum targetOrientation = OrientationEnum.Default;

		private float lastThrusterStrength = 0;

		//NOTE This is not in respect to the Orientation of the Front of the shuttle or the direction of the Shuttle consul, Just to do with the rotation of target transform
		public OrientationEnum TargetOrientation
		{
			get => targetOrientation;
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
		public event Action<OrientationEnum> OnRotate90;


		public bool RCSModeActive = false; //TODO Check With other stuff

		public PlayerScript playerControllingRcs;

		public bool UpdateHandled = false;

		public HashSet<NetworkedMatrixMove> TheReusingSet = new();
		public HashSet<NetworkedMatrixMove> TheReusingSetVisited = new();


		public List<Thruster> TheReusingConnectedThrusters = new();
		public bool RCSRequiresThrusters = true;
		public List<ShuttleConsole> ShuttleConsuls = new();

		public float AITravelSpeed = 20;
		public float AITravelSpeedFast = 90;


		private Vector3 TravelToWorldPOSMatrixTraversall
		{
			get
			{
				if (TravelToObject != null)
					if (TravelToObject.transform.parent.parent.parent ==
					    transform.parent) // this is so it actually Is moving the object not just Moving forever
						return TravelToObject.transform.position;

				return travelToWorldPOS;
			}
		}


		public Vector3 TravelToWorldPOS
		{
			get
			{
				if (travelToWorldPOSOverride != null) return travelToWorldPOSOverride.Value;

				if (TravelToObject != null)
					if (TravelToObject.transform.parent.parent.parent ==
					    transform.parent) // this is so it actually Is moving the object not just Moving forever
						return TravelToObject.transform.position;

				return travelToWorldPOS;
			}
		}

		public GameObject TravelToObject;

		public Vector3? mtravelToWorldPOSOverride;

		public Vector3? travelToWorldPOSOverride
		{
			set
			{
				mtravelToWorldPOSOverride = value;
				if (Debug &&  mtravelToWorldPOSOverride.HasValue)
				{
					if (AIGameGizmoSprite == null)
					{
						AIGameGizmoSprite = GameGizmomanager.AddNewSpriteStaticClient(null, mtravelToWorldPOSOverride.Value, Color.darkGreen, X);
					}

					if (mtravelToWorldPOSOverride.HasValue)
					{
						AIGameGizmoSprite.Position = mtravelToWorldPOSOverride.Value;
					}
				}
				else if (AIGameGizmoSprite != null)
				{
					AIGameGizmoSprite.Remove();
					AIGameGizmoSprite = null;
				}
			}
			get
			{
				return mtravelToWorldPOSOverride;
			}
		}

		private Vector3 travelToWorldPOS = new(-999999999, -9999999, 0);

		[SyncVar] public bool HasMoveToTarget = false;

		public bool ISMovingX = false;

		public OrientationEnum TargetFaceDirectionOverride;

		public bool FullAISpeed = false;
		public bool isMovingAroundMatrix = false;


		public Vector3? PointIsWithinMatrixPerimeterPoint;

		public int? MatrixMoveAroundCurrentTargetCorner = null;

		public List<MatrixInfo> MovingAroundMatrixs = new();

		public BetterBounds? NavigatingAroundBetterBounds;

		public Vector3? ClosestCashed = null;

		public List<MatrixInfo> IgnoreMatrixs = new();
		public bool IgnorePotentialCollisions;


		public Vector3 CentreOfAIMovementWorld
		{
			get
			{
				if (CentreObjectOverride != null)
					return CentreObjectOverride.transform.position;
				else
					return CentreOfMass.ToWorld(MetaTileMap.matrix);
			}
		}

		public GameObject CentreObjectOverride;

		//Used to tell if rotatable need an update
		private OrientationEnum PreviousDirectionFacing;

		public OrientationEnum previousDirectionFacing => PreviousDirectionFacing;

		public MatrixSync MatrixSync;

		public void Awake()
		{
			if (TargetTransform == null) TargetTransform = transform.parent;

			MatrixSync ??= GetComponent<MatrixSync>();
			if (MatrixSync == null)
			{
				Loggy.Error($"Please remove this {name}");
				Destroy(this);
				return;
			}

			if (TargetTransform == null) TargetTransform = transform.parent;


			MetaTileMap = TargetTransform.GetComponentInChildren<MetaTileMap>();
			ObjectLayer = TargetTransform.GetComponentInChildren<ObjectLayer>();


			UpdateManager.Add(CallbackType.EARLY_UPDATE, UpdateMe);
			ElapsedTimeSinceLastUpdate.Reset();
			ElapsedTimeSinceLastUpdate.Start();
			OnRotate?.Invoke();
			var FacedDirection = ForwardsDirection.ToOrientationEnum();
			if (PreviousDirectionFacing != FacedDirection)
			{
				PreviousDirectionFacing = FacedDirection;
				OnRotate90?.Invoke(TargetTransform.localToWorldMatrix.MultiplyVector(Vector3.up)
					.ToOrientationEnum());
			}

			OnStartMovement += KnockDownPlayers;
			OnRotate += KnockDownPlayers;

			SetGizmoPosition(currentLocalPivot);
		}

		public void OnDestroy()
		{
			OnRotate = null;
			OnRotate90 = null;
			OnStartMovement = null;
			OnStopMovement = null;
			UpdateManager.Remove(CallbackType.EARLY_UPDATE, UpdateMe);
			ElapsedTimeSinceLastUpdate.Stop();
		}

		private void KnockDownPlayers()
		{
			MatrixSync.MatrixMove.NetworkedMatrixMove.KnockdownUnseatedPlayers(
				GameConfigManager.GameConfig.MinimumThrustStrengthToKnockdownPlayers + 1,
				GetAllNetworkedMatrixMove(TheReusingSet, true, this, TheReusingSetVisited));
		}

		public bool IsConnectedToShuttle(NetworkedMatrixMove NetMove)
		{
			TheReusingSet.Clear();
			TheReusingSetVisited.Clear();
			HashSet<NetworkedMatrixMove> Matrixes =
				GetAllNetworkedMatrixMove(TheReusingSet, false, this, TheReusingSetVisited);
			return Matrixes.Contains(NetMove);
		}

		[NaughtyAttributes.Button]
		public void StartUpdating()
		{
			UpdateManager.Add(CallbackType.EARLY_UPDATE, UpdateMe);
			ElapsedTimeSinceLastUpdate.Reset();
			ElapsedTimeSinceLastUpdate.Start();
		}

		[NaughtyAttributes.Button]
		public void StopUpdating()
		{
			UpdateManager.Remove(CallbackType.EARLY_UPDATE, UpdateMe);
			ElapsedTimeSinceLastUpdate.Stop();
		}

		public void SetGizmoPosition(Vector3 Position)
		{
			if (Debug)
			{
				if (GameGizmoSprite == null)
					GameGizmoSprite =
						GameGizmomanager.AddNewSpriteStaticClient(ObjectLayer.gameObject, Position, Color.green, X);

				GameGizmoSprite.Position = Position;
			}
			else if (GameGizmoSprite != null)
			{
				GameGizmoSprite.Remove();
				GameGizmoSprite = null;
			}
		}


		public HashSet<NetworkedMatrixMove> GetAllNetworkedMatrixMove(HashSet<NetworkedMatrixMove> ToUse,
			bool RespectConsuls, NetworkedMatrixMove OriginMove, HashSet<NetworkedMatrixMove> Visited)
		{
			if (Visited.Contains(this)) return ToUse;

			var AddThisMatrix = true;

			if (OriginMove != this)
				if (RespectConsuls)
					foreach (ShuttleConsole Consul in ShuttleConsuls)
						if (Consul.EngineSupport == false)
							AddThisMatrix = false;


			if (AddThisMatrix) ToUse.Add(this);

			Visited.Add(this);

			foreach (ShuttleConnector ConnectedShuttleConnector in ConnectedShuttleConnectors)
			{
				if (ConnectedShuttleConnector.ConnectedToConnector?.RelatedMove?.NetworkedMatrixMove == null) continue;
				ConnectedShuttleConnector.ConnectedToConnector.RelatedMove.NetworkedMatrixMove
					.GetAllNetworkedMatrixMove(ToUse, RespectConsuls, OriginMove, Visited);
			}

			return ToUse;
		}

		public Vector3 GetAllCentreOfMass(HashSet<NetworkedMatrixMove> ToUseMatrixMove)
		{
			float TotalMass = 0;
			Vector3 Positions = Vector3.zero;
			foreach (NetworkedMatrixMove MatrixMove in ToUseMatrixMove)
			{
				Positions += MatrixMove.CentreOfMass.ToWorld(MatrixMove.MetaTileMap.matrix) * MatrixMove.Mass;
				TotalMass += MatrixMove.Mass;
			}

			return Positions / TotalMass;
		}

		public float GetAllMass(HashSet<NetworkedMatrixMove> ToUseMatrixMove)
		{
			float mass = 0;
			foreach (NetworkedMatrixMove MatrixMove in ToUseMatrixMove) mass += MatrixMove.Mass;

			return mass;
		}

		public List<Thruster> GetThrusters(HashSet<NetworkedMatrixMove> ToUseMatrixMove, List<Thruster> thrusters)
		{
			foreach (NetworkedMatrixMove MatrixMove in ToUseMatrixMove)
				thrusters.AddRange(MatrixMove.ConnectedThrusters);

			return thrusters;
		}

		public void TurnOffAllThrusters()
		{
			foreach (Thruster Thruster in ConnectedThrusters) Thruster.SetTargetMolesUsed(Thruster.MaxMolesUseda * 0);
		}

		private float SignedAngleZ(Vector3 a, Vector3 b)
		{
			// Drop Z component — use only XY
			Vector2 a2 = new Vector2(a.x, a.y).normalized;
			Vector2 b2 = new Vector2(b.x, b.y).normalized;

			float angle = Mathf.Atan2(b2.y, b2.x) - Mathf.Atan2(a2.y, a2.x);
			angle = Mathf.Rad2Deg * angle;

			// Normalize to -180..180
			if (angle > 180) angle -= 360;
			if (angle < -180) angle += 360;

			return angle;
		}

		private void InternalSetThrusterStrength(Thruster.ThrusterDirectionClassification Direction, float Multiplier)
		{
			if (SpinneyMode || Direction == Thruster.ThrusterDirectionClassification.Up ||
			    Direction == Thruster.ThrusterDirectionClassification.Down)
			{
				foreach (Thruster Thruster in ConnectedThrusters)
					if (Thruster.ThisThrusterDirectionClassification == Direction)
						Thruster.SetTargetMolesUsed(Thruster.MaxMolesUseda * Multiplier);
			}
			else
			{
				foreach (Thruster Thruster in ConnectedThrusters)
					if (Thruster.ThisThrusterDirectionClassification != Thruster.ThrusterDirectionClassification.Up &&
					    Thruster.ThisThrusterDirectionClassification != Thruster.ThrusterDirectionClassification.Down)
						Thruster.SetTargetMolesUsed(Thruster.MaxMolesUseda * 0);


				if (Multiplier < 0.9f) return;
				//var CurrentOrientation = TargetOrientation;

				// 1 — Calculate signed angle (ALWAYS CORRECT, handles 0–360 wrap)
				float currentZ = TargetTransform.eulerAngles.z;

				var targetZ = (float)TargetOrientation.To360Z();
				if (TargetOrientation == OrientationEnum.Default) targetZ = (float)TargetTransform.eulerAngles.z;


				float zdiff = Mathf.DeltaAngle(currentZ, targetZ);

				if (zdiff > 10)
				{
					if (Direction == Thruster.ThrusterDirectionClassification.Left) return;
				}
				else if (zdiff < -10)
				{
					if (Direction == Thruster.ThrusterDirectionClassification.Right) return;
				}

				OrientationEnum CurrentOrientation = currentZ.Angle360ToOrientationEnum();

				if (TargetOrientation != OrientationEnum.Default)
					// 2 — Determine current orientation enum
					CurrentOrientation = TargetOrientation;

				TargetOrientation = GetNextOrientation(CurrentOrientation, Direction);
			}
		}

		private OrientationEnum GetNextOrientation(OrientationEnum current,
			Thruster.ThrusterDirectionClassification dir)
		{
			if (dir == Thruster.ThrusterDirectionClassification.Right)
				switch (current)
				{
					case OrientationEnum.Up_By0: return OrientationEnum.Right_By270;
					case OrientationEnum.Right_By270: return OrientationEnum.Down_By180;
					case OrientationEnum.Down_By180: return OrientationEnum.Left_By90;
					default: return OrientationEnum.Up_By0;
				}
			else // LEFT
				switch (current)
				{
					case OrientationEnum.Up_By0: return OrientationEnum.Left_By90;
					case OrientationEnum.Left_By90: return OrientationEnum.Down_By180;
					case OrientationEnum.Down_By180: return OrientationEnum.Right_By270;
					default: return OrientationEnum.Up_By0;
				}
		}

		public void SetThrusterStrength(Thruster.ThrusterDirectionClassification Direction, float Multiplier,
			bool RespectConsuls)
		{
			TheReusingSet.Clear();
			TheReusingSetVisited.Clear();
			HashSet<NetworkedMatrixMove> Matrixes =
				GetAllNetworkedMatrixMove(TheReusingSet, RespectConsuls, this, TheReusingSetVisited);
			foreach (NetworkedMatrixMove move in Matrixes) move.InternalSetThrusterStrength(Direction, Multiplier);

			if (Multiplier == 0)
				lastThrusterStrength = 0;
			else
				KnockdownUnseatedPlayers(Multiplier, Matrixes);
		}

		public void KnockdownUnseatedPlayers(float multiplier, HashSet<NetworkedMatrixMove> matrixMoves)
		{
			if (multiplier < GameConfigManager.GameConfig.MinimumThrustStrengthToKnockdownPlayers) return;
			if (multiplier < lastThrusterStrength) return;

			foreach (NetworkedMatrixMove networkedMatrixMove in matrixMoves)
			foreach (RegisterPlayer mob in networkedMatrixMove.MetaTileMap.matrix.PresentPlayers)
			{
				if (mob.IsLayingDown || mob.PlayerScript.ObjectPhysics.IsBuckled) return;
				mob.ServerStun(Mathf.Clamp(multiplier * 2, 1, 5), checkForArmor: false);
				Chat.AddExamineMsg(mob.PlayerScript.gameObject,
					"A sudden jolt from below throws you off your feet!");
			}

			lastThrusterStrength = multiplier;
		}

		public void AddConnector(ShuttleConnector ShuttleConnector)
		{
			if (ConnectedShuttleConnectors.Contains(ShuttleConnector) == false)
				ConnectedShuttleConnectors.Add(ShuttleConnector);
		}


		public void RemoveConnector(ShuttleConnector ShuttleConnector)
		{
			if (ConnectedShuttleConnectors.Contains(ShuttleConnector))
				ConnectedShuttleConnectors.Remove(ShuttleConnector);
		}


		public void AddThruster(Thruster Thruster)
		{
			if (ConnectedThrusters.Contains(Thruster) == false) ConnectedThrusters.Add(Thruster);
		}


		public void RemoveThruster(Thruster Thruster)
		{
			if (ConnectedThrusters.Contains(Thruster)) ConnectedThrusters.Remove(Thruster);
		}

		public void RcsMove(Orientation GlobalMoveDirection)
		{
			RcsMove(GlobalMoveDirection.LocalVector.ToOrientationEnum());
		}

		public void RcsMove(OrientationEnum GlobalMoveDirection, bool ISAImove = false)
		{
			if (ISAImove)
			{
				if (WorldCurrentVelocity.magnitude > AIRCSDragMovement ||
				    Mathf.Abs(CurrentTorque) > AIRCSDragMovement ||
				    TargetOrientation != OrientationEnum.Default)
					return;
			}
			else
			{
				if (WorldCurrentVelocity.magnitude > RCSDragMovement || Mathf.Abs(CurrentTorque) > RCSDragMovement ||
				    TargetOrientation != OrientationEnum.Default)
					return;
			}


			var HasThrusterDirection = false;
			TheReusingSet.Clear();
			TheReusingSetVisited.Clear();
			HashSet<NetworkedMatrixMove> Matrixes =
				GetAllNetworkedMatrixMove(TheReusingSet, true, this, TheReusingSetVisited);
			List<Thruster> Thrusters = GetThrusters(Matrixes, TheReusingConnectedThrusters);
			if (RCSRequiresThrusters)
			{
				foreach (Thruster Thruster in Thrusters)
					if (((Vector3)Thruster.Rotatable.WorldDirection).ToOrientationEnum() ==
					    GlobalMoveDirection.ToOpposite())
					{
						HasThrusterDirection = true;
						break;
					}
			}
			else
			{
				HasThrusterDirection = true;
			}


			if (HasThrusterDirection)
				foreach (NetworkedMatrixMove Matrix in Matrixes)
					Matrix.WorldCurrentVelocity += GlobalMoveDirection.ToLocalVector3();
		}


		public void UpdateMe()
		{
			//Updates needed for
			//humm, active thrusters, MatrixMove.ConnectedThrusters
			//Current momentum -> WorldCurrentVelocity and CurrentTorque
			//has AI -> HasMoveToTarget
			//if Shuttle connectors then find the biggest one and update that, Connected skip this test? yeah

			UpdateSyncVars();

			if (ConnectedShuttleConnectors.Count > 0)
			{
				UpdateLoop();
				return;
			}


			if (ConnectedThrusters.Count == 0 &&
			    WorldCurrentVelocity.magnitude == 0 &&
			    CurrentTorque == 0 &&
			    HasMoveToTarget == false &&
			    ServerSyncDoesntMatch() == false &&
			    TargetOrientation == OrientationEnum.Default &&
			    TargetTransform.position == TargetTransform.position.RoundToIntFloat())
			{
				ElapsedTimeSinceLastUpdate.Stop();
				ElapsedTimeSinceLastUpdate.Reset(); //TODO Editor pausing??
				return;
			}


			UpdateLoop();
		}


		public void UpdateLoop(bool DoneMasterAlready = false)
		{
			ElapsedTimeSinceLastUpdate.Stop();
			var DeltaTimeSeconds = (float)ElapsedTimeSinceLastUpdate.Elapsed.TotalSeconds;
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
			TheReusingSetVisited.Clear();
			HashSet<NetworkedMatrixMove> Matrixes =
				GetAllNetworkedMatrixMove(TheReusingSet, false, this, TheReusingSetVisited);


			if (DoneMasterAlready == false)
			{
				NetworkedMatrixMove ControllingMatrix = null;
				var hasActiveConsulToo = false;


				foreach (NetworkedMatrixMove Matrix in Matrixes) //Find the biggest  in control matrix
				{
					if (ControllingMatrix == null)
					{
						ControllingMatrix = Matrix;
						foreach (ShuttleConsole Consul in Matrix.ShuttleConsuls)
							if (Consul.EngineOn)
								hasActiveConsulToo = true;
					}


					if (hasActiveConsulToo)
					{
						if (Matrix.Mass > ControllingMatrix.Mass)
							foreach (ShuttleConsole Consul in Matrix.ShuttleConsuls)
								if (Consul.EngineOn)
									ControllingMatrix = Matrix;
					}
					else
					{
						if (Matrix.Mass > ControllingMatrix.Mass)
							ControllingMatrix = Matrix;
						else
							foreach (ShuttleConsole Consul in Matrix.ShuttleConsuls)
								if (Consul.EngineOn)
								{
									hasActiveConsulToo = true;
									ControllingMatrix = Matrix;
								}
					}
				}

				if (ControllingMatrix != this)
				{
					//Basically quit out since this matrix here should be the one doing the updates
					ControllingMatrix.UpdateLoop(true); //note Technically recursive And a bit messy
					//But it's better than each matrix going through the update and then finally reaching the biggest one
					return;
				}
			}


			TheReusingConnectedThrusters.Clear();
			List<Thruster> Thrusters = GetThrusters(Matrixes, TheReusingConnectedThrusters);
			float AllMass = GetAllMass(Matrixes);
			Vector3 WoldCentreOfMass = GetAllCentreOfMass(Matrixes);

			if (AllMass == 0) return;

			bool AllRCSModeActive = Matrixes.Any(x => x.RCSModeActive);
			Vector3 WorldPivot = Vector2.zero;


			float sumThrust = 0;

			if (SpinneyMode)
			{
				foreach (Thruster Thruster in Thrusters)
				{
					float ThrusterMagnitude = Thruster.WorldThrustDirectionAndMagnitude.magnitude;

					if (Mathf.Abs(ThrusterMagnitude) > 0 && Mathf.Abs(sumThrust + ThrusterMagnitude) > float.Epsilon)
					{
						float ScalerThrusterMagnitude =
							ThrusterMagnitude
							/
							(ThrusterMagnitude + sumThrust);

						WorldPivot = Vector2.Lerp(WorldPivot, Thruster.transform.position, ScalerThrusterMagnitude);
					}

					sumThrust += ThrusterMagnitude;
				}


				float MassMagnitude = AllMass; //Because your mass doesn't like being moved and counterbalances it

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
					float torque = Vector3.Cross(WorldPivot - (Vector3)Thruster.transform.position,
						(Vector3)Thruster.WorldThrustDirectionAndMagnitude).z;
					sumTorques += torque;
				}


				sumTorques *= DeltaTimeSeconds;

				if (Mathf.Abs(sumTorques) > 0 && Mathf.Abs(CurrentTorque + sumTorques) > float.Epsilon)
				{
					float ScalerSumTorques =
						sumTorques
						/
						(sumTorques + CurrentTorque);


					currentLocalPivot = Vector2.Lerp(currentLocalPivot, WorldPivot.ToLocal(MetaTileMap.matrix),
						ScalerSumTorques);
				}

				Vector3 PivotDifference = WoldCentreOfMass - currentLocalPivot.ToWorld(MetaTileMap.matrix);
				float MomentumStrength = PivotDifference.magnitude * CurrentTorque * DeltaTimeSeconds;

				CurrentTorque += sumTorques / AllMass;

				if (PivotDifference.magnitude > 0 && MomentumStrength > 0)
				{
					float TorquesDifference = sumTorques / AllMass;

					float ScalerMomentumStrength =
						MomentumStrength
						/
						(MomentumStrength + TorquesDifference);

					currentLocalPivot = Vector2.Lerp(currentLocalPivot, WoldCentreOfMass.ToLocal(MetaTileMap.matrix),
						ScalerMomentumStrength);


					//TODO Balance the WorldCurrentVelocity added Because it doesn't seem to be strong enough whenTwo shuttle split apart meybe 2x Faster?

					WorldCurrentVelocity += new Vector3(-PivotDifference.y, PivotDifference.x, 0).normalized *
					                        (ScalerMomentumStrength * (MomentumStrength / AllMass));
				}
			}
			else
			{
				currentLocalPivot = WoldCentreOfMass.ToLocal(MetaTileMap.matrix);
				if (HasMoveToTarget) currentLocalPivot = CentreOfAIMovementWorld.ToLocal(MetaTileMap.matrix);
			}

			Vector3 OverallthrustDirection = Vector3.zero;

			if (MoveCoolDown == 0)
			{
				foreach (Thruster thruster in Thrusters)
				{
					// Calculate the vector from center of mass to force position
					Vector3 r = (Vector3)thruster.transform.position - WoldCentreOfMass;

					// Prevent issues with extremely small r values
					float mag = r.magnitude;
					if (mag < 1e-6)
						// Use a small threshold to check near-zero values
						r = new Vector3(1, 0, 0); // Assign a reasonable default direction (arbitrary but consistent)
					else if (mag < 1) r = r.normalized; // Normalize if it's not zero

					// Calculate the component of force along the line connecting force position to center of mass
					Vector3 forceComponent =
						Vector3.Dot(thruster.WorldThrustDirectionAndMagnitude, r) / r.sqrMagnitude * r;
					forceComponent.z = 0;
					OverallthrustDirection -= forceComponent;
				}

				WorldCurrentVelocity += OverallthrustDirection * DeltaTimeSeconds / AllMass;
			}
			else
			{
				WorldCurrentVelocity *= 0;
				MoveCoolDown -= DeltaTimeSeconds;
				if (MoveCoolDown < 0) MoveCoolDown = 0;
			}

			var Handbrake = false;

			foreach (NetworkedMatrixMove Matrix in Matrixes)
				if (Matrix.Handbrake)
				{
					Handbrake = true;
					break;
				}

			if (Handbrake && WorldCurrentVelocity.magnitude > SpinneyThreshold - 1)
				WorldCurrentVelocity = WorldCurrentVelocity -
				                       (WorldCurrentVelocity.normalized * (SpinneyThreshold - 1) -
				                        WorldCurrentVelocity) * HandbrakeDrag;


			var DoUpdateLocalPosition = false;

			//HasMoveToTarget

			DoUpdateLocalPosition = DragCalculations(DeltaTimeSeconds, AllRCSModeActive,
				RCSModeActive || OverallthrustDirection.magnitude < 0.3);
			AligneToTiles(DeltaTimeSeconds, Matrixes);

			SetTransformPosition(TargetTransform.position + (Vector3)
				((Vector3)WorldCurrentVelocity * DeltaTimeSeconds), false, Matrixes);

			if (DragSpinneyCoolDown > 0)
			{
				DragSpinneyCoolDown -= DeltaTimeSeconds;
				if (DragSpinneyCoolDown < 0) DragSpinneyCoolDown = 0;
			}

			if (SpinneyMode)
			{
				TargetOrientation = OrientationEnum.Default;
				Vector3 KeepMomentum =
					TargetTransform.worldToLocalMatrix.MultiplyVector(WorldCurrentVelocity *
					                                                  SpinneyTurnVelocityBent);
				WorldCurrentVelocity -= SpinneyTurnVelocityBent * WorldCurrentVelocity;
				TransformUpdateRotate(ObjectLayer.transform.TransformPoint(currentLocalPivot),
					CurrentTorque * DeltaTimeSeconds, false, Matrixes);
				WorldCurrentVelocity += TargetTransform.localToWorldMatrix.MultiplyVector(KeepMomentum);
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
				int direction = angleDifference < 0 ? -1 : 1;


				Vector3 KeepMomentum = TargetTransform.worldToLocalMatrix.MultiplyVector(WorldCurrentVelocity);


				bool UpdateConversion = WorldCurrentVelocity.sqrMagnitude == 0;

				// If the difference is small, snap to the target rotation
				if (Mathf.Abs(angleDifference) < step)
				{
					TransformUpdateRotate(ObjectLayer.transform.TransformPoint(currentLocalPivot).RoundToInt(),
						angleDifference, UpdateConversion, Matrixes);
					TargetOrientation = OrientationEnum.Default;
				}
				else
				{
					// Rotate the object around the pivot using transform.RotateAround
					TransformUpdateRotate(ObjectLayer.transform.TransformPoint(currentLocalPivot).RoundToInt(),
						direction * rotationSpeed * DeltaTimeSeconds, UpdateConversion, Matrixes);
				}

				if (HasMoveToTarget == false)
					WorldCurrentVelocity = TargetTransform.localToWorldMatrix.MultiplyVector(KeepMomentum);
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
				int direction = angleDifference < 0 ? -1 : 1;

				// If the difference is small, snap to the target rotation
				if (Mathf.Abs(angleDifference) < step)
					TransformSetEuler(new Vector3(0, 0, targetRotation), false, Matrixes);
				else
					// Rotate the object around the pivot using transform.RotateAround
					TransformUpdateRotate(ObjectLayer.transform.TransformPoint(currentLocalPivot),
						direction * ShuttleNonSpinneyModeRounding * DeltaTimeSeconds, false, Matrixes);
			}

			if (DoUpdateLocalPosition)
				foreach (NetworkedMatrixMove Matrixe in Matrixes)
					Matrixe.UpdateLocalAndWorldConversion();

			if (WorldCurrentVelocity.magnitude > 0.001f || Mathf.Abs(CurrentTorque) > 0.001f ||
			    TargetOrientation != OrientationEnum.Default)
			{
				if (IsMoving == false)
					foreach (NetworkedMatrixMove Matrixe in Matrixes)
					{
						Matrixe.OnStartMovement?.Invoke();
						Matrixe.IsMoving = true;
					}

				IsMoving = true;
			}
			else
			{
				if (IsMoving == true)
					foreach (NetworkedMatrixMove Matrixe in Matrixes.ToList())
					{
						Matrixe.OnStopMovement?.Invoke();
						Matrixe.IsMoving = false;
					}

				IsMoving = false;
			}

			foreach (NetworkedMatrixMove Matrixe in Matrixes)
			{
				Matrixe.WorldCurrentVelocity = WorldCurrentVelocity;
				Matrixe.CurrentTorque = CurrentTorque;
				Matrixe.UpdateSyncVars();
				Matrixe.SetGizmoPosition(currentLocalPivot.ToWorld(MetaTileMap.matrix)
					.ToLocal(Matrixe.MetaTileMap.matrix));

				if (Matrixe != this) Matrixe.UpdateHandled = true;
			}
		}

		public void AligneToTiles(float DeltaTimeSeconds, HashSet<NetworkedMatrixMove> Matrixes)
		{
			if (SpinneyMode == false && TargetOrientation == OrientationEnum.Default)
			{
				if (Mathf.Abs(WorldCurrentVelocity.x) < 0.50f)
				{
					Vector3 Position = TargetTransform.position;
					if (WorldCurrentVelocity.x > 0f)
						Position.x += 0.45f;
					else
						Position.x -= 0.45f;

					Position.x = Mathf.Round(Position.x);

					SetTransformPosition(
						Vector3.Lerp(TargetTransform.position, Position, 2 * TileAlignmentSpeed * DeltaTimeSeconds),
						false,
						Matrixes);
				}

				if (Mathf.Abs(WorldCurrentVelocity.y) < 0.50f)
				{
					Vector3 Position = TargetTransform.position;
					if (WorldCurrentVelocity.y > 0)
						Position.y += 0.45f;
					else
						Position.y -= 0.45f;

					Position.y = Mathf.Round(Position.y);

					SetTransformPosition(
						Vector3.Lerp(TargetTransform.position, Position, 2 * TileAlignmentSpeed * DeltaTimeSeconds),
						false,
						Matrixes);
				}
			}
		}

		public bool DragCalculations(float DeltaTimeSeconds, bool AllRCSModeActive, bool SlowDrag)
		{
			var DoUpdateLocalPosition = false;
			bool AINoDrag = HasMoveToTarget && WorldCurrentVelocity.magnitude > 2;

			if (WorldCurrentVelocity.magnitude > 0 && ApplyDrag && AINoDrag == false && SlowDrag)
			{
				DoUpdateLocalPosition = true;
				WorldCurrentVelocity = ApplyDragTo(WorldCurrentVelocity, Drag, DeltaTimeSeconds);
			}

			if (WorldCurrentVelocity.magnitude > 0 && WorldCurrentVelocity.magnitude < LowSpeedDragThreshold &&
			    AINoDrag == false && SlowDrag)
			{
				DoUpdateLocalPosition = true;
				WorldCurrentVelocity = ApplyDragTo(WorldCurrentVelocity, LowSpeedDrag, DeltaTimeSeconds);
			}


			if (Mathf.Abs(WorldCurrentVelocity.x) > HighSpeedDragMinimumThreshold && ApplyDrag && AINoDrag == false)
			{
				float MomentumDifference = Mathf.Abs(WorldCurrentVelocity.x) - HighSpeedDragMinimumThreshold;
				float DragMultiplier = MomentumDifference / (HighSpeedDrag100Threshold - HighSpeedDragMinimumThreshold);
				WorldCurrentVelocity.x =
					ApplyDragTo(WorldCurrentVelocity.x, HighSpeedDrag * DragMultiplier, DeltaTimeSeconds);
			}


			if (Mathf.Abs(WorldCurrentVelocity.y) > HighSpeedDragMinimumThreshold && ApplyDrag && AINoDrag == false)
			{
				float MomentumDifference = Mathf.Abs(WorldCurrentVelocity.y) - HighSpeedDragMinimumThreshold;
				float DragMultiplier = MomentumDifference / (HighSpeedDrag100Threshold - HighSpeedDragMinimumThreshold);
				WorldCurrentVelocity.y =
					ApplyDragTo(WorldCurrentVelocity.y, HighSpeedDrag * DragMultiplier, DeltaTimeSeconds);
			}

			if (Mathf.Abs(CurrentTorque) > 0 && ApplyDrag && AINoDrag == false)
			{
				DoUpdateLocalPosition = true;
				CurrentTorque = ApplyDragTo(CurrentTorque, DragTorque, DeltaTimeSeconds);
			}

			//No drifting drag at slow Speed
			if (SpinneyMode == false && AllRCSModeActive == false &&
			    TargetFaceDirectionOverride == OrientationEnum.Default && AINoDrag == false)
			{
				float dotProduct = Vector3.Dot(WorldCurrentVelocity.normalized, ForwardsDirection.normalized);
				WorldCurrentVelocity = ForwardsDirection * (dotProduct * WorldCurrentVelocity.magnitude);
			}

			return DoUpdateLocalPosition;
		}

		public Vector3 ApplyDragTo(Vector3 CurrentMomentum, float Drag, float deltaTimeSeconds)
		{
			float Multiplier = Drag * deltaTimeSeconds;
			if (Drag * deltaTimeSeconds > 1) Multiplier = 1;

			CurrentMomentum -= CurrentMomentum * Multiplier;
			return CurrentMomentum;
		}

		public float ApplyDragTo(float CurrentMomentum, float Drag, float deltaTimeSeconds)
		{
			float Multiplier = Drag * deltaTimeSeconds;
			if (Drag * deltaTimeSeconds > 1) Multiplier = 1;

			CurrentMomentum -= CurrentMomentum * Multiplier;
			return CurrentMomentum;
		}

		public void UpdateLocalAndWorldConversion()
		{
			MetaTileMap.UpdateTransformMatrix();
		}

		public bool ServerSyncDoesntMatch()
		{
			if (isServer == false) return false;
			if (SynchronisedSpin != CurrentTorque) return true;
			if (SynchronisedMass != Mass) return true;
			if (SynchronisedVelocity != WorldCurrentVelocity) return true;
			if (SynchronisedPivotPoint != currentLocalPivot) return true;
			if (SynchronisedPosition != TargetTransform.position) return true;
			if (SynchronisedRotation != TargetTransform.rotation.eulerAngles) return true;
			return false;
		}


		public void UpdateSyncVars()
		{
			if (isServer == false) return;
			if (SynchronisedSpin != CurrentTorque) SynchroniseSpin(SynchronisedSpin, CurrentTorque);

			if (SynchronisedMass != Mass) SynchroniseMass(SynchronisedMass, Mass);

			if (SynchronisedVelocity != WorldCurrentVelocity)
				SynchroniseVelocity(SynchronisedVelocity, WorldCurrentVelocity);

			if (SynchronisedPivotPoint != currentLocalPivot)
				SynchronisePivotPoint(SynchronisedPivotPoint, currentLocalPivot);


			if (SynchronisedPosition != TargetTransform.position)
				SynchronisePosition(SynchronisedPosition, TargetTransform.position);

			if (SynchronisedRotation != TargetTransform.rotation.eulerAngles)
				SynchroniseRotation(SynchronisedRotation, TargetTransform.rotation.eulerAngles);
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
				foreach (NetworkedMatrixMove matrix in Matrixs)
				{
					if (matrix == this) continue;
					Vector3 Offset = TargetTransform.position - matrix.TargetTransform.position;
					matrix.SetTransformPosition(NewPosition - Offset, UpdateConversion);
				}


			TargetTransform.position = NewPosition;
			if (UpdateConversion) UpdateLocalAndWorldConversion();
		}

		public void TransformUpdateRotate(Vector3 RotateAround, float By, bool UpdateConversion = true,
			HashSet<NetworkedMatrixMove> Matrixs = null)
		{
			if (Matrixs != null)
				foreach (NetworkedMatrixMove matrix in Matrixs)
				{
					if (matrix == this) continue;
					matrix.TransformUpdateRotate(RotateAround, By, UpdateConversion);
				}

			var axis = new Vector3(0, 0, 1);
			TargetTransform.RotateAround(RotateAround, axis, By);

			if (Mathf.Abs(By) > 0) OnRotate?.Invoke();

			var facedDirection = ForwardsDirection.ToOrientationEnum();
			if (PreviousDirectionFacing != facedDirection)
			{
				PreviousDirectionFacing = facedDirection;
				OnRotate90?.Invoke(TargetTransform.localToWorldMatrix.MultiplyVector(Vector3.up)
					.ToOrientationEnum());
			}

			if (UpdateConversion) UpdateLocalAndWorldConversion();
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
				foreach (NetworkedMatrixMove matrix in Matrixs)
				{
					if (matrix == this) continue;
					Quaternion Offset = Quaternion.Inverse(TargetTransform.rotation) * matrix.TargetTransform.rotation;
					matrix.TransformSetQuaternion(SetTO * Offset, UpdateConversion);
				}

			float difference = TargetTransform.rotation.eulerAngles.z - SetTO.eulerAngles.z;

			TargetTransform.rotation = SetTO;

			if (difference != 0) OnRotate?.Invoke();

			var FacedDirection = ForwardsDirection.ToOrientationEnum();
			if (PreviousDirectionFacing != FacedDirection)
			{
				PreviousDirectionFacing = FacedDirection;
				OnRotate90?.Invoke(TargetTransform.localToWorldMatrix.MultiplyVector(Vector3.up)
					.ToOrientationEnum());
			}


			if (UpdateConversion) UpdateLocalAndWorldConversion();
		}


		#region ShuttleCollision

		public void CheckCollisions()
		{
			if (SpinneyMode == false) return;

			if (Safety == false) return;
			//Basically the air movement
			BetterBounds thisBigBound = MetaTileMap.matrix.MatrixInfo.WorldBounds.ExpandAllDirectionsBy(10);

			foreach (KeyValuePair<int, MatrixInfo> Matrix in MatrixManager.Instance.ActiveMatrices)
			{
				if ((Matrix.Value.WorldBounds.center - CentreOfAIMovementWorld).magnitude > 1000) continue;
				if (Matrix.Value == MatrixManager.Instance.spaceMatrix.MatrixInfo) continue;
				if (Matrix.Value == MetaTileMap.matrix.MatrixInfo) continue;
				if (TheReusingSet.Contains(Matrix.Value.MatrixMove.NetworkedMatrixMove)) continue;

				BetterBounds OtherBigBound = Matrix.Value.WorldBounds.ExpandAllDirectionsBy(10);

				if (thisBigBound.Intersects(OtherBigBound, out BetterBounds Overlap))
					WorldCurrentVelocity = WorldCurrentVelocity.normalized * (SpinneyThreshold - 1);
			}
		}

		#endregion

		#region AIMOVE

		/// <summary>
		/// Monitors the autopilot state and updates velocity, RCS mode and target orientation.
		/// This method is the high-level entry point; detailed behaviors are delegated to smaller helpers.
		/// </summary>
		public void MonitorAutopilot()
		{
			if (HasMoveToTarget == false) return;
			if (CustomNetworkManager.IsServer == false) return;

			CheckMatrixRoute();

			// Calculate world difference to target (rounded center)
			Vector3 Different = TravelToWorldPOS - CentreOfAIMovementWorld.RoundToInt();

			// If very close, use RCS for fine movement and stop high-speed AI thrust
			if (Mathf.Abs(Different.x) < 1.5f && Mathf.Abs(Different.y) < 1.5f)
			{
				HandleCloseProximity(Different);
			}
			else
			{
				HandleLongDistanceMovement(Different);
			}
		}

		/// <summary>
		/// Handle very close distances: disable full speed, enable RCS and issue small RCS corrections.
		/// </summary>
		private void HandleCloseProximity(Vector3 Different)
		{
			if (FullAISpeed)
			{
				WorldCurrentVelocity = Vector3.zero;
				FullAISpeed = false;
			}

			RCSModeActive = true;

			if (Different.magnitude > 0.5f)
			{
				RcsMove(Different.normalized.ToOrientationEnum(), true);
			}
		}

		/// <summary>
		/// Handle longer-distance movement by choosing X or Y axis travel and updating velocity/orientation.
		/// </summary>
		private void HandleLongDistanceMovement(Vector3 Different)
		{
			FullAISpeed = true;
			RCSModeActive = false;

			if (ISMovingX)
			{
				HandleMovementX(Different);
			}

			if (ISMovingX == false)
			{
				HandleMovementY(Different);
			}
		}

		/// <summary>
		/// Compute velocity and orientation adjustments when moving along the X axis.
		/// </summary>
		private void HandleMovementX(Vector3 Different)
		{
			if (Mathf.Abs(Different.x) > 1)
			{
				bool fast = Mathf.Abs(Different.x) > 100;

				var SpeedMultiplier = 1f;
				if (Different.x > 30) SpeedMultiplier = Mathf.Max(Different.y / 30, 0.3f);

				float TravelSpeed = AITravelSpeed * SpeedMultiplier;
				if (fast) TravelSpeed = AITravelSpeedFast;

				WorldCurrentVelocity = Different.x > 0 ? new Vector3(TravelSpeed, 0, 0) : new Vector3(-TravelSpeed, 0, 0);

				if (TargetOrientation == OrientationEnum.Default)
				{
					UpdateOrientationForVelocity();
				}
				else
				{
					WorldCurrentVelocity = new Vector3(0, 0, 0);
				}
			}
			else
			{
				if (ISMovingX) WorldCurrentVelocity = new Vector3(0, 0, 0);

				ISMovingX = false;
			}
		}

		/// <summary>
		/// Compute velocity and orientation adjustments when moving along the Y axis.
		/// </summary>
		private void HandleMovementY(Vector3 Different)
		{
			if (Mathf.Abs(Different.y) > 1)
			{
				bool fast = Mathf.Abs(Different.y) > 100;
				var SpeedMultiplier = 1f;
				if (Different.y > 30) SpeedMultiplier = Mathf.Max(Different.y / 30, 0.3f);

				float TravelSpeed = AITravelSpeed * SpeedMultiplier;
				if (fast) TravelSpeed = AITravelSpeedFast;

				WorldCurrentVelocity = Different.y > 0 ? new Vector3(0, TravelSpeed, 0) : new Vector3(0, -TravelSpeed, 0);

				if (TargetOrientation == OrientationEnum.Default)
				{
					UpdateOrientationForVelocity();
				}
				else
				{
					WorldCurrentVelocity = new Vector3(0, 0, 0);
				}
			}
			else
			{
				if (ISMovingX == false) WorldCurrentVelocity = new Vector3(0, 0, 0);

				ISMovingX = true;
			}
		}

		/// <summary>
		/// Update TargetOrientation based on current WorldCurrentVelocity and configured face overrides.
		/// Shared by both X and Y movement handlers.
		/// </summary>
		private void UpdateOrientationForVelocity()
		{
			float OrientationZ = TargetTransform.rotation.eulerAngles.z;

			float DesiredDirection = 0;
			if (TargetFaceDirectionOverride == OrientationEnum.Default)
				DesiredDirection = WorldCurrentVelocity.normalized.ToOrientationEnum().ToQuaternion().eulerAngles.z;
			else
				DesiredDirection = TargetFaceDirectionOverride.ToQuaternion().eulerAngles.z;

			float CurrentForwards = ForwardsDirection.ToOrientationEnum().ToQuaternion().eulerAngles.z;
			OrientationEnum MovingDirection = (OrientationZ + (DesiredDirection - CurrentForwards)).Angle360ToOrientationEnum();

			OrientationEnum Orientation = OrientationZ.Angle360ToOrientationEnum();
			if (Orientation != MovingDirection)
			{
				// Only change orientation if movement is significant; threshold kept from original logic.
				float compareComponent = Mathf.Abs(WorldCurrentVelocity.x) > Mathf.Abs(WorldCurrentVelocity.y) ? Mathf.Abs(WorldCurrentVelocity.x) : Mathf.Abs(WorldCurrentVelocity.y);
				if (compareComponent > 10)
					TargetOrientation = MovingDirection;
			}
		}

		public void SetMatrixCorners(BetterBounds Bounds)
		{
			if (Debug)
			{
				if (MatrixBoundsGameGizmo.Count == 0)
					foreach (Vector3 Corner in Bounds.Corners())
						MatrixBoundsGameGizmo.Add(
							GameGizmomanager.AddNewSpriteStaticClient(null, Corner, Color.red, X));

				var i = 0;
				foreach (Vector3 Corner in Bounds.Corners())
				{
					MatrixBoundsGameGizmo[i].Position = Corner;
					i++;
				}
			}
			else if (MatrixBoundsGameGizmo.Count != 0)
			{
				foreach (GameGizmoSprite Corner in MatrixBoundsGameGizmo) Corner.Remove();

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
					AIGameGizmoSprite = GameGizmomanager.AddNewSpriteStaticClient(null, Position, Color.blue, X);

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
				float Difference = (CentreOfAIMovementWorld.RoundToInt() - TravelToWorldPOS).magnitude;

				if (Difference < 0.5f)
				{
					MatrixMoveAroundCurrentTargetCorner++;
					if (MatrixMoveAroundCurrentTargetCorner > 3) MatrixMoveAroundCurrentTargetCorner = 0;

					//Loggy.Error("going to MatrixMoveAroundCurrentTargetCorner " + MatrixMoveAroundCurrentTargetCorner);

					Vector3Int Position = NavigatingAroundBetterBounds.Value
						.GetCorner(MatrixMoveAroundCurrentTargetCorner.Value).RoundToInt();
					Position.z = 0;
					travelToWorldPOSOverride = Position;
				}
				else
				{
					Vector3 currentPosition = CentreOfAIMovementWorld;


					var Breakout = false;


					if ((PointIsWithinMatrixPerimeterPoint.Value - currentPosition).magnitude < 7) Breakout = true;
					//IgnoreMatrixs.Clear();
					//IgnoreMatrixs.AddRange(MovingAroundMatrixs);
					if (Breakout)
					{
						isMovingAroundMatrix = false;
						travelToWorldPOSOverride = null;
						IgnoreMatrixs.Clear();
						IgnoreMatrixs.AddRange(MovingAroundMatrixs);
						ClosestCashed = null;
						MatrixMoveAroundCurrentTargetCorner = null;
						MovingAroundMatrixs.Clear();
						PointIsWithinMatrixPerimeterPoint = null;
						return;
					}
				}
			}

			BetterBounds thisBigBound =
				MetaTileMap.matrix.MatrixInfo.WorldBounds
					.ExpandAllDirectionsBy(10); //TODO Handle if there are connected matrixes

			foreach (KeyValuePair<int, MatrixInfo> Matrix in MatrixManager.Instance.ActiveMatrices)
			{
				if ((Matrix.Value.WorldBounds.center - CentreOfAIMovementWorld).magnitude > 1000) continue;
				if (Matrix.Value == MatrixManager.Instance.spaceMatrix.MatrixInfo) continue;
				if (Matrix.Value == MetaTileMap.matrix.MatrixInfo) continue;
				if (IgnoreMatrixs.Contains(Matrix.Value)) continue;
				if (Matrix.Value.Matrix.AIShuttleShouldAvoid == false) continue;
				if (MovingAroundMatrixs.Contains(Matrix.Value)) continue;

				BetterBounds OtherBigBound = Matrix.Value.WorldBounds.ExpandAllDirectionsBy(10);


				if (thisBigBound.Intersects(OtherBigBound, out BetterBounds Overlap))
				{

					if (MovingAroundMatrixs.Count == 0) //reset
					{
						IgnoreMatrixs.Clear();
					}
					MovingAroundMatrixs.Add(Matrix.Value);
					foreach (MatrixInfo NavigatingMatrix in MovingAroundMatrixs)
						OtherBigBound = OtherBigBound.Combine(NavigatingMatrix.WorldBounds.ExpandAllDirectionsBy(10));

					//SO
					//now How to pick a corner to go to
					OtherBigBound = OtherBigBound.ExpandAllDirectionsBy(40);
					SetMatrixCorners(OtherBigBound);
					Vector3 Closest = OtherBigBound.Minimum;

					Vector3 DistanceToUse = CentreOfAIMovementWorld;
					if (ClosestCashed != null) DistanceToUse = ClosestCashed.Value;

					float BestDistance = (DistanceToUse - Closest).magnitude;
					var tempCorner = 0;

					MatrixMoveAroundCurrentTargetCorner = 0;
					foreach (Vector3 Corner in OtherBigBound.Corners())
					{
						float Distance = (DistanceToUse - Corner).magnitude;

						if (BestDistance > Distance)
						{
							BestDistance = Distance;
							Closest = Corner;

							MatrixMoveAroundCurrentTargetCorner = tempCorner;
						}

						tempCorner++;
					}

					MatrixMoveAroundCurrentTargetCorner--;
					if (MatrixMoveAroundCurrentTargetCorner < 0)
					{
						MatrixMoveAroundCurrentTargetCorner = 3;
					}

					//Loggy.Error("matrix corner" + MatrixMoveAroundCurrentTargetCorner);
					Closest = OtherBigBound.GetClosestPerimeterPoint(DistanceToUse);

					PointIsWithinMatrixPerimeterPoint =
						OtherBigBound.GetClosestPerimeterPoint(TravelToWorldPOSMatrixTraversall);
					//SO is The closest if it's big, may result in the Side swapping, the problem is and that means going through a matrix
					//If it's a step configurations

					NavigatingAroundBetterBounds = OtherBigBound;

					Vector3Int Position = Closest.RoundToInt();
					if (ClosestCashed == null) ClosestCashed = Closest;
					Position.z = 0;
					travelToWorldPOSOverride = Position;

					isMovingAroundMatrix = true;
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
}