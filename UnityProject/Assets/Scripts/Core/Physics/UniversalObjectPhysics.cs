using System;
using System.Collections.Generic;
using HealthV2;
using Items;
using Logs;
using Messages.Server.SoundMessages;
using Mirror;
using Objects;
using Objects.Construction;
using SecureStuff;
using Tiles;
using UnityEngine;
using UnityEngine.Events;
using Util;
using Random = UnityEngine.Random;

namespace Core.Physics
{
	public partial class UniversalObjectPhysics : NetworkBehaviour, IRegisterTileInitialised
	{
		///////// NOTES //////////
		// TODO: (Bod) Definitely can do the cleanup There's a decent amount of duplication
		// (Max): no shit
		//=========Needs to be resolved sometime=======
		// TODO: Movement desynchronisations,
		// TODO: Pulling a locker and then going down and the opposite direction you are pulling, causes the locker to get a push but server disagrees
		// TODO: parentContainer Need to test
		// TODO: Maybe work on conveyor belts and players a bit more
		// TODO: Sometime Combine buckling and object storage
		// TODO: move IsCuffed to PlayerOnlySyncValues maybe?
		// TODO: Make space Movement perfect ( Is pretty much good now )
		// TODO: after thrown not synchronised Properly need to synchronise rotation
		// TODO: When throwing rotation Direction needs to be set by server
		//=========Needs immediate attenion============
		//TODO: Smooth pushing, syncvar well if Statements in force and stuff On process player action, Smooth resetting if Space wind
		//////////////////////////////

		[PlayModeOnly] public Vector2 LocalDifferenceNeeded;
		[PlayModeOnly] private Vector2 newtonianMovement; //* attributes.Size -> weight
		[PlayModeOnly] public Vector3 LocalTargetPosition;
		[PlayModeOnly] private Vector3 LastDifference = Vector3.zero;
		[PlayModeOnly] public bool CorrectingCourse = false;
		[PlayModeOnly] public bool Animating = false;

		[PlayModeOnly] public bool SetIgnoreSticky = false;

		//Cannot grab onto anything so no friction
		[PlayModeOnly] public float airTime;
		[PlayModeOnly] public float slideTime;

		[PlayModeOnly] public float spinMagnitude = 0;

		//Reduced friction during this time, if stickyMovement Just has normal friction vs just grabbing
		[PlayModeOnly] public GameObject thrownBy;
		[PlayModeOnly] public GameObject thrownProtection;
		[PlayModeOnly] public BodyPartType aim;
		[PlayModeOnly] public int ForcedPushedFrame = 0;
		[PlayModeOnly] public int TryPushedFrame = 0;
		[PlayModeOnly] public int PushedFrame = 0;
		public bool ChangesDirectionPush = false;
		public bool Intangible = false;

		public bool MappingIntangible = false;

		public bool SnapToGridOnStart = false;
		public bool IsPlayer = false;

		protected MatrixCash SetMatrixCache = new MatrixCash();

		public float ObjectBouncyness = 0.75f;

		public const float DEFAULT_PUSH_SPEED = 6;

		/// <summary>
		/// Maximum speed player can reach by throwing stuff in space
		/// </summary>
		public const float MAX_SPEED = 25;

		public const int HIGH_SPEED_COLLISION_THRESHOLD = 13;
		public const float DEFAULT_Friction = 15f;
		public const float DEFAULT_SLIDE_FRICTION = 9f;

		public CheckedComponent<Pickupable> pickupable = new CheckedComponent<Pickupable>();
		private BoxCollider2D Collider; //TODO Checked component
		private FloorDecal floorDecal; // Used to make sure some objects are not causing gravity
		private Vector3Int oldLocalTilePosition;

		private float localTileMoveSpeedOverride = 0;

		[SyncVar]
		private float
			networkedTileMoveSpeedOverride = 0; //TODO Potential Desynchronisation issues, Probably should have a who caused

		[SyncVar] public float tileMoveSpeed = 1;
		[SyncVar] private uint parentContainer;
		[SyncVar] protected int SetTimestampID = -1;
		[SyncVar] protected int SetLastResetID = -1;
		[SyncVar] public bool HasOwnGravity = false;
		[SyncVar] private bool doNotApplyMomentumOnTarget = false;

		[SyncVar(hook = nameof(SynchroniseVisibility))]
		private bool isVisible = true;

		[SyncVar(hook = nameof(SyncLocalTarget))]
		private Vector3WithData synchLocalTargetPosition;

		protected bool doStepInteractions = true;

		[NonSerialized] public bool DoImpactVomit = true;

		public Vector3 OfficialPosition => GetRootObject.transform.position;
		public bool IsVisible => isVisible;
		public bool IsNotPushable => isNotPushable;
		public bool CanMove => isNotPushable == false && IsBuckled == false;

		public Vector3WithData SetLocalTarget
		{
			set
			{
				if (CustomNetworkManager.IsServer && synchLocalTargetPosition.Vector3 != value.Vector3)
				{
					SyncLocalTarget(synchLocalTargetPosition, value);
				}

				LocalTargetPosition = value.Vector3;
			}
		}

		public float CurrentTileMoveSpeed
		{
			get
			{
				if (localTileMoveSpeedOverride != 0)
				{
					return Mathf.Max(0.25f, localTileMoveSpeedOverride);
				}

				if (networkedTileMoveSpeedOverride != 0)
				{
					return Mathf.Max(0.25f, networkedTileMoveSpeedOverride);
				}
				else
				{
					return Mathf.Max(0.25f, tileMoveSpeed);
				}
			}
		}

		public Vector2 NewtonianMovement
		{
			get => newtonianMovement;
			set
			{
				if (value.magnitude > MAX_SPEED)
				{
					value *= (MAX_SPEED / value.magnitude);
				}

				newtonianMovement = value;
			}
		}

		public bool IsSliding => slideTime > 0;
		public bool IsInAir => airTime > 0;

		public bool stickyMovement = false;

		//If this thing likes to grab onto stuff such as like a player
		public bool IsStickyMovement => stickyMovement && SetIgnoreSticky == false;
		public bool OnThrowEndResetRotation;


		public float maximumStickSpeed = 1.5f;
		//Speed In tiles per second that, The thing would able to be stop itself if it was sticky

		public bool onStationMovementsRound;

		[HideInInspector] public CheckedComponent<Attributes> attributes = new CheckedComponent<Attributes>();

		[HideInInspector] public RegisterTile registerTile;

		protected LayerMask defaultInteractionLayerMask;

		[HideInInspector] public GameObject[] ContextGameObjects = new GameObject[2];

		[PlayModeOnly] public bool IsCurrentlyFloating;

