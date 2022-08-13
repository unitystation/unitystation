using System;
using System.Collections.Generic;
using AdminCommands;
using Core.Editor.Attributes;
using HealthV2;
using Items;
using JetBrains.Annotations;
using Messages.Server.SoundMessages;
using Mirror;
using Objects;
using Objects.Construction;
using Tiles;
using UI.Action;
using UnityEngine;
using UnityEngine.Events;
using Util;
using Random = UnityEngine.Random;

public class UniversalObjectPhysics : NetworkBehaviour, IRightClickable, IRegisterTileInitialised
{
	//TODO parentContainer Need to test
	//TODO Maybe work on conveyor belts and players a bit more
	//TODO Sometime Combine buckling and object storage
	//=================================== Maybe
	//TODO move IsCuffed to PlayerOnlySyncValues maybe?
	//=================================== balance Maybe
	//=============================================== TODO some time
	//TODO Make space Movement perfect ( Is pretty much good now )
	//TODO after thrown not synchronised Properly need to synchronise rotation
	//TODO When throwing rotation Direction needs to be set by server
	//=============================================== Definitely
	//TODO Smooth pushing, syncvar well if Statements in force and stuff On process player action,  Smooth resetting if Space wind

	public const float DEFAULT_PUSH_SPEED = 6;

	/// <summary>
	/// Maximum speed player can reach by throwing stuff in space
	/// </summary>
	public const float MAX_SPEED = 25;

	public const int HIGH_SPEED_COLLISION_THRESHOLD = 13;


	//public const float DEFAULT_Friction = 9999999999999f;
	public const float DEFAULT_Friction = 15f;
	public const float DEFAULT_SLIDE_FRICTION = 9f;

	public bool DEBUG = false;

	[PrefabModeOnly] public bool SnapToGridOnStart = false;

	private BoxCollider2D Collider; //TODO Checked component

	private float TileMoveSpeedOverride = 0;


	private FloorDecal floorDecal; // Used to make sure some objects are not causing gravity

	[PlayModeOnly] public Vector3 LocalTargetPosition;

	protected Rotatable rotatable;

	[PrefabModeOnly] public bool ChangesDirectionPush = false;

	[PrefabModeOnly] public bool Intangible = false;

	public bool CanBeWindPushed = true;




	[PrefabModeOnly] public bool IsPlayer = false;

	public Vector3WithData SetLocalTarget
	{
		set
		{
			if (CustomNetworkManager.IsServer && synchLocalTargetPosition.Vector3 != value.Vector3)
			{
				SyncLocalTarget(synchLocalTargetPosition, value);
			}

			//Logger.LogError("local target position set of "  + value + " On matrix " + registerTile.Matrix);
			LocalTargetPosition = value.Vector3;
		}
	}

	public Vector3 OfficialPosition
	{
		get
		{
			if (ContainedInContainer != null)
			{
				return ContainedInContainer.registerTile.ObjectPhysics.Component.OfficialPosition;
			}
			else if (pickupable.HasComponent && pickupable.Component.ItemSlot != null)
			{
				return pickupable.Component.ItemSlot.ItemStorage.gameObject.AssumedWorldPosServer();
			}
			else
			{
				return transform.position;
			}
		}
	}

	private Vector3Int oldLocalTilePosition;

	public CheckedComponent<Pickupable> pickupable = new CheckedComponent<Pickupable>();

	public bool InitialLocationSynchronised;

	public bool IsVisible => isVisible;

	[SyncVar(hook = nameof(SyncMovementSpeed))]
	public float tileMoveSpeed = 1;

	public float TileMoveSpeed => tileMoveSpeed;

	[SyncVar(hook = nameof(SyncLocalTarget))]
	private Vector3WithData synchLocalTargetPosition;

	public Vector3 SynchLocalTargetPosition => synchLocalTargetPosition.Vector3;

	[SyncVar(hook = nameof(SynchronisedoNotApplyMomentumOnTarget))]
	private bool doNotApplyMomentumOnTarget = false;

	[SyncVar(hook = nameof(SynchroniseVisibility))]
	private bool isVisible = true;

	[SyncVar(hook = nameof(SyncIsNotPushable))]
	public bool isNotPushable;

	public bool IsNotPushable => isNotPushable;

	public bool CanMove => isNotPushable == false && IsBuckled == false;

	[SyncVar(hook = nameof(SynchroniseUpdatePulling))]
	private PullData ThisPullData;

	[SyncVar(hook = nameof(SynchroniseParent))]
	private uint parentContainer;

	[SyncVar]
	protected int SetTimestampID = -1;

	[SyncVar]
	protected int SetLastResetID = -1;

	[SyncVar]
	public bool HasOwnGravity = false;

	private ObjectContainer CachedContainedInContainer;

	public ObjectContainer ContainedInContainer
	{
		get
		{
			if (parentContainer is NetId.Invalid or NetId.Empty)
			{
				return null;
			}

			if (CachedContainedInContainer == null)
			{
				var spawned =
					CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;
				if (spawned.TryGetValue(parentContainer, out var net))
				{
					CachedContainedInContainer = net.GetComponent<ObjectContainer>();
				}
				else
				{
					CachedContainedInContainer = null;
				}
			}
			else
			{
				if (CachedContainedInContainer.registerTile.netId != parentContainer)
				{
					var spawned =
						CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;
					if (spawned.TryGetValue(parentContainer, out var net))
					{
						CachedContainedInContainer = net.GetComponent<ObjectContainer>();
					}
					else
					{
						CachedContainedInContainer = null;
					}
				}
			}

			return CachedContainedInContainer;
		}
	}

	[PlayModeOnly] private Vector2 newtonianMovement; //* attributes.Size -> weight


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

	[PlayModeOnly] public float airTime; //Cannot grab onto anything so no friction

	[PlayModeOnly] public float slideTime;
	//Reduced friction during this time, if stickyMovement Just has normal friction vs just grabbing
	[PlayModeOnly] public bool SetIgnoreSticky = false;

	[PlayModeOnly] public GameObject thrownBy;

	[PlayModeOnly] public BodyPartType aim;

	[PlayModeOnly] public float spinMagnitude = 0;

	[PlayModeOnly] public int ForcedPushedFrame = 0;
	[PlayModeOnly] public int TryPushedFrame = 0;
	[PlayModeOnly] public int PushedFrame = 0;
	[PlayModeOnly] public bool FramePushDecision = true;

	[PrefabModeOnly] public bool stickyMovement = false;
	//If this thing likes to grab onto stuff such as like a player


	public bool IsStickyMovement => stickyMovement && SetIgnoreSticky == false;

	[PrefabModeOnly] public bool OnThrowEndResetRotation;


	[PrefabModeOnly] public float maximumStickSpeed = 1.5f;
	//Speed In tiles per second that, The thing would able to be stop itself if it was sticky