		private bool
			ResetClientPositionReachTile =
				false; //this is needed to fix issues with pull getting out of sync for Other players, Properly should fix the root cause, Of sending Delta pushes

		private uint
			SpecifiedClientPositionReachTile =
				0; //This is so when the client walks back into its own container it was pulling it doesn't bug out

		//Pulling.Component.ResetLocationOnClients();



		#region Events

		[PlayModeOnly] public ForceEvent OnThrowStart = new ForceEvent();

		[PlayModeOnly] public ForceEventWithChange OnImpact = new ForceEventWithChange();

		[PlayModeOnly] public DualVector3IntEvent OnLocalTileReached = new DualVector3IntEvent();

		[PlayModeOnly] public ForceEvent OnThrowEnd = new ForceEvent();

		[PlayModeOnly] public Action OnVisibilityChange;

		#endregion

		[HideInInspector] public List<IBumpableObject> Bumps = new List<IBumpableObject>();

		[HideInInspector] public List<UniversalObjectPhysics> Hits = new List<UniversalObjectPhysics>();

		[PlayModeOnly, SerializeField] private bool isFlyingSliding;


		[PlayModeOnly] public bool IsMoving = false; //Is animating with tile movement

		public bool IsWalking => MoveIsWalking && IsMoving;

		[PlayModeOnly] public bool MoveIsWalking = false;
		[PlayModeOnly] public double LastUpdateClientFlying = 0; //NetworkTime.time
		[PlayModeOnly] public float TimeSpentFlying = 0;
		[PlayModeOnly] private float SecondsFlying;

		public bool IsFlyingSliding
		{
			get
			{
				return isFlyingSliding;
				//Is animating with space flying
			}
			set
			{
				if (value)
				{
					SecondsFlying = 0;
				}

				isFlyingSliding = value;
			}
		}

		private List<DirectionAndDecision> TriedDirectionsFrame = new List<DirectionAndDecision>();

		public struct DirectionAndDecision
		{
			public Vector2Int worldDirection;
			public bool Decision;
		}

		public virtual void Awake()
		{
			Collider = this.GetComponent<BoxCollider2D>();
			floorDecal = this.GetComponent<FloorDecal>();
			ContextGameObjects[0] = gameObject;
			defaultInteractionLayerMask = LayerMask.GetMask("Furniture", "Walls", "Windows", "Machines", "Players",
				"Door Closed",
				"HiddenWalls", "Objects");
			attributes.DirectSetComponent(GetComponent<Attributes>());
			registerTile = GetComponent<RegisterTile>();
			rotatable = GetComponent<Rotatable>();
			pickupable.DirectSetComponent(GetComponent<Pickupable>());

			SetRotationTarget();
		}

		public void Start()
		{
			if (isServer)
			{
				SetLocalTarget = new Vector3WithData()
				{
					Vector3 = transform.localPosition,
					ByClient = NetId.Empty,
					Matrix = -1,
					Speed = tileMoveSpeed
				};
			}
			else
			{
				InternalTriggerOnLocalTileReached(synchLocalTargetPosition.Vector3.RoundToInt());
				SetTransform(synchLocalTargetPosition.Vector3, false);
			}

			CheckNSnapToGrid(isServer);
		}

		public virtual void OnDestroy()
		{
			if (PulledBy.HasComponent)
			{
				PulledBy.Component.PullSet(null, false);
			}

			if (Animating) UpdateManager.Remove(CallbackType.EARLY_UPDATE, AnimationUpdateMe);
			if (IsFlyingSliding)
			{
				UpdateManager.Remove(CallbackType.EARLY_UPDATE, FlyingUpdateMe);
			}

			if (CorrectingCourse) UpdateManager.Remove(CallbackType.EARLY_UPDATE, FloatingCourseCorrection);
			if (BuckledToObject != null) Unbuckle();
			if (ObjectIsBuckling != null) ObjectIsBuckling.Unbuckle();
		}

		public virtual void OnEnable() { }

		public virtual void OnDisable()
		{
			UpdateManager.Remove(CallbackType.EARLY_UPDATE, FlyingUpdateMe);
			UpdateManager.Remove(CallbackType.EARLY_UPDATE, AnimationUpdateMe);
			UpdateManager.Remove(CallbackType.EARLY_UPDATE, FloatingCourseCorrection);
		}

		public void CheckNSnapToGrid(bool isServer)
		{
			if (SnapToGridOnStart && isServer)
			{
				SetTransform(transform.position.RoundToInt(), true);
			}
		}


		public void OnRegisterTileInitialised(RegisterTile registerTile)
		{
			//Set old pos here so it is somewhat more valid than default vector value
			oldLocalTilePosition = transform.localPosition.RoundToInt();
			InternalTriggerOnLocalTileReached(oldLocalTilePosition);
		}

		public Size GetSize()
		{
			return attributes.HasComponent ? attributes.Component.Size : Size.Huge;
		}

		public float GetWeight()
		{
			return SizeToWeight(GetSize());
		}

		public void SetMovementSpeed(float newMove)
		{
			if (isServer == false) return;
			tileMoveSpeed = newMove;
		}


		public void SyncLocalTarget(Vector3WithData oldLocalTarget, Vector3WithData newLocalTarget)
		{
			synchLocalTargetPosition = newLocalTarget;

			if (isServer) return;
			if (LocalTargetPosition == newLocalTarget.Vector3) return;
			if (isOwned && PulledBy.HasComponent == false) return;


			var spawned = CustomNetworkManager.Spawned;

			if (newLocalTarget.ByClient is not NetId.Empty or NetId.Invalid
			    && spawned.TryGetValue(newLocalTarget.ByClient, out var local)
			    && local.gameObject == PlayerManager.LocalPlayerObject) return;

			if (newLocalTarget.Matrix != -1)
			{
				SetMatrix(MatrixManager.Get(newLocalTarget.Matrix).Matrix, false);
			}

			if (isClient)
			{
				SetLocalTarget = newLocalTarget;
			}

			if (IsFlyingSliding)
			{
				IsFlyingSliding = false;
				airTime = 0;
				slideTime = 0;
				UpdateManager.Remove(CallbackType.EARLY_UPDATE, FlyingUpdateMe);
			}

			if (Animating == false && transform.localPosition != newLocalTarget.Vector3)
			{
				Animating = true;
				UpdateManager.Add(CallbackType.EARLY_UPDATE, AnimationUpdateMe);
			}
		}


		[ClientRpc] //TODO investigate GameObject to netID Breaking everything
		public void RPCClientTilePush(Vector2Int worldDirection, float speed, int causedByClient, bool overridePull,
			int timestampID, bool forced)
		{
			SetTimestampID = timestampID;
			if (isServer) return;
			GameObject causedByClientOb = ((uint) causedByClient).NetIdToGameObject();
			if (PlayerManager.LocalPlayerObject == causedByClientOb) return;
			if (PulledBy.HasComponent && overridePull == false) return;

			Pushing.Clear();
			SetMatrixCache.ResetNewPosition(transform.position, registerTile);
			if (forced)
			{
				ForceTilePush(worldDirection, Pushing, causedByClientOb, speed);
			}
			else
			{
				TryTilePush(worldDirection, causedByClientOb, speed);
			}
		}


		public bool IDIsLocalPlayerObject(uint ID)
		{
			return ID is not NetId.Empty or NetId.Invalid
			       && CustomNetworkManager.Spawned.TryGetValue(ID, out var local)
			       && local.gameObject == PlayerManager.LocalPlayerObject;
		}

		[ClientRpc]
		public void UpdateClientMomentum(Vector3 resetToLocal, Vector2 newMomentum, float inairTime, float inslideTime,
			int matrixID, float inSpinFactor, bool forceOverride, uint doNotUpdateThisClient, float timeSent)
		{
			if (isServer) return;

			if (IDIsLocalPlayerObject(doNotUpdateThisClient)) return;

			if (IsFlyingSliding && (TimeSpentFlying - timeSent) < 0)
			{
				return; //Invalid check request, In between landing and push off
			}


			//if (isLocalPlayer) return; //We are updating other Objects than the player on the client //TODO Also block if being pulled by local player //Why we need this?
			Vector2? OldMomentum = null;
			if (NewtonianMovement != newMomentum)
			{
				OldMomentum = NewtonianMovement;
				NewtonianMovement = newMomentum;
				airTime = inairTime;
				slideTime = inslideTime;
				spinMagnitude = inSpinFactor;
				SetMatrix(MatrixManager.Get(matrixID).Matrix);
			}


			if (IsFlyingSliding) //If we flying try and smooth it
			{
				if (isOwned && this is MovementSynchronisation) //The client is technically ahead of the server
				{
					var TimeDifference = (TimeSpentFlying - timeSent);
					var ToResetToPosition = resetToLocal + (newMomentum * TimeDifference).To3();
					;
					resetToLocal = ToResetToPosition;
				}

				LocalDifferenceNeeded = resetToLocal - transform.localPosition;


				if (CorrectingCourse == false)
				{
					CorrectingCourse = true;
					UpdateManager.Add(CallbackType.EARLY_UPDATE, FloatingCourseCorrection);
				}
			}
			else //We are walking around somewhere
			{
				SetLocalTarget = new Vector3WithData()
				{
					Vector3 = resetToLocal,
					ByClient = NetId.Empty,
					Matrix = matrixID
				};
				SetTransform(resetToLocal, false);
			}

			InternalTriggerOnLocalTileReached(resetToLocal.RoundToInt());

			if (Animating == false && IsFlyingSliding == false)
			{
				StartFlyingUpdateMe();
			}
		}

		private void SynchroniseVisibility(bool oldVisibility, bool newVisibility)
		{
			isVisible = newVisibility;
			OnVisibilityChange?.Invoke();

			if (isVisible)
			{
				var sprites = GetComponentsInChildren<SpriteRenderer>();
				foreach (var sprite in sprites)
				{
					sprite.enabled = true;
				}

				if (this is not MovementSynchronisation c) return;
				c.playerScript.RegisterPlayer.LayDownBehavior.EnsureCorrectState();
			}
			else
			{
				var sprites = GetComponentsInChildren<SpriteRenderer>();
				foreach (var sprite in sprites)
				{
					sprite.enabled = false;
				}

				SetTransform(TransformState.HiddenPos, false);
				InternalTriggerOnLocalTileReached(TransformState.HiddenPos);
				ResetEverything();

				if (BuckledToObject != null)
				{
					BuckledToObject.Unbuckle();
				}
			}
		}

		public virtual void AppearAtWorldPositionServer(Vector3 worldPos, bool smooth = false, bool doStepInteractions = true,
			Vector2? momentum = null, MatrixInfo  Matrixoveride = null)
		{
			this.doStepInteractions = doStepInteractions;

			SynchroniseVisibility(isVisible, true);
			var matrix = MatrixManager.AtPoint(worldPos, isServer);
			if (Matrixoveride != null)
			{
				matrix = Matrixoveride;
			}
			ForceSetLocalPosition(worldPos.ToLocal(matrix), momentum == null ? Vector2.zero : momentum.Value, smooth,
				matrix.Id);

			this.doStepInteractions = true;
		}

		public void DropAtAndInheritMomentum(UniversalObjectPhysics droppedFrom)
		{
			SynchroniseVisibility(isVisible, true);
			AppearAtWorldPositionServer(droppedFrom.OfficialPosition,
				momentum: droppedFrom.GetRootObject.GetComponent<UniversalObjectPhysics>().NewtonianMovement);
		}

		public void DisappearFromWorld()
		{
			if (CustomNetworkManager.IsServer)
			{
				SynchroniseVisibility(isVisible, false);
			}
		}

		public void ForceSetLocalPosition(Vector3 resetToLocal, Vector2 momentum, bool smooth, int matrixID,
			bool updateClient = true, float rotation = 0, NetworkConnection client = null, int resetID = -1,
			uint ignoreForClient = NetId.Empty, Vector3? localTarget = null)
		{
			//rotationTarget.rotation = Quaternion.Euler(new Vector3(0, 0, rotation));

			if (isServer && updateClient)
			{
				isVisible = true;
				if (client != null)
				{
					resetID = resetID == -1 ? Time.frameCount : resetID;
					SetLastResetID = resetID;
					RPCForceSetPosition(
						client,
						resetToLocal,
						momentum,
						localTarget ?? TransformState.HiddenPos,
						smooth
						, matrixID,
						rotation,
						resetID);
				}
				else
				{
					resetID = resetID == -1 ? Time.frameCount : resetID;
					SetLastResetID = resetID;
					RPCForceSetPosition(
						resetToLocal,
						momentum,
						localTarget ?? TransformState.HiddenPos,
						smooth,
						matrixID,
						rotation,
						resetID,
						ignoreForClient);
				}
			}
			else if (isServer == false)
			{
				SetLastResetID = resetID;
			}

			SetMatrix(MatrixManager.Get(matrixID).Matrix);

			if (smooth)
			{
				if (IsFlyingSliding)
				{
					NewtonianMovement = momentum;
					LocalDifferenceNeeded = resetToLocal - transform.localPosition;
					InternalTriggerOnLocalTileReached(resetToLocal.RoundToInt());
					if (CorrectingCourse == false)
					{
						CorrectingCourse = true;
						UpdateManager.Add(CallbackType.EARLY_UPDATE, FloatingCourseCorrection);
					}
				}
				else
				{
					var ToResetTo = resetToLocal;

					if (localTarget != null)
					{
						ToResetTo = localTarget.Value;
					}

					NewtonianMovement = momentum;
					SetLocalTarget = new Vector3WithData()
					{
						Vector3 = ToResetTo,
						ByClient = NetId.Empty,
						Matrix = matrixID
					};
					InternalTriggerOnLocalTileReached(resetToLocal.RoundToInt());

					if (Animating == false)
					{
						Animating = true;
						UpdateManager.Add(CallbackType.EARLY_UPDATE, AnimationUpdateMe);
					}
				}
			}
			else
			{
				if (IsFlyingSliding)
				{
					NewtonianMovement = momentum;
					SetTransform(resetToLocal, false);
					InternalTriggerOnLocalTileReached(resetToLocal.RoundToInt());
				}
				else
				{
					var ToResetTo = resetToLocal;

					if (localTarget != null)
					{
						ToResetTo = localTarget.Value;
					}

					NewtonianMovement = momentum;
					SetLocalTarget = new Vector3WithData()
					{
						Vector3 = ToResetTo,
						ByClient = NetId.Empty,
						Matrix = matrixID
					};
					SetTransform(resetToLocal, false);
					InternalTriggerOnLocalTileReached(resetToLocal);

					if (Animating == false)
					{
						Animating = true;
						UpdateManager.Add(CallbackType.EARLY_UPDATE, AnimationUpdateMe);
					}
				}
			}
		}