	[PrefabModeOnly] public bool onStationMovementsRound;

	[HideInInspector] public CheckedComponent<Attributes> attributes = new CheckedComponent<Attributes>();

	[HideInInspector] public RegisterTile registerTile;

	protected LayerMask defaultInteractionLayerMask;

	[HideInInspector] public GameObject[] ContextGameObjects = new GameObject[2];

	[PlayModeOnly] public bool IsCurrentlyFloating;


	[PlayModeOnly]
	// TODO: Bod this is not what CheckedComponent is for as the reference is not on the same object as this script - Dan
	public CheckedComponent<UniversalObjectPhysics> Pulling = new CheckedComponent<UniversalObjectPhysics>();

	[PlayModeOnly]
	// TODO: Bod this is not what CheckedComponent is for as the reference is not on the same object as this script - Dan
	public CheckedComponent<UniversalObjectPhysics> PulledBy = new CheckedComponent<UniversalObjectPhysics>();

	protected bool doStepInteractions = true;

	#region Events

	[PlayModeOnly] public ForceEvent OnThrowStart = new ForceEvent();

	[PlayModeOnly] public ForceEventWithChange OnImpact = new ForceEventWithChange();

	[PlayModeOnly] public DualVector3IntEvent OnLocalTileReached = new DualVector3IntEvent();

	[PlayModeOnly] public ForceEvent OnThrowEnd = new ForceEvent();