		[ClientRpc]
		public void RPCForceSetPosition(Vector3 resetToLocal, Vector2 momentum, Vector3 LocalTarget, bool smooth,
			int matrixID, float rotation,
			int resetID, uint ignoreForClient)
		{
			if (isServer) return;
			if (ignoreForClient is not NetId.Empty or NetId.Invalid
			    && CustomNetworkManager.Spawned.TryGetValue(ignoreForClient, out var local)
			    && local.gameObject == PlayerManager.LocalPlayerObject) return;

			Vector3? NullLocalTarget = LocalTarget;
			if (NullLocalTarget.Value == TransformState.HiddenPos)
			{
				NullLocalTarget = null;
			}

			ForceSetLocalPosition(resetToLocal, momentum, smooth, matrixID, false, rotation, resetID: resetID,
				localTarget: NullLocalTarget);
		}

		[TargetRpc]
		public void RPCForceSetPosition(NetworkConnection target, Vector3 resetToLocal, Vector2 momentum,
			Vector3 LocalTarget, bool smooth,
			int matrixID, float rotation, int resetID)
		{
			Vector3? NullLocalTarget = LocalTarget;
			if (NullLocalTarget.Value == TransformState.HiddenPos)
			{
				NullLocalTarget = null;
			}

			ForceSetLocalPosition(resetToLocal, momentum, smooth, matrixID, false, rotation, resetID: resetID,
				localTarget: NullLocalTarget);
		}


		//Warning only update clients!!
		public void ResetLocationOnClients(bool smooth = false, uint ignoreForClient = NetId.Empty)
		{
			if (isServer == false) return;
			SetLastResetID = Time.frameCount;
			RPCForceSetPosition(transform.localPosition, NewtonianMovement, LocalTargetPosition, smooth,
				registerTile.Matrix.Id,
				rotationTarget.localRotation.eulerAngles.z, SetLastResetID, ignoreForClient);

			if (Pulling.HasComponent)
			{
				Pulling.Component.ResetLocationOnClients(smooth, ignoreForClient);
			}

			if (ObjectIsBuckling != null && ObjectIsBuckling.Pulling.HasComponent)
			{
				ObjectIsBuckling.Pulling.Component.ResetLocationOnClients(smooth, ignoreForClient);
			}
			//Update client to server state
		}

		//Warning only update client!!

		public void ResetLocationOnClient(NetworkConnection client, bool smooth = false)
		{
			isVisible = true;
			SetLastResetID = Time.frameCount;
			RPCForceSetPosition(client, transform.localPosition, NewtonianMovement, LocalTargetPosition, smooth,
				registerTile.Matrix.Id,
				rotationTarget.localRotation.eulerAngles.z, SetLastResetID);

			if (Pulling.HasComponent)
			{
				Pulling.Component.ResetLocationOnClient(client, smooth);
			}
			//Update client to server state
		}


		public void FloatingCourseCorrection()
		{
			if (this == null)
			{
				UpdateManager.Remove(CallbackType.EARLY_UPDATE, FloatingCourseCorrection);
				return;
			}

			CorrectingCourse = true;
			var position = transform.localPosition;
			var newPosition = this.MoveTowards(position, (position + LocalDifferenceNeeded.To3()),
				(NewtonianMovement.magnitude + 4) * Time.deltaTime);
			LocalDifferenceNeeded -= (newPosition - position).To2();
			SetTransform(newPosition, false);

			if (LocalDifferenceNeeded.magnitude < 0.01f)
			{
				CorrectingCourse = false;
				UpdateManager.Remove(CallbackType.EARLY_UPDATE, FloatingCourseCorrection);
			}
		}


		public void ServerSetAnchored(bool isAnchored, GameObject performer)
		{
			//check if blocked
			if (isAnchored)
			{
				if (ServerValidations.IsConstructionBlocked(performer, gameObject,
					    (Vector2Int) registerTile.WorldPositionServer)) return;
			}

			SetIsNotPushable(isAnchored);
		}

		public void SetMatrix(Matrix movetoMatrix, bool SetTarget = true)
		{

			if (movetoMatrix == null) return;
			if (registerTile == null)
			{
				Loggy.LogError("null Register tile on " + this.name);
				return;
			}

			var TransformCash = transform.position;
			if (isServer)
			{
				registerTile.ServerSetNetworkedMatrixNetID(movetoMatrix.NetworkedMatrix.MatrixSync.netId);
			}

			registerTile.FinishNetworkedMatrixRegistration(movetoMatrix.NetworkedMatrix);
			SetTransform(TransformCash, true);
			LocalDifferenceNeeded = Vector2.zero;

			if (SetTarget)
			{
				SetLocalTarget = new Vector3WithData()
				{
					Vector3 = transform.localPosition,
					ByClient = NetId.Empty,
					Matrix = movetoMatrix.Id
				};
			}
		}

		/// <summary>
		/// Return true if the object causes gravity, otherwise false.
		/// <returns>bool that represents if the object must cause gravity or not.</returns>
		/// </summary>
		public bool CausesGravity()
		{
			if (isNotPushable == false || floorDecal != null)
				return false;

			return true;
		}

		private DirectionAndDecision? GetDecision(Vector2Int worldDirection)
		{
			var Count = TriedDirectionsFrame.Count;
			for (int i = 0; i < Count; i++)
			{
				if (TriedDirectionsFrame[i].worldDirection == worldDirection)
				{
					return TriedDirectionsFrame[i];
				}
			}

			return null;
		}

		public void ResetEverything()
		{

			if (IsFlyingSliding)
			{
				UpdateManager.Remove(CallbackType.EARLY_UPDATE, FlyingUpdateMe);
				IsFlyingSliding = false;
			}

			if (Animating)
			{
				UpdateManager.Remove(CallbackType.EARLY_UPDATE, AnimationUpdateMe);
				Animating = false;
			}

			if (CorrectingCourse)
			{
				UpdateManager.Remove(CallbackType.EARLY_UPDATE, FloatingCourseCorrection);
				CorrectingCourse = false;
			}

			IsMoving = false;
			MoveIsWalking = false;
			if (registerTile.Matrix != null)
			{
				SetLocalTarget = new Vector3WithData()
				{
					Vector3 = transform.localPosition,
					ByClient = NetId.Empty,
					Matrix = registerTile.Matrix.Id
				};
			}


			NewtonianMovement = Vector2.zero;
			airTime = 0;
			slideTime = 0;
			IsFlyingSliding = false;
			Animating = false;
			if (this is not MovementSynchronisation c) return;
			c.playerScript.RegisterPlayer.LayDownBehavior.EnsureCorrectState();
		}

		[Server]
		public void ForceDrop(Vector3 pos)
		{
			Vector2 impulse = Random.insideUnitCircle.normalized;
			var matrix = MatrixManager.AtPoint(pos, isServer);
			var localPos = pos.ToLocal(matrix);
			ForceSetLocalPosition(localPos, impulse * Random.Range(0.2f, 2f), false, matrix.Id);
		}

		//what is a Swap
		//server both swap ///
		//client running They swap positions (Client predicted) Nothing from server
		//client running Nothing (client Miss predict)  , ( Body that was pushed gets moved)
		//Client standing there, They moved a tile /
		//Client observing  both swap ?/

		public void CheckMatrixSwitch()
		{
			var newMatrix =
				MatrixManager.AtPoint(transform.position, true, registerTile.Matrix.MatrixInfo);
			if (registerTile.Matrix.Id != newMatrix.Id)
			{
				var worldPOS = transform.position;
				SetMatrix(newMatrix.Matrix);
				InternalTriggerOnLocalTileReached(worldPOS.ToLocal(newMatrix.Matrix).RoundToInt());
				ResetLocationOnClients();
			}
		}

		public void NewtonianNewtonPush(Vector2 worldDirection, float newtonians = Single.NaN,
			float inAirTime = Single.NaN,
			float inSlideTime = Single.NaN, BodyPartType inAim = BodyPartType.Chest, GameObject inThrownBy = null,
			float spinFactor = 0) //Collision is just naturally part of Newtonian push
		{
			var speed = newtonians / SizeToWeight(GetSize());
			NewtonianPush(worldDirection, speed, inAirTime, inSlideTime, inAim, inThrownBy, spinFactor);
		}

		public void NewtonianPush(Vector2 worldDirection, float speed, float nairTime = Single.NaN,
			float inSlideTime = Single.NaN, BodyPartType inAim = BodyPartType.Chest, GameObject inThrownBy = null,
			float spinFactor = 0, GameObject doNotUpdateThisClient = null,
			bool ignoreSticky = false) //Collision is just naturally part of Newtonian push
		{
			if (isVisible == false) return;
			if (CanMove == false) return;
			if (PulledBy.HasComponent) return;
			if (worldDirection == Vector2.zero) return;

			if (speed == 0) return;

			if (speed.IsUnreasonableNumber())
			{
				Loggy.LogError("Unreasonable number detected in NewtonianPush for" + this.gameObject);
				return;
			}

			aim = inAim;
			thrownBy = inThrownBy;
			thrownProtection = thrownBy;
			if (Random.Range(0, 2) == 1)
			{
				spinMagnitude = spinFactor * 1;
			}
			else
			{
				spinMagnitude = spinFactor * -1;
			}

			if (float.IsNaN(nairTime) == false || float.IsNaN(inSlideTime) == false)
			{
				worldDirection.Normalize();

				NewtonianMovement += worldDirection * speed;

				if (float.IsNaN(nairTime) == false)
				{
					airTime = nairTime;
				}

				if (float.IsNaN(inSlideTime) == false)
				{
					this.slideTime = inSlideTime;
				}
			}
			else
			{
				if (IsStickyMovement && IsFloating() == false)
				{
					return;
				}

				worldDirection.Normalize();
				NewtonianMovement += worldDirection * speed;
			}

			OnThrowStart.Invoke(this);
			if (NewtonianMovement.magnitude > 0.01f)
			{
				StartFlyingUpdateMe();
			}

			if (isServer)
			{
				LastUpdateClientFlying = NetworkTime.time;
				UpdateClientMomentum(transform.localPosition, NewtonianMovement, airTime, this.slideTime,
					registerTile.Matrix.Id, spinFactor, true, doNotUpdateThisClient.NetId(), TimeSpentFlying);
			}
		}

		public void AppliedFriction(float frictionCoefficient)
		{
			var speedLossDueToFriction = frictionCoefficient * Time.deltaTime;

			var oldMagnitude = NewtonianMovement.magnitude;

			var newMagnitude = oldMagnitude - speedLossDueToFriction;

			if (newMagnitude <= 0)
			{
				NewtonianMovement *= 0;
			}
			else
			{
				NewtonianMovement *= (newMagnitude / oldMagnitude);
			}
		}