	#endregion


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
	}

	public Vector3 CalculateLocalPosition()
	{
		if (isServer)
		{
			return transform.localPosition;
		}
		else
		{
			if (InitialLocationSynchronised == false)
			{
				InitialLocationSynchronised = true;
				return synchLocalTargetPosition.Vector3;
			}

			return transform.localPosition;
		}
	}

	public void Start()
	{
		if (isServer)
		{
			SetLocalTarget = new Vector3WithData()
			{
				Vector3 = transform.localPosition,
				ByClient = NetId.Empty
			};
		}
		else
		{
			SetRegisterTileLocation(synchLocalTargetPosition.Vector3.RoundToInt());
			SetTransform(synchLocalTargetPosition.Vector3, false);
		}

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

	public virtual void OnEnable() { }

	public virtual void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, FlyingUpdateMe);
		UpdateManager.Remove(CallbackType.UPDATE, AnimationUpdateMe);
		UpdateManager.Remove(CallbackType.UPDATE, FloatingCourseCorrection);
	}

	public struct PullData : IEquatable<PullData>
	{
		public UniversalObjectPhysics NewPulling;
		public bool WasCausedByClient;

		public override bool Equals(object obj)
		{
			return obj is PullData other && Equals(other);
		}

		public bool Equals(PullData other)
		{
			return Equals(NewPulling, other.NewPulling) && WasCausedByClient == other.WasCausedByClient;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(NewPulling, WasCausedByClient);
		}
	}


	public struct Vector3WithData : IEquatable<Vector3WithData>
	{
		public Vector3 Vector3;
		public uint ByClient;


		public bool Equals(Vector3WithData other) => Equals(Vector3, other.Vector3) && ByClient == other.ByClient;

		public override bool Equals(object obj)
		{
			return obj is Vector3WithData other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Vector3, ByClient);
		}
	}

	public void SyncMovementSpeed(float old, float newMove)
	{
		tileMoveSpeed = newMove;
	}


	public void SyncLocalTarget(Vector3WithData oldLocalTarget, Vector3WithData newLocalTarget)
	{
		synchLocalTargetPosition = newLocalTarget;
		if (isServer) return;
		if (LocalTargetPosition == newLocalTarget.Vector3) return;
		if (isLocalPlayer || PulledBy.HasComponent) return;

		var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;
		if (newLocalTarget.ByClient != NetId.Empty && newLocalTarget.ByClient != NetId.Invalid
				&& spawned.TryGetValue(newLocalTarget.ByClient, out var local)
				&& local.gameObject == PlayerManager.LocalPlayerObject) return;

		SetLocalTarget = newLocalTarget;

		if (IsFlyingSliding)
		{
			IsFlyingSliding = false;
			airTime = 0;
			slideTime = 0;
			UpdateManager.Remove(CallbackType.UPDATE, FlyingUpdateMe);
		}

		if (Animating == false && transform.localPosition != newLocalTarget.Vector3)
			UpdateManager.Add(CallbackType.UPDATE, AnimationUpdateMe);
	}


	public void StoreTo(ObjectContainer newParent)
	{
		if (newParent.OrNull()?.gameObject == this.gameObject)
		{
			Chat.AddGameWideSystemMsgToChat(
				" Something doesn't feel right? **BBBBBBBBBBBBBOOOOOOOOOOOOOOOOOOOMMMMMMMMMMMEMMEEEEE** ");
			Logger.LogError("Tried to store object within itself");
			return; //Storing something inside of itself what?
		}

		PullSet(null,false); //Presume you can't Pulling stuff inside container
		//TODO Handle non-disappearing containers like Cart riding

		if (newParent == null)
		{
			parentContainer = NetId.Empty;
		}
		else
		{
			parentContainer = newParent.registerTile.netId;
		}

		CachedContainedInContainer = newParent;
		if (newParent == null)
		{
			SynchroniseVisibility(isVisible, true);
		}
		else
		{
			SynchroniseVisibility(isVisible, false);
		}
	}
	//TODO Sometime Handle stuff like cart riding
	//would be basically just saying with updates with an offset to the thing that parented to the object


	[ClientRpc]
	public void RPCClientTilePush(Vector2Int worldDirection, float speed, GameObject causedByClient, bool overridePull,
		int timestampID)
	{
		SetTimestampID = timestampID;
		if (isServer) return;
		if (PlayerManager.LocalPlayerObject == causedByClient) return;
		if (PulledBy.HasComponent && overridePull == false) return;

		Pushing.Clear();
		SetMatrixCache.ResetNewPosition(transform.position, registerTile);
		ForceTilePush(worldDirection, Pushing, causedByClient, speed);
	}


	[ClientRpc]
	public void UpdateClientMomentum(Vector3 resetToLocal, Vector2 newMomentum, float inairTime, float inslideTime,
		int matrixID, float inSpinFactor, bool forceOverride, uint doNotUpdateThisClient)
	{
		if (isServer) return;

		var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;
		if (doNotUpdateThisClient != NetId.Empty && doNotUpdateThisClient != NetId.Invalid
			&& spawned.TryGetValue(doNotUpdateThisClient, out var local)
			&& local.gameObject == PlayerManager.LocalPlayerObject) return;


		//if (isLocalPlayer) return; //We are updating other Objects than the player on the client //TODO Also block if being pulled by local player //Why we need this?
		newtonianMovement = newMomentum;
		airTime = inairTime;
		slideTime = inslideTime;
		spinMagnitude = inSpinFactor;
		SetMatrix(MatrixManager.Get(matrixID).Matrix);

		SetRegisterTileLocation(resetToLocal.RoundToInt());

		if (IsFlyingSliding) //If we flying try and smooth it
		{
			LocalDifferenceNeeded = resetToLocal - transform.localPosition;
			if (CorrectingCourse == false)
			{
				CorrectingCourse = true;
				UpdateManager.Add(CallbackType.UPDATE, FloatingCourseCorrection);
			}
		}
		else //We are walking around somewhere
		{
			SetLocalTarget = new Vector3WithData()
			{
				Vector3 = resetToLocal,
				ByClient = NetId.Empty
			};
			SetTransform(resetToLocal, false);
		}

		if (Animating == false && IsFlyingSliding == false)
		{
			IsFlyingSliding = true;
			UpdateManager.Add(CallbackType.UPDATE, FlyingUpdateMe);
		}
	}

	private void SynchronisedoNotApplyMomentumOnTarget(bool oldVariable, bool newVariable)
	{
		doNotApplyMomentumOnTarget = newVariable;
	}

	private void SynchroniseVisibility(bool oldVisibility, bool newVisibility)
	{
		isVisible = newVisibility;
		if (isVisible)
		{
			var sprites = GetComponentsInChildren<SpriteRenderer>();
			foreach (var sprite in sprites)
			{
				sprite.enabled = true;
			}
		}
		else
		{
			var sprites = GetComponentsInChildren<SpriteRenderer>();
			foreach (var sprite in sprites)
			{
				sprite.enabled = false;
			}

			SetTransform(TransformState.HiddenPos, false);
			SetRegisterTileLocation(TransformState.HiddenPos);
			ResetEverything();
		}
	}

	private void SynchroniseParent(uint oldPulling, uint newPulling)
	{
		parentContainer = newPulling;
		//Visibility is handled by Server with Synchronised var
	}

	public void SynchroniseUpdatePulling(PullData oldPullData, PullData newPulling)
	{
		ThisPullData = newPulling;
		if (newPulling.WasCausedByClient && isLocalPlayer) return;
		PullSet(newPulling.NewPulling, false, true);
	}

	public void AppearAtWorldPositionServer(Vector3 worldPos, bool smooth = false, bool doStepInteractions = true)
	{
		this.doStepInteractions = doStepInteractions;

		SynchroniseVisibility(isVisible, true);
		var matrix = MatrixManager.AtPoint(worldPos, isServer);
		ForceSetLocalPosition(worldPos.ToLocal(matrix), Vector2.zero, smooth, matrix.Id);

		this.doStepInteractions = true;
	}

	public void DropAtAndInheritMomentum(UniversalObjectPhysics droppedFrom)
	{
		SynchroniseVisibility(isVisible, true);
		ForceSetLocalPosition(droppedFrom.transform.localPosition, droppedFrom.newtonianMovement, false,
			droppedFrom.registerTile.Matrix.Id);
	}

	public void DisappearFromWorld()
	{
		SynchroniseVisibility(isVisible, false);
	}

	public void ForceSetLocalPosition(Vector3 resetToLocal, Vector2 momentum, bool smooth, int matrixID,
		bool updateClient = true, float rotation = 0, NetworkConnection client = null, int resetID = -1 )
	{
		transform.localRotation = Quaternion.Euler(new Vector3(0, 0, rotation));
		if (isServer && updateClient)
		{
			isVisible = true;
			if (client != null)
			{
				if (resetID == -1)
				{
					resetID = Time.frameCount;
					SetLastResetID = resetID;
				}

				RPCForceSetPosition(client, resetToLocal, momentum, smooth, matrixID, rotation, resetID);
			}
			else
			{
				if (resetID == -1)
				{
					resetID = Time.frameCount;
					SetLastResetID = resetID;
				}

				RPCForceSetPosition(resetToLocal, momentum, smooth, matrixID, rotation, resetID);
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
				newtonianMovement = momentum;
				LocalDifferenceNeeded = resetToLocal - transform.localPosition;
				SetRegisterTileLocation(resetToLocal.RoundToInt());
				if (CorrectingCourse == false)
				{
					CorrectingCourse = true;
					UpdateManager.Add(CallbackType.UPDATE, FloatingCourseCorrection);
				}
			}
			else
			{
				newtonianMovement = momentum;

				SetLocalTarget = new Vector3WithData()
				{
					Vector3 = resetToLocal,
					ByClient = NetId.Empty
				};
				SetRegisterTileLocation(resetToLocal.RoundToInt());

				if (Animating == false)
				{
					Animating = true;
					UpdateManager.Add(CallbackType.UPDATE, AnimationUpdateMe);
				}
			}
		}
		else
		{
			if (IsFlyingSliding)
			{
				newtonianMovement = momentum;
				SetTransform(resetToLocal, false);
				SetRegisterTileLocation(resetToLocal.RoundToInt());
			}
			else
			{
				newtonianMovement = momentum;
				SetLocalTarget = new Vector3WithData()
				{
					Vector3 = resetToLocal,
					ByClient = NetId.Empty
				};
				SetTransform(resetToLocal, false);
				SetRegisterTileLocation(resetToLocal.RoundToInt());
			}
		}
	}

	[ClientRpc]
	public void RPCForceSetPosition(Vector3 resetToLocal, Vector2 momentum, bool smooth, int matrixID, float rotation, int resetID)
	{
		ForceSetLocalPosition(resetToLocal, momentum, smooth, matrixID, false, rotation, resetID: resetID);
	}

	[TargetRpc]
	public void RPCForceSetPosition(NetworkConnection target, Vector3 resetToLocal, Vector2 momentum, bool smooth,
		int matrixID, float rotation, int resetID)
	{
		ForceSetLocalPosition(resetToLocal, momentum, smooth, matrixID, false, rotation, resetID: resetID);
	}


	//Warning only update clients!!
	public void ResetLocationOnClients(bool smooth = false)
	{

		SetLastResetID = Time.frameCount;
		RPCForceSetPosition(transform.localPosition, newtonianMovement, smooth, registerTile.Matrix.Id,
			transform.localRotation.eulerAngles.z, SetLastResetID);

		if (Pulling.HasComponent)
		{
			Pulling.Component.ResetLocationOnClients(smooth);
		}

		if (ObjectIsBucklingChecked.HasComponent && ObjectIsBucklingChecked.Component.Pulling.HasComponent)
		{
			ObjectIsBucklingChecked.Component.Pulling.Component.ResetLocationOnClients(smooth);
		}
		//Update client to server state
	}

	//Warning only update client!!

	public void ResetLocationOnClient(NetworkConnection client, bool smooth = false)
	{
		isVisible = true;
		SetLastResetID = Time.frameCount;
		RPCForceSetPosition(client, transform.localPosition, newtonianMovement, smooth, registerTile.Matrix.Id,
			transform.localRotation.eulerAngles.z, SetLastResetID);

		if (Pulling.HasComponent)
		{
			Pulling.Component.ResetLocationOnClient(client, smooth);
		}
		//Update client to server state
	}

	[PlayModeOnly] public Vector2 LocalDifferenceNeeded;

	[PlayModeOnly] public bool CorrectingCourse = false;


	public void FloatingCourseCorrection()
	{
		if (this == null)
		{
			UpdateManager.Remove(CallbackType.UPDATE, FloatingCourseCorrection);
			return;
		}

		CorrectingCourse = true;
		var position = transform.localPosition;
		var newPosition = this.MoveTowards(position, (position + LocalDifferenceNeeded.To3()),
			(newtonianMovement.magnitude + 4) * Time.deltaTime);
		LocalDifferenceNeeded -= (newPosition - position).To2();
		SetTransform(newPosition, false);

		if (LocalDifferenceNeeded.magnitude < 0.01f)
		{
			CorrectingCourse = false;
			UpdateManager.Remove(CallbackType.UPDATE, FloatingCourseCorrection);
		}
	}


	[HideInInspector] public List<UniversalObjectPhysics> Pushing = new List<UniversalObjectPhysics>();

	[HideInInspector] public List<IBumpableObject> Bumps = new List<IBumpableObject>();

	[HideInInspector] public List<UniversalObjectPhysics> Hits = new List<UniversalObjectPhysics>();

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

	public void SetIsNotPushable(bool newState)
	{
		isNotPushable = newState;

		//Force update atmos
		registerTile.Matrix.TileChangeManager.SubsystemManager.UpdateAt(OfficialPosition.ToLocalInt(registerTile.Matrix));
	}

	private void SyncIsNotPushable(bool wasNotPushable, bool isNowNotPushable)
	{
		if (isNowNotPushable && PulledBy.HasComponent)
		{
			PulledBy.Component.StopPulling(false);
		}

		isNotPushable = isNowNotPushable;
	}


	public void SetMatrix(Matrix movetoMatrix)
	{
		if (movetoMatrix == null) return;
		if (registerTile == null)
		{
			Logger.LogError("null Register tile on " + this.name);
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
		SetLocalTarget = new Vector3WithData()
		{
			Vector3 = transform.localPosition,
			ByClient = NetId.Empty
		};
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

	public bool CanPush(Vector2Int worldDirection)
		//NOTE: It's presumed that If true one time the rest universal physics objects will return true to , manually checks for isNotPushable
	{
		if (worldDirection == Vector2Int.zero) return true;
		if (CanMove == false) return false;
		if (PushedFrame == Time.frameCount)
		{
			return FramePushDecision;
		}
		else if (TryPushedFrame == Time.frameCount)
		{
			return false;
		}

		TryPushedFrame = Time.frameCount;
		//TODO Secured stuff
		Pushing.Clear();
		Bumps.Clear();
		var from = transform.position;
		if (IsMoving) //We are moving combined targets
		{
			from = LocalTargetPosition.ToWorld(registerTile.Matrix);
		}


		SetMatrixCache.ResetNewPosition(from, registerTile);


		if (MatrixManager.IsPassableAtAllMatricesV2(from,
			    from + worldDirection.To3Int(), SetMatrixCache, this,
			    Pushing, Bumps)) //Validate
		{
			PushedFrame = Time.frameCount;
			FramePushDecision = true;
			return true;
		}
		else
		{
			PushedFrame = Time.frameCount;
			FramePushDecision = false;
			return false;
		}
	}

	public void TryTilePush(Vector2Int worldDirection, GameObject byClient, float speed = Single.NaN,
		UniversalObjectPhysics pushedBy = null, bool overridePull = false, UniversalObjectPhysics pulledBy = null)
	{
		if (isVisible == false) return;
		if (pushedBy == this) return;
		if (CanPush(worldDirection))
		{
			ForceTilePush(worldDirection, Pushing, byClient, speed, pushedBy: pushedBy, overridePull: overridePull, pulledBy : pulledBy);
		}
	}

	public void ForceTilePush(Vector2Int worldDirection, List<UniversalObjectPhysics> inPushing, GameObject byClient,
		float speed = Single.NaN, bool isWalk = false,
		UniversalObjectPhysics pushedBy = null, bool overridePull = false, UniversalObjectPhysics pulledBy = null) //PushPull TODO Change to physics object
	{
		if (isVisible == false) return;
		if (ForcedPushedFrame == Time.frameCount)
		{
			return;
		}

		ForcedPushedFrame = Time.frameCount;
		if (CanMove == false) return;
		//Nothing is pushing this (  mainly because I left it on player And forgot to turn it off ), And it was hard to tell it was on

		if (PulledBy.HasComponent && pulledBy == null) //Something else pushed it/It pushed itself so cancel pull
		{
			PulledBy.Component.PullSet(null, false);
		}

		doNotApplyMomentumOnTarget = false;
		if (float.IsNaN(speed))
		{
			speed = TileMoveSpeed;
		}

		if (inPushing.Count > 0 && registerTile.IsPassable(isServer) == false) //Has to push stuff
		{
			//Push Object
			foreach (var push in inPushing)
			{
				if (push == this) continue;
				if (push == pushedBy) continue;
				if (push.Intangible) continue;
				if (Pulling.HasComponent && Pulling.Component == push) continue;
				if (PulledBy.HasComponent && PulledBy.Component == push) continue;
				if (pushedBy == null)
				{
					pushedBy = this;
				}

				var pushDirection = -1 * (this.transform.position - push.transform.position).RoundTo2Int();
				if (pushDirection == Vector2Int.zero)
				{
					pushDirection = worldDirection;
				}

				push.TryTilePush(pushDirection, byClient, speed, pushedBy);
			}
		}

		var cachedPosition = transform.position;
		if (IsMoving) //We are moving combined targets
		{
			cachedPosition = LocalTargetPosition.ToWorld(registerTile.Matrix);
		}

		var newWorldPosition = cachedPosition + worldDirection.To3Int();

		if ((newWorldPosition - transform.position).magnitude > 1.45f) return; //Is pushing too far

		var movetoMatrix = SetMatrixCache.GetforDirection(worldDirection.To3Int()).Matrix;


		if (registerTile.Matrix != movetoMatrix)
		{
			SetMatrix(movetoMatrix);
		}

		if (ChangesDirectionPush)
		{
			rotatable.OrNull()?.SetFaceDirectionLocalVector(worldDirection);
		}


		var localPosition = (newWorldPosition).ToLocal(movetoMatrix);

		SetRegisterTileLocation(localPosition.RoundToInt());

		SetLocalTarget = new Vector3WithData()
		{
			Vector3 = localPosition.RoundToInt(),
			ByClient = byClient.NetId()
		};


		MoveIsWalking = isWalk;


		if (Animating == false)
		{
			Animating = true;
			UpdateManager.Add(CallbackType.UPDATE, AnimationUpdateMe);
			TileMoveSpeedOverride = speed;
		}

		if (isServer && (PulledBy.HasComponent == false || overridePull))
		{
			SetTimestampID = Time.frameCount;
			RPCClientTilePush(worldDirection, speed, byClient, overridePull, SetTimestampID); //TODO Local direction
		}


		if (Pulling.HasComponent)
		{
			var inDirection = cachedPosition - Pulling.Component.transform.position;
			if (inDirection.magnitude > 2f && (isServer || isLocalPlayer))
			{
				PullSet(null, false); //TODO maybe remove
				if (isLocalPlayer && isServer == false) CmdStopPulling();
			}
			else
			{
				Pulling.Component.TryTilePush(inDirection.NormalizeTo2Int(), byClient, speed, pushedBy,  pulledBy : this);
			}
		}

		if (ObjectIsBucklingChecked.HasComponent && ObjectIsBucklingChecked.Component.Pulling.HasComponent)
		{
			var inDirection = cachedPosition - ObjectIsBucklingChecked.Component.Pulling.Component.transform.position;
			if (inDirection.magnitude > 2f && (isServer || isLocalPlayer))
			{
				ObjectIsBucklingChecked.Component.PullSet(null, false); //TODO maybe remove
				if (ObjectIsBucklingChecked.Component.isLocalPlayer && isServer == false) ObjectIsBucklingChecked.Component.CmdStopPulling();
			}
			else
			{
				ObjectIsBucklingChecked.Component.Pulling.Component.TryTilePush(inDirection.NormalizeTo2Int(), byClient, speed, pushedBy);
			}
		}
	}

	public void ResetEverything()
	{
		if (IsFlyingSliding) UpdateManager.Remove(CallbackType.UPDATE, FlyingUpdateMe);
		if (Animating) UpdateManager.Remove(CallbackType.UPDATE, AnimationUpdateMe);
		if (CorrectingCourse) UpdateManager.Remove(CallbackType.UPDATE, FloatingCourseCorrection);

		IsMoving = false;
		MoveIsWalking = false;
		SetLocalTarget = new Vector3WithData()
		{
			Vector3 = transform.localPosition,
			ByClient = NetId.Empty
		};
		newtonianMovement = Vector2.zero;
		airTime = 0;
		slideTime = 0;
		IsFlyingSliding = false;
		Animating = false;
	}

	[Server]
	public void ForceDrop(Vector3 pos)
	{
		Vector2 impulse = Random.insideUnitCircle.normalized;
		var matrix = MatrixManager.AtPoint(pos, isServer);
		var localPos = pos.ToLocal(matrix);
		ForceSetLocalPosition(localPos, impulse * Random.Range(0.2f, 2f), false, matrix.Id);
	}

	public void CheckMatrixSwitch()
	{
		var newMatrix =
			MatrixManager.AtPoint(transform.position, true, registerTile.Matrix.MatrixInfo);
		if (registerTile.Matrix.Id != newMatrix.Id)
		{
			var worldPOS = transform.position;
			SetMatrix(newMatrix.Matrix);
			SetRegisterTileLocation(worldPOS.ToLocal(newMatrix.Matrix).RoundToInt());
			ResetLocationOnClients();
		}
	}

	public void NewtonianNewtonPush(Vector2 worldDirection, float newtonians = Single.NaN, float inAirTime = Single.NaN,
		float inSlideTime = Single.NaN, BodyPartType inAim = BodyPartType.Chest, GameObject inThrownBy = null,
		float spinFactor = 0) //Collision is just naturally part of Newtonian push
	{
		var speed = newtonians / SizeToWeight(GetSize());
		NewtonianPush(worldDirection, speed, inAirTime, inSlideTime, inAim, inThrownBy, spinFactor);
	}

	public void PullSet(UniversalObjectPhysics toPull, bool byClient, bool synced = false)
	{
		if (toPull != null && ContainedInContainer != null) return; //Can't pull stuff inside of objects)

		if (isServer && synced == false)
			SynchroniseUpdatePulling(ThisPullData, new PullData() {NewPulling = toPull, WasCausedByClient = byClient});

		if (toPull != null)
		{
			if (PulledBy.HasComponent)
			{
				if (toPull == PulledBy.Component)
				{
					PulledBy.Component.PullSet(null, false);
				}
			}

			if (Pulling.HasComponent)
			{
				Pulling.Component.PulledBy.SetToNull();
				Pulling.SetToNull();
				ContextGameObjects[1] = null;
			}

			if (toPull.IsNotPushable) return;

			Pulling.DirectSetComponent(toPull);
			toPull.PulledBy.DirectSetComponent(this);
			ContextGameObjects[1] = toPull.gameObject;
			if (isLocalPlayer) UIManager.Action.UpdatePullingUI(true);
		}
		else
		{
			if (isLocalPlayer) UIManager.Action.UpdatePullingUI(false);
			if (Pulling.HasComponent)
			{
				Pulling.Component.PulledBy.SetToNull();
				Pulling.SetToNull();
				ContextGameObjects[1] = null;
			}
		}
	}


	public void NewtonianPush(Vector2 worldDirection, float speed, float nairTime = Single.NaN,
		float inSlideTime = Single.NaN, BodyPartType inAim = BodyPartType.Chest, GameObject inThrownBy = null,
		float spinFactor = 0, GameObject doNotUpdateThisClient = null,
		bool ignoreSticky = false) //Collision is just naturally part of Newtonian push
	{

		if (isVisible == false) return;
		if (CanMove == false) return;
		if (PulledBy.HasComponent) return;

		aim = inAim;
		thrownBy = inThrownBy;

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
		if (newtonianMovement.magnitude > 0.01f)
		{
			//It's moving add to update manager
			if (IsFlyingSliding == false)
			{
				IsFlyingSliding = true;
				UpdateManager.Add(CallbackType.UPDATE, FlyingUpdateMe);
			}
		}

		if (isServer)
		{
			LastUpdateClientFlying = NetworkTime.time;
			UpdateClientMomentum(transform.localPosition, newtonianMovement, airTime, this.slideTime,
				registerTile.Matrix.Id, spinFactor, true, doNotUpdateThisClient.NetId());
		}
	}

	public void AppliedFriction(float frictionCoefficient)
	{
		var speedLossDueToFriction = frictionCoefficient * Time.deltaTime;

		var oldMagnitude = newtonianMovement.magnitude;

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

	[PlayModeOnly] public bool Animating = false;

	[PlayModeOnly] private Vector3 LastDifference = Vector3.zero;

	public void AnimationUpdateMe()
	{
		if (isVisible == false)
		{
			MoveIsWalking = false;
			IsMoving = false;
			Animating = false;
			UpdateManager.Remove(CallbackType.UPDATE, AnimationUpdateMe);
			return;
		}

		if (this == null || transform == null)
		{
			MoveIsWalking = false;
			IsMoving = false;
			Animating = false;
			UpdateManager.Remove(CallbackType.UPDATE, AnimationUpdateMe);
		}

		Animating = true;
		var localPos = transform.localPosition;

		IsMoving = (localPos - LocalTargetPosition).magnitude > 0.001;
		if (IsMoving)
		{
			if (TileMoveSpeedOverride > 0)
			{
				SetTransform(this.MoveTowards(localPos, LocalTargetPosition,
					TileMoveSpeedOverride * Time.deltaTime), false); //* transform.localPosition.SpeedTo(targetPos)
			}
			else
			{
				SetTransform(this.MoveTowards(localPos, LocalTargetPosition,
					TileMoveSpeed * Time.deltaTime), false);
			}

			LastDifference = transform.localPosition - localPos;
		}
		else
		{
			var cache = TileMoveSpeedOverride;
			if (TileMoveSpeedOverride == 0)
			{
				cache = TileMoveSpeed;
			}

			TileMoveSpeedOverride = 0;
			Animating = false;

			InternalTriggerOnLocalTileReached(transform.localPosition);
			MoveIsWalking = false;

			if (IsFloating() && PulledBy.HasComponent == false && doNotApplyMomentumOnTarget == false)
			{

				NewtonianMovement += (Vector2) LastDifference.normalized * cache;
				LastDifference = Vector3.zero;
			}

			doNotApplyMomentumOnTarget = false;

			UpdateManager.Remove(CallbackType.UPDATE, AnimationUpdateMe);

			if (newtonianMovement.magnitude > 0.01f)
			{
				//It's moving add to update manager
				if (IsFlyingSliding == false)
				{
					IsFlyingSliding = true;
					UpdateManager.Add(CallbackType.UPDATE, FlyingUpdateMe);
					if (isServer)
						UpdateClientMomentum(transform.localPosition, newtonianMovement, airTime, slideTime,
							registerTile.Matrix.Id, spinMagnitude, false, NetId.Empty);
				}
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

		if (ObjectIsBucklingChecked.HasComponent)
		{
			ObjectIsBucklingChecked.Component.SetTransform(position, world);
		}
	}

	public void SetRegisterTileLocation(Vector3Int localPosition)
	{
		registerTile.ServerSetLocalPosition(localPosition);
		registerTile.ClientSetLocalPosition(localPosition);
		if (ObjectIsBucklingChecked.HasComponent)
		{
			ObjectIsBucklingChecked.Component.SetRegisterTileLocation(localPosition);
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

	[PlayModeOnly] private float SecondsFlying;

	public bool IsFlyingSliding
	{
		get
		{
			return isFlyingSliding ;
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


	[PlayModeOnly, SerializeField]
	private bool isFlyingSliding;


	[PlayModeOnly] public bool IsMoving = false; //Is animating with tile movement

	public bool IsWalking => MoveIsWalking && IsMoving;

	[PlayModeOnly] public bool MoveIsWalking = false;

	[PlayModeOnly] public double LastUpdateClientFlying = 0; //NetworkTime.time

	public void FlyingUpdateMe()
	{
		if (isVisible == false)
		{
			IsFlyingSliding = false;
			airTime = 0;
			slideTime = 0;
			UpdateManager.Remove(CallbackType.UPDATE, FlyingUpdateMe);
			return;
		}

		if (IsMoving)
		{
			return;
		}

		isFlyingSliding = true;
		MoveIsWalking = false;

		if (this == null)
		{
			UpdateManager.Remove(CallbackType.UPDATE, FlyingUpdateMe);
		}

		if (IsPlayer == false)
		{
			SecondsFlying += Time.deltaTime;
			if (SecondsFlying > 90) //Stop taking up CPU resources! If you're flying through space for too long
			{
				newtonianMovement *= 0;
			}
		}

		if (PulledBy.HasComponent)
		{
			return; //It is recursively handled By parent
		}

		if (airTime > 0)
		{
			airTime -= Time.deltaTime; //Doesn't matter if it goes under zero

			if (airTime <= 0)
			{
				OnImpact.Invoke(this, newtonianMovement);
			}
		}
		else if (slideTime > 0)
		{
			slideTime -= Time.deltaTime; //Doesn't matter if it goes under zero
			var floating = IsFloating();
			if (floating == false)
			{
				AppliedFriction(DEFAULT_SLIDE_FRICTION);
			}
		}
		else if (IsStickyMovement)
		{
			var floating = IsFloating();
			if (floating == false)
			{
				if (newtonianMovement.magnitude > maximumStickSpeed) //Too fast to grab onto anything
				{
					AppliedFriction(DEFAULT_Friction);
				}
				else
				{
					//Stuck
					newtonianMovement *= 0;
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


		var position = this.transform.position;

		var newPosition = position + (newtonianMovement.To3() * Time.deltaTime);

		// if (Newposition.magnitude > 100000)
		// {
			// newtonianMovement *= 0;
		// }

		var intPosition = position.RoundToInt();
		var intNewPosition = newPosition.RoundToInt();

		transform.Rotate(new Vector3(0, 0, spinMagnitude * newtonianMovement.magnitude * Time.deltaTime));

		if (intPosition != intNewPosition)
		{
			if ((intPosition - intNewPosition).magnitude > 1.5f)
			{
				if (Collider != null) Collider.enabled = false;

				var hit = MatrixManager.Linecast(position,
					LayerTypeSelection.Walls | LayerTypeSelection.Grills | LayerTypeSelection.Windows,
					defaultInteractionLayerMask, newPosition, true);
				if (hit.ItHit)
				{
					OnImpact.Invoke(this, newtonianMovement);
					NewtonianMovement -= 2 * (newtonianMovement * hit.Normal) * hit.Normal;
					var offset = (0.1f * hit.Normal);
					newPosition = hit.HitWorld + offset.To3();
					NewtonianMovement *= 0.9f;
					spinMagnitude *= -1;
				}
				if (Collider != null) Collider.enabled = true;
			}




			if (newtonianMovement.magnitude > 0)
			{
				var cashedNewtonianMovement = newtonianMovement;
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
						if (isServer)
						{
							bump.OnBump(this.gameObject, null);
						}
					}

					var normal = (intPosition - intNewPosition).To3();
					if (Hits.Count == 0)
					{
						newPosition = position;
					}

					OnImpact.Invoke(this, newtonianMovement);
					NewtonianMovement -= 2 * (newtonianMovement * normal) * normal;
					NewtonianMovement *= 0.9f;
					spinMagnitude *= -1;

				}

				if (Pushing.Count > 0)
				{
					foreach (var push in Pushing)
					{
						if (push == this) continue;

						push.NewtonianNewtonPush(newtonianMovement, (newtonianMovement.magnitude * GetWeight()),
							Single.NaN, Single.NaN, aim, thrownBy, spinMagnitude);
					}

					var normal = (intPosition - intNewPosition).To3();

					if (Hits.Count == 0)
					{
						newPosition = position;
					}

					OnImpact.Invoke(this, newtonianMovement);
					NewtonianMovement -= 2 * (newtonianMovement * normal) * normal;
					spinMagnitude *= -1;
					NewtonianMovement *= 0.5f;
				}

				if (attributes.HasComponent)
				{
					OnImpact.Invoke(this, newtonianMovement);
					var IAV2 = (attributes.Component as ItemAttributesV2);
					if (IAV2 != null)
					{
						foreach (var hit in Hits)
						{
							//Integrity
							//LivingHealthMasterBase
							//TODO DamageTile( goal,Matrix.Matrix.TilemapsDamage);

							if (hit.gameObject == thrownBy) continue;
							if (cashedNewtonianMovement.magnitude > IAV2.ThrowSpeed * 0.75f)
							{
								//Remove cast to int when moving health values to float
								var damage = (IAV2.ServerThrowDamage);


								if (hit.TryGetComponent<Integrity>(out var integrity))
								{
									integrity.ApplyDamage(damage, AttackType.Melee, IAV2.ServerDamageType);
								}

								if (hit.TryGetComponent<LivingHealthMasterBase>(out var livingHealthMasterBase))
								{
									var hitZone = aim.Randomize();
									livingHealthMasterBase.ApplyDamageToBodyPart(thrownBy, damage, AttackType.Melee,
										DamageType.Brute,
										hitZone);
									Chat.AddThrowHitMsgToChat(gameObject, livingHealthMasterBase.gameObject, hitZone);
								}

								AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.85f, 1f));
								SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.GenericHit, transform.position,
									audioSourceParameters, sourceObj: gameObject);
							}
						}
					}
				}
			}
		}

		if (isVisible == false) return;

		var movetoMatrix = MatrixManager.AtPoint(newPosition.RoundToInt(), isServer).Matrix;

		var cachedPosition = this.transform.position;

		SetTransform(newPosition, true);

		if (registerTile.Matrix != movetoMatrix)
		{
			SetMatrix(movetoMatrix);
		}

		var localPosition = newPosition.ToLocal(movetoMatrix);

		SetRegisterTileLocation(localPosition.RoundToInt());

		if (isServer)
		{
			if (NetworkTime.time - LastUpdateClientFlying > 2)
			{
				LastUpdateClientFlying = NetworkTime.time;
				UpdateClientMomentum(transform.localPosition, newtonianMovement, airTime, slideTime,
					registerTile.Matrix.Id, spinMagnitude, true, NetId.Empty);
			}
		}


		if (newtonianMovement.magnitude < 0.01f) //Has slowed down enough
		{
			localPosition = transform.localPosition;
			SetLocalTarget = new Vector3WithData()
			{
				Vector3 = localPosition,
				ByClient = NetId.Empty
			};

			InternalTriggerOnLocalTileReached(localPosition);
			if (onStationMovementsRound)
			{
				doNotApplyMomentumOnTarget = true;
				if (Animating == false)
				{
					Animating = true;
					UpdateManager.Add(CallbackType.UPDATE, AnimationUpdateMe);
				}
			}

			IsFlyingSliding = false;
			airTime = 0;
			slideTime = 0;
			OnThrowEnd.Invoke(this);
			//maybe

			if (OnThrowEndResetRotation)
			{
				transform.localRotation = Quaternion.Euler(0, 0, 0);
			}

			UpdateManager.Remove(CallbackType.UPDATE, FlyingUpdateMe);
		}


		if (Pulling.HasComponent)
		{
			var inDirection = cachedPosition - Pulling.Component.transform.position;
			if (inDirection.magnitude > 2f && (isServer || isLocalPlayer))
			{
				PullSet(null, false); //TODO maybe remove
				if (isLocalPlayer && isServer == false) CmdStopPulling();
			}
			else
			{
				Pulling.Component.ProcessNewtonianPull(newtonianMovement, newPosition);
			}
		}

		if (ObjectIsBucklingChecked.HasComponent && ObjectIsBucklingChecked.Component.Pulling.HasComponent)
		{
			var inDirection = cachedPosition - ObjectIsBucklingChecked.Component.Pulling.Component.transform.position;
			if (inDirection.magnitude > 2f && (isServer || isLocalPlayer))
			{
				ObjectIsBucklingChecked.Component.PullSet(null, false); //TODO maybe remove
				if (ObjectIsBucklingChecked.Component.isLocalPlayer && isServer == false) ObjectIsBucklingChecked.Component.CmdStopPulling();
			}
			else
			{
				ObjectIsBucklingChecked.Component.Pulling.Component.ProcessNewtonianPull(newtonianMovement, newPosition);
			}
		}
	}


	public void ProcessNewtonianPull(Vector2 InNewtonianMovement, Vector2 PullerPosition)
	{
		if (Animating)
		{
			TileMoveSpeedOverride = 0;
			Animating = false;
			MoveIsWalking = false;
			IsMoving = false;
			UpdateManager.Remove(CallbackType.UPDATE, AnimationUpdateMe);
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
		SetRegisterTileLocation(localPosition.RoundToInt());

		if (Pulling.HasComponent)
		{
			Pulling.Component.ProcessNewtonianPull(InNewtonianMovement, newPosition);
		}

		if (ObjectIsBucklingChecked.HasComponent && ObjectIsBucklingChecked.Component.Pulling.HasComponent)
		{
			ObjectIsBucklingChecked.Component.Pulling.Component.ProcessNewtonianPull(InNewtonianMovement, newPosition);
		}
	}


	protected MatrixCash SetMatrixCache = new MatrixCash();

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
		var rounded = localPos.TruncateToInt();

		OnLocalTileReached.Invoke(oldLocalTilePosition, rounded);

		oldLocalTilePosition = rounded;

		if (isServer == false) return;

		LocalTileReached(rounded);
	}

	public virtual void LocalTileReached(Vector3Int localPos)
	{
		if(doStepInteractions == false) return;

		var matrix = registerTile.Matrix;
		if(matrix == null) return;

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

	public virtual RightClickableResult GenerateRightClickOptions()
	{
		var options = RightClickableResult.Create();

		if (string.IsNullOrEmpty(PlayerList.Instance.AdminToken) == false &&
		    KeyboardInputManager.Instance.CheckKeyAction(KeyAction.ShowAdminOptions, KeyboardInputManager.KeyEventType.Hold))
		{
			options.AddAdminElement("Teleport To", AdminTeleport).AddAdminElement("Toggle Pushable", AdminTogglePushable);
		}

		//check if our local player can reach this
		var initiator = PlayerManager.LocalPlayerScript.GetComponent<UniversalObjectPhysics>();
		if (initiator == null) return options;

		//if it's pulled by us
		if (PulledBy.HasComponent && PulledBy.Component == initiator)
		{
			//already pulled by us, but we can stop pulling
			options.AddElement("StopPull", TryTogglePull);
		}
		else
		{
			// Check if in range for pulling, not trying to pull itself and it can be pulled.
			if (Validations.IsReachableByRegisterTiles(initiator.registerTile, registerTile, false,
				    context: gameObject) &&
			    isNotPushable == false && initiator != this)
			{
				options.AddElement("Pull", TryTogglePull);
			}
		}

		return options;
	}

	private void AdminTeleport()
	{
		AdminCommandsManager.Instance.CmdTeleportToObject(gameObject);
	}

	private void AdminTogglePushable()
	{
		AdminCommandsManager.Instance.CmdTogglePushable(gameObject);
	}

	public virtual void OnDestroy()
	{
		if (PulledBy.HasComponent)
		{
			PulledBy.Component.PullSet(null, false);
		}
		if (Animating) UpdateManager.Remove(CallbackType.UPDATE, AnimationUpdateMe);
		if (IsFlyingSliding) UpdateManager.Remove(CallbackType.UPDATE, FlyingUpdateMe);
		if (CorrectingCourse) UpdateManager.Remove(CallbackType.UPDATE, FloatingCourseCorrection);

	}


	public void TryTogglePull()
	{
		var initiator = PlayerManager.LocalPlayerScript.GetComponent<UniversalObjectPhysics>();
		//client pre-validation
		if (Validations.IsReachableByRegisterTiles(initiator.registerTile, this.registerTile, false,
			    context: gameObject) && initiator != this)
		{
			//client request: start/stop pulling
			if (PulledBy.Component == initiator)
			{
				initiator.PullSet(null, true);
				initiator.CmdStopPulling();
			}
			else
			{
				if (this.isNotPushable) return;
				initiator.PullSet(this, true);
				initiator.CmdPullObject(gameObject);
			}
		}
		else
		{
			initiator.PullSet(null, true);
			initiator.CmdStopPulling();
		}
	}

	[Command]
	public void CmdPullObject(GameObject pullableObject)
	{
		if (ContainedInContainer != null) return;//Can't pull stuff inside of objects
		if (pullableObject == null || pullableObject == this.gameObject) return;
		var pullable = pullableObject.GetComponent<UniversalObjectPhysics>();
		if (pullable == null || pullable.isNotPushable)
		{
			return;
		}

		if (Pulling.HasComponent)
		{
			//Just stopping pulling of object if we try pulling it again
			if (Pulling.Component == pullable)
			{
				return;
			}

			PullSet(null, true);
		}

		PlayerInfo clientWhoAsked = PlayerList.Instance.Get(gameObject);
		if (Validations.CanApply(clientWhoAsked.Script, gameObject, NetworkSide.Server
			    , apt: Validations.CheckState(x => x.CanPull)) == false)
		{
			return;
		}

		if (Validations.IsReachableByRegisterTiles(pullable.registerTile, this.registerTile, true,
			    context: pullableObject))
		{
			PullSet(pullable, true);
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.ThudSwoosh, pullable.transform.position,
				sourceObj: pullableObject);
			//TODO Update the UI
		}
	}

	/// Client requests to stop pulling any objects
	[Command]
	public void CmdStopPulling()
	{
		PullSet(null, true);
	}

	public void StopPulling(bool byClient)
	{
		if (isServer == false) CmdStopPulling();
		PullSet(null, byClient);
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

	#region Buckling
	//TODO pulling

	// netid of the game object we are buckled to, NetId.Empty if not buckled
	[SyncVar(hook = nameof(SyncBuckledToObject))]
	protected UniversalObjectPhysics ObjectIsBuckling = null;

	public CheckedComponent<UniversalObjectPhysics> ObjectIsBucklingChecked =
		new CheckedComponent<UniversalObjectPhysics>();


	public UniversalObjectPhysics BuckledToObject;

	public bool IsBuckled => BuckledToObject != null;


	// syncvar hook invoked client side when the buckledTo changes
	private void SyncBuckledToObject(UniversalObjectPhysics oldBuckledTo, UniversalObjectPhysics newBuckledTo)
	{
		// unsub if we are subbed
		if (oldBuckledTo != null)
		{
			var directionalObject = this.GetComponent<Rotatable>();
			if (directionalObject != null)
			{
				directionalObject.OnRotationChange.RemoveListener(oldBuckledTo.OnBuckledObjectDirectionChange);
			}
			oldBuckledTo.BuckleToChange(null);
			oldBuckledTo.BuckledToObject = null;
		}



		ObjectIsBucklingChecked.DirectSetComponent(newBuckledTo);
		ObjectIsBuckling = newBuckledTo;

		// sub
		if (ObjectIsBuckling != null)
		{
			ObjectIsBuckling.BuckledToObject = this;
			ObjectIsBuckling.BuckleToChange(this);
			var directionalObject = this.GetComponent<Rotatable>();
			if (directionalObject != null)
			{
				directionalObject.OnRotationChange.AddListener(newBuckledTo.OnBuckledObjectDirectionChange);
			}
		}
	}

	public virtual void BuckleToChange(UniversalObjectPhysics newBuckledTo)
	{

	}


	private void OnBuckledObjectDirectionChange(OrientationEnum newDir)
	{
		if (rotatable == null)
		{
			rotatable = gameObject.GetComponent<Rotatable>();
		}

		rotatable.FaceDirection(newDir);
	}

	/// <summary>
	/// Server side logic for unbuckling a player
	/// </summary>
	[Server]
	public void Unbuckle()
	{
		ObjectIsBuckling = null;
	}

	/// <summary>
	/// Server side logic for buckling a player
	/// </summary>
	[Server]
	public void BuckleObjectToThis(UniversalObjectPhysics newBuckledTo)
	{
		ObjectIsBuckling = newBuckledTo;
	}

	#endregion
	public class ForceEventWithChange : UnityEvent<UniversalObjectPhysics, Vector2>
	{
	}

	public class ForceEvent : UnityEvent<UniversalObjectPhysics>
	{
	}
}