		public void AnimationUpdateMe()
		{
			if (isVisible == false)
			{
				MoveIsWalking = false;
				IsMoving = false;
				Animating = false;
				UpdateManager.Remove(CallbackType.EARLY_UPDATE, AnimationUpdateMe);
				return;
			}

			if (this == null || transform == null)
			{
				MoveIsWalking = false;
				IsMoving = false;
				Animating = false;
				UpdateManager.Remove(CallbackType.EARLY_UPDATE, AnimationUpdateMe);
			}

			Animating = true;
			var localPos = transform.localPosition;

			IsMoving = (localPos - LocalTargetPosition).magnitude > 0.001;
			if (IsMoving)
			{
				SetTransform(this.MoveTowards(localPos, LocalTargetPosition,
					CurrentTileMoveSpeed * Time.deltaTime), false);


				LastDifference = transform.localPosition - localPos;
			}
			else
			{
				var cache = CurrentTileMoveSpeed;
				localTileMoveSpeedOverride = 0;
				if (isServer)
				{
					networkedTileMoveSpeedOverride = 0;
				}

				Animating = false;

				MoveIsWalking = false;

				if (IsFloating() && PulledBy.HasComponent == false && doNotApplyMomentumOnTarget == false)
				{
					NewtonianMovement = (Vector2) LastDifference.normalized * cache;
					LastDifference = Vector3.zero;
				}

				doNotApplyMomentumOnTarget = false;

				UpdateManager.Remove(CallbackType.EARLY_UPDATE, AnimationUpdateMe);

				if (ResetClientPositionReachTile)
				{
					ResetClientPositionReachTile = false;
					ResetLocationOnClients(ignoreForClient: SpecifiedClientPositionReachTile);
					SpecifiedClientPositionReachTile = NetId.Empty;
				}

				InternalTriggerOnLocalTileReached(transform.localPosition.RoundToInt());

				if (NewtonianMovement.magnitude > 0.01f)
				{
					//It's moving add to update manager
					StartFlyingUpdateMe();
				}
			}
		}

		public void SetTransform(Vector3 position, bool world)
		{
			if (world)
			{
				transform.position = position;
			}
			else
			{
				transform.localPosition = position;
			}

			if (ObjectIsBuckling != null)
			{
				ObjectIsBuckling.SetTransform(position, world);
			}
		}

		public void SetRegisterTileLocation(Vector3Int localPosition)
		{
			if (MappingIntangible == false)
			{
				registerTile.ServerSetLocalPosition(localPosition); //TODO It's dumb and slow Combine into one
				registerTile.ClientSetLocalPosition(localPosition);
			}

			if (ObjectIsBuckling != null)
			{
				ObjectIsBuckling.SetRegisterTileLocation(localPosition);
			}
		}

		public Vector3 MoveTowards(
			Vector3 current,
			Vector3 target,
			float maxDistanceDelta)
		{
			var magnitude = (current - target).magnitude;
			if (magnitude > 7f)
			{
				maxDistanceDelta *= 40;
			}
			else if (magnitude > 3f)
			{
				maxDistanceDelta *= 10;
			}

			return Vector3.MoveTowards(current, target,
				maxDistanceDelta);
		}


		public void StartFlyingUpdateMe()
		{
			//It's moving add to update manager
			if (IsFlyingSliding == false)
			{
				IsFlyingSliding = true;
				TimeSpentFlying = 0;
				LastUpdateClientFlying = NetworkTime.time;
				UpdateManager.Add(CallbackType.EARLY_UPDATE, FlyingUpdateMe);
			}
		}

		private void NewtonianNaNCorrection()
		{
			if (NewtonianMovement.x.IsUnreasonableNumber() || NewtonianMovement.y.IsUnreasonableNumber())
			{
				Loggy.LogError("Unreasonable number detected with NewtonianMovement" + transform.name);
				var vec = NewtonianMovement;
				vec.x = 0;
				vec.y = 0;
				NewtonianMovement = vec;
			}

			if (transform.position.x.IsUnreasonableNumber() || transform.position.y.IsUnreasonableNumber() ||
			    transform.position.z.IsUnreasonableNumber())
			{
				Loggy.LogError("Unreasonable number detected with transform.position with " + transform.name);
				var vec = transform.position;
				vec.x = 0;
				vec.y = 0;
				vec.z = 0;
				transform.position = vec;
			}
		}

		private void AirTimeChecks()
		{
			SecondsFlying += Time.deltaTime;
			if (SecondsFlying > 90) //Stop taking up CPU resources! If you're flying through space for too long
			{
				NewtonianMovement *= 0;
			}


			if (PulledBy.HasComponent) return; //It is recursively handled By parent

			if (airTime > 0)
			{
				airTime -= Time.deltaTime; //Doesn't matter if it goes under zero

				if (airTime <= 0)
				{
					OnImpact?.Invoke(this, NewtonianMovement);
				}
			}
			else if (slideTime > 0)
			{
				slideTime -= Time.deltaTime; //Doesn't matter if it goes under zero
				var floating = IsFloating();
				if (floating == false)
				{
					AppliedFriction(DEFAULT_Friction);
				}
			}
			else if (IsStickyMovement)
			{
				var floating = IsFloating();

				if (floating == false)
				{
					if (NewtonianMovement.magnitude > maximumStickSpeed) //Too fast to grab onto anything
					{
						AppliedFriction(DEFAULT_Friction);
					}
					else
					{
						//Stuck
						NewtonianMovement *= 0;
					}
				}

			}
			else
			{
				var floating = IsFloating();
				if (floating == false)
				{
					AppliedFriction(DEFAULT_Friction);
				}
			}
		}

		private void ProcessCollisionDetection(ref Vector3 position, ref Vector3 newPosition)
		{
			if (Collider != null) Collider.enabled = false;
			var hit = MatrixManager.Linecast((position - (NewtonianMovement.normalized * 0.2f).To3()),
				LayerTypeSelection.Walls | LayerTypeSelection.Grills | LayerTypeSelection.Windows,
				defaultInteractionLayerMask, newPosition);
			if (hit.ItHit)
			{
				OnImpact?.Invoke(this, NewtonianMovement);
				NewtonianMovement -= 2 * (NewtonianMovement * hit.Normal) * hit.Normal;
				var offset = (0.1f * hit.Normal);
				newPosition = hit.HitWorld + offset.To3();
				NewtonianMovement *= ObjectBouncyness;
				spinMagnitude *= -1;
				if (hit.CollisionHit.GameObject != null)
				{
					Hits.Add(hit.CollisionHit.GameObject.GetUniversalObjectPhysics());
				}
			}

			if (Collider != null) Collider.enabled = true;
		}

		private void ProcessThingsToHit()
		{
			if (Hits.Count > 0)
			{
				OnImpact?.Invoke(this, NewtonianMovement);
			}

			if (attributes.Component is not ItemAttributesV2 iav2) return;
			foreach (var hit in Hits)
			{
				//TODO: DamageTile( goal,Matrix.Matrix.TilemapsDamage);
				if (hit == null) continue;
				if (NewtonianMovement.magnitude < iav2.ThrowSpeed * 0.25f) continue;
				var damage = (iav2.ServerThrowDamage);
				if (hit.TryGetComponent<Integrity>(out var integrity) && isServer)
				{
					integrity.ApplyDamage(damage, AttackType.Melee, iav2.ServerDamageType);
				}

				//TODO: Add the ability to catch thrown objects if the player has the "throw" state enabled on them.
				if (hit.TryGetComponent<LivingHealthMasterBase>(out var livingHealthMasterBase) && isServer)
				{
					var randomHitZone = aim.Randomize();
					livingHealthMasterBase.ApplyDamageToBodyPart(thrownBy, damage, AttackType.Melee,
						DamageType.Brute,
						randomHitZone);
					global::Chat.AddThrowHitMsgToChat(gameObject, livingHealthMasterBase.gameObject,
						randomHitZone);
				}

				if (isServer) continue;
				AudioSourceParameters audioSourceParameters =
					new AudioSourceParameters(pitch: Random.Range(0.85f, 1f));
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.GenericHit,
					transform.position,
					audioSourceParameters, sourceObj: gameObject);
			}
		}

		public void FlyingUpdateMe()
		{
			NewtonianNaNCorrection();
			if (isVisible == false)
			{
				IsFlyingSliding = false;
				airTime = 0;
				slideTime = 0;
				UpdateManager.Remove(CallbackType.EARLY_UPDATE, FlyingUpdateMe);
				return;
			}

			if (IsMoving) return;
			isFlyingSliding = true;
			MoveIsWalking = false;
			TimeSpentFlying += Time.deltaTime;

			if (this == null)
			{
				IsFlyingSliding = false;
				airTime = 0;
				slideTime = 0;
				UpdateManager.Remove(CallbackType.EARLY_UPDATE, FlyingUpdateMe);
			}

			AirTimeChecks();
			Vector3 position = transform.position;
			Vector3 newPosition = position + (NewtonianMovement.To3() * Time.deltaTime);

			if (newPosition.magnitude > 100000)
			{
				NewtonianMovement *= 0;
			}

			if (newPosition.z == -100)
			{
				DisappearFromWorld();
			}

			var intPosition = position.RoundToInt();
			var intNewPosition = newPosition.RoundToInt();
			rotationTarget.Rotate(new Vector3(0, 0, spinMagnitude * NewtonianMovement.magnitude * Time.deltaTime));
			var movetoMatrix = MatrixManager.AtPoint(newPosition.RoundToInt(), isServer).Matrix;

			if (intPosition != intNewPosition)
			{
				Hits.Clear();
				if ((position - newPosition).magnitude > 0.90f)
				{
					ProcessCollisionDetection(ref position, ref newPosition);
				}
				else
				{
					if (NewtonianMovement.magnitude > 0)
					{
						SetMatrixCache.ResetNewPosition(intPosition, registerTile);
						Pushing.Clear();
						Bumps.Clear();
						Hits.Clear();
						if (MatrixManager.IsPassableAtAllMatricesV2(intPosition,
							    intNewPosition, SetMatrixCache, this,
							    Pushing, Bumps, Hits) == false)
						{
							foreach (var bump in Bumps) //Bump Whatever we Bumped into
							{
								if (isServer == false) continue;
								if (bump as UniversalObjectPhysics == this) continue;
								bump.OnBump(this.gameObject, null);
							}

							var normal = (intPosition - intNewPosition).To3();
							if (Hits.Count == 0)
							{
								newPosition = position;
							}

							OnImpact.Invoke(this, NewtonianMovement);
							NewtonianMovement -= 2 * (NewtonianMovement * normal) * normal;
							NewtonianMovement *= ObjectBouncyness;
							spinMagnitude *= -1;
						}

						if (Pushing.Count > 0)
						{
							foreach (var push in Pushing)
							{
								if (push == this) continue;
								if (push.gameObject == thrownProtection) continue;
								push.NewtonianNewtonPush(NewtonianMovement, (NewtonianMovement.magnitude * GetWeight()),
									Single.NaN, Single.NaN, aim, thrownBy, spinMagnitude);
							}

							var normal = (intPosition - intNewPosition).To3();
							if (Hits.Count == 0)
							{
								newPosition = position;
							}

							OnImpact.Invoke(this, NewtonianMovement);
							NewtonianMovement -= 2 * (NewtonianMovement * normal) * normal;
							spinMagnitude *= -1;
							NewtonianMovement *= 0.5f;
						}
					}
				}

				if (attributes.HasComponent)
				{
					ProcessThingsToHit();
				}

				var localPosition = newPosition.ToLocal(movetoMatrix);
				InternalTriggerOnLocalTileReached(localPosition.RoundToInt());
			}

			if (isVisible == false) return;
			var cachedPosition = this.transform.position;
			SetTransform(newPosition, true);
			if (registerTile.Matrix != movetoMatrix)
			{
				SetMatrix(movetoMatrix);
			}

			if (isServer && NetworkTime.time - LastUpdateClientFlying > 2) //We only need correction for that item
			{
				LastUpdateClientFlying = NetworkTime.time;
				UpdateClientMomentum(transform.localPosition, NewtonianMovement, airTime, slideTime,
					registerTile.Matrix.Id, spinMagnitude, true, NetId.Empty, TimeSpentFlying);
			}

			if (slideTime <= 0 && airTime <= 0 && IsStickyMovement)
			{
				if (IsFloating() == false && NewtonianMovement.magnitude <= maximumStickSpeed)
				{
					//Too fast to grab onto anything
					//Stuck
					NewtonianMovement *= 0;
				}
			}

			if (NewtonianMovement.magnitude < 0.01f) //Has slowed down enough
			{
				var localPosition = transform.localPosition;
				SetLocalTarget = new Vector3WithData()
				{
					Vector3 = localPosition,
					ByClient = NetId.Empty,
					Matrix = movetoMatrix.Id
				};
				InternalTriggerOnLocalTileReached(localPosition.RoundToInt());
				if (onStationMovementsRound)
				{
					if (this is not MovementSynchronisation)
					{
						doNotApplyMomentumOnTarget = true;
					}

					if (Animating == false)
					{
						LocalTargetPosition = transform.localPosition.RoundToInt();
						Animating = true;
						MoveIsWalking = true;
						IsMoving = true;
						UpdateManager.Add(CallbackType.EARLY_UPDATE, AnimationUpdateMe);
					}
				}
				else if (ResetClientPositionReachTile)
				{
					ResetClientPositionReachTile = false;
					ResetLocationOnClients(ignoreForClient: SpecifiedClientPositionReachTile);
					SpecifiedClientPositionReachTile = NetId.Empty;
				}

				IsFlyingSliding = false;
				airTime = 0;
				slideTime = 0;
				OnThrowEnd.Invoke(this);
				thrownProtection = null;
				if (OnThrowEndResetRotation)
				{
					rotationTarget.rotation = Quaternion.Euler(0, 0, 0);
					if (this is MovementSynchronisation c)
						c.playerScript.RegisterPlayer.LayDownBehavior.EnsureCorrectState();
				}

				UpdateManager.Remove(CallbackType.EARLY_UPDATE, FlyingUpdateMe);
			}


			if (Pulling.HasComponent)
			{
				var inDirection = cachedPosition - Pulling.Component.transform.position;
				if (inDirection.magnitude > 2f && (isServer))
				{
					PullSet(null, false);
				}
				else
				{
					Pulling.Component.ProcessNewtonianPull(NewtonianMovement, newPosition);
				}
			}

			if (ObjectIsBuckling != null && ObjectIsBuckling.Pulling.HasComponent)
			{
				var inDirection = cachedPosition - ObjectIsBuckling.Pulling.Component.transform.position;
				if (inDirection.magnitude > 2f && (isServer || isOwned))
				{
					ObjectIsBuckling.PullSet(null, false); //TODO maybe remove
					if (ObjectIsBuckling.isOwned && isServer == false)
						ObjectIsBuckling.CmdStopPulling();
				}
				else
				{
					ObjectIsBuckling.Pulling.Component.ProcessNewtonianPull(NewtonianMovement,
						newPosition);
				}
			}
		}


		public void ProcessNewtonianPull(Vector2 InNewtonianMovement, Vector2 PullerPosition)
		{
			if (Animating)
			{
				localTileMoveSpeedOverride = 0;
				if (isServer)
				{
					networkedTileMoveSpeedOverride = 0;
				}

				Animating = false;
				MoveIsWalking = false;
				IsMoving = false;
				UpdateManager.Remove(CallbackType.EARLY_UPDATE, AnimationUpdateMe);
			}

			var position = this.transform.position;
			var newMove = InNewtonianMovement;
			Vector3 newPosition = Vector3.zero;
			newMove.Normalize();
			Vector3 targetFollowLocation = PullerPosition + (newMove * -1);
			if (Vector2.Distance(targetFollowLocation, transform.position) > 0.1f)
			{
				newPosition = this.MoveTowards(position, targetFollowLocation,
					(InNewtonianMovement.magnitude + 4) * Time.deltaTime);
			}
			else
			{
				newPosition = position + (InNewtonianMovement.To3() * Time.deltaTime);
			}

			//Check collision?
			SetTransform(newPosition, true);
			var movetoMatrix = MatrixManager.AtPoint(newPosition.RoundToInt(), isServer).Matrix;
			if (registerTile.Matrix != movetoMatrix)
			{
				SetMatrix(movetoMatrix);
			}

			var localPosition = (newPosition).ToLocal(movetoMatrix);
			InternalTriggerOnLocalTileReached(localPosition.RoundToInt());

			if (Pulling.HasComponent)
			{
				Pulling.Component.ProcessNewtonianPull(InNewtonianMovement, newPosition);
			}

			if (ObjectIsBuckling != null && ObjectIsBuckling.Pulling.HasComponent)
			{
				ObjectIsBuckling.Pulling.Component.ProcessNewtonianPull(InNewtonianMovement, newPosition);
			}
		}

		public bool IsFloating()
		{
			if (IsStickyMovement)
			{
				SetMatrixCache.ResetNewPosition(registerTile.WorldPosition, registerTile);
				//TODO good way to Implement
				if (MatrixManager.IsFloatingAtV2Tile(transform.position, CustomNetworkManager.IsServer,
					    SetMatrixCache))
				{
					if (MatrixManager.IsFloatingAtV2Objects(ContextGameObjects, registerTile.WorldPosition,
						    CustomNetworkManager.IsServer, SetMatrixCache))
					{
						IsCurrentlyFloating = true;
						return true;
					}
				}

				IsCurrentlyFloating = false;
				return false;
			}
			else
			{
				if (registerTile.Matrix.HasGravity || HasOwnGravity) //Presuming Register tile has the correct matrix
				{
					if (registerTile.Matrix.MetaTileMap.IsEmptyTileMap(registerTile.LocalPosition) == false)
					{
						IsCurrentlyFloating = false;
						return false;
					}
				}

				IsCurrentlyFloating = true;
				return true;
			}
		}

		private void InternalTriggerOnLocalTileReached(Vector3 localPos)
		{
			//Can truncate as the localPos should be near enough to the int value
			var rounded = localPos.RoundToInt();

			if (TransformState.HiddenPos != localPos)
			{
				OnLocalTileReached.Invoke(oldLocalTilePosition, rounded);
			}


			oldLocalTilePosition = rounded;
			SetRegisterTileLocation(rounded);
			if (isServer == false)
			{
				ClientTileReached(rounded);
			}
			else
			{
				LocalServerTileReached(rounded);
			}
		}


		public virtual void ClientTileReached(Vector3Int localPos)
		{
		}

		public virtual void LocalServerTileReached(Vector3Int localPos)
		{
			if (doStepInteractions == false) return;
			if (localPos == TransformState.HiddenPos) return;

			var matrix = registerTile.Matrix;
			if (matrix == null) return;

			var tile = matrix.MetaTileMap.GetTile(localPos, LayerType.Base);
			if (tile != null && tile is BasicTile c)
			{
				foreach (var interaction in c.TileStepInteractions)
				{
					if (interaction.WillAffectObject(gameObject) == false) continue;
					interaction.OnObjectEnter(gameObject);
				}
			}

			//Check for tiles before objects because of this list
			if (matrix.MetaTileMap.ObjectLayer.EnterTileBaseList == null) return;
			var loopTo = matrix.MetaTileMap.ObjectLayer.EnterTileBaseList.Get(localPos);
			foreach (var enterTileBase in loopTo)
			{
				if (enterTileBase.WillAffectObject(gameObject) == false) continue;
				enterTileBase.OnObjectEnter(gameObject);
			}
		}

		//--Handles--
		//pushing
		//IS Gravity
		//space movement/Slipping
		//Pulling

		public static float SizeToWeight(Size size)
		{
			return size switch
			{
				Size.None => 0,
				Size.Tiny => 0.85f,
				Size.Small => 1f,
				Size.Medium => 3f,
				Size.Large => 5f,
				Size.Massive => 10f,
				Size.Humongous => 50f,
				_ => 1f
			};
		}

		public class ForceEventWithChange : UnityEvent<UniversalObjectPhysics, Vector2>
		{
		}

		public class ForceEvent : UnityEvent<UniversalObjectPhysics>
		{
		}

		public struct Vector3WithData : IEquatable<Vector3WithData>
		{
			public Vector3 Vector3;
			public uint ByClient;
			public int Matrix;
			public float Speed;

			public bool Equals(Vector3WithData other) =>
				Equals(Vector3, other.Vector3)
				&& ByClient == other.ByClient
				&& Matrix == other.Matrix
				&& Speed == other.Speed;

			public override bool Equals(object obj)
			{
				return obj is Vector3WithData other && Equals(other);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(Vector3, ByClient, Matrix, Speed);
			}
		}
	}
}