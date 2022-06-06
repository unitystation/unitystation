using System;
using System.Collections.Generic;
using Core.Editor.Attributes;
using HealthV2;
using Items;
using Messages.Server.SoundMessages;
using Mirror;
using Objects;
using Objects.Construction;
using UnityEngine;
using UnityEngine.Events;
using Util;
using Random = UnityEngine.Random;

public class UniversalObjectPhysics : NetworkBehaviour, IRightClickable
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
			else if (pickupable != null && pickupable.ItemSlot != null)
			{
				return pickupable.ItemSlot.ItemStorage.gameObject.AssumedWorldPosServer();
			}
			else
			{
				return transform.position;
			}
		}
	}

	private Pickupable pickupable;

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

	[SyncVar(hook = nameof(SynchroniseUpdatePulling))]
	private PullData ThisPullData;

	[SyncVar(hook = nameof(SynchroniseParent))]
	private uint parentContainer;

	protected int SetTimestampID = -1;

	private ObjectContainer CashedContainedInContainer;

	public ObjectContainer ContainedInContainer
	{
		get
		{
			if (parentContainer is NetId.Invalid or NetId.Empty)
			{
				return null;
			}
			else
			{
				if (CashedContainedInContainer == null)
				{
					if (NetworkIdentity.spawned.TryGetValue(parentContainer, out var Net))
					{
						CashedContainedInContainer = Net.GetComponent<ObjectContainer>();
					}
					else
					{
						CashedContainedInContainer = null;
					}
				}
				else
				{
					if (CashedContainedInContainer.registerTile.netId != parentContainer)
					{
						if (NetworkIdentity.spawned.TryGetValue(parentContainer, out var Net))
						{
							CashedContainedInContainer = Net.GetComponent<ObjectContainer>();
						}
						else
						{
							CashedContainedInContainer = null;
						}
					}
				}

				return CashedContainedInContainer;
			}
		}
	}

	[PlayModeOnly] public Vector2 newtonianMovement; //* attributes.Size -> weight

	[PlayModeOnly] public float airTime; //Cannot grab onto anything so no friction

	[PlayModeOnly] public float slideTime;
	//Reduced friction during this time, if stickyMovement Just has normal friction vs just grabbing

	[PlayModeOnly] public GameObject thrownBy;

	[PlayModeOnly] public BodyPartType aim;

	[PlayModeOnly] public float spinMagnitude = 0;

	[PlayModeOnly] public int ForcedPushedFrame = 0;
	[PlayModeOnly] public int TryPushedFrame = 0;
	[PlayModeOnly] public int PushedFrame = 0;
	[PlayModeOnly] public bool FramePushDecision = true;

	[PrefabModeOnly] public bool stickyMovement = false;
	//If this thing likes to grab onto stuff such as like a player

	[PrefabModeOnly] public bool OnThrowEndResetRotation;

	[PrefabModeOnly] public float maximumStickSpeed = 1.5f;
	//Speed In tiles per second that, The thing would able to be stop itself if it was sticky

	[PrefabModeOnly] public bool onStationMovementsRound;

	[HideInInspector] public Attributes attributes;

	[HideInInspector] public RegisterTile registerTile;

	protected LayerMask defaultInteractionLayerMask;

	[HideInInspector] public GameObject[] ContextGameObjects = new GameObject[2];

	[PlayModeOnly] public bool IsCurrentlyFloating;


	[PlayModeOnly]
	public CheckedComponent<UniversalObjectPhysics> Pulling = new CheckedComponent<UniversalObjectPhysics>();

	[PlayModeOnly]
	public CheckedComponent<UniversalObjectPhysics> PulledBy = new CheckedComponent<UniversalObjectPhysics>();

	#region Events

	[PlayModeOnly] public ForceEvent OnThrowStart = new ForceEvent();

	[PlayModeOnly] public ForceEvent OnThrowEnd = new ForceEvent();

	[PlayModeOnly] public Vector3Event OnLocalTileReached = new Vector3Event();

	#endregion


	public virtual void Awake()
	{
		Collider = this.GetComponent<BoxCollider2D>();
		floorDecal = this.GetComponent<FloorDecal>();
		ContextGameObjects[0] = gameObject;
		defaultInteractionLayerMask = LayerMask.GetMask("Furniture", "Walls", "Windows", "Machines", "Players",
			"Door Closed",
			"HiddenWalls", "Objects");
		attributes = GetComponent<Attributes>();
		registerTile = GetComponent<RegisterTile>();
		rotatable = GetComponent<Rotatable>();
		pickupable = GetComponent<Pickupable>();
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
			registerTile.ServerSetLocalPosition(synchLocalTargetPosition.Vector3.RoundToInt());
			registerTile.ClientSetLocalPosition(synchLocalTargetPosition.Vector3.RoundToInt());
			transform.localPosition = synchLocalTargetPosition.Vector3;
		}

		if (SnapToGridOnStart && isServer)
		{
			transform.position = transform.position.RoundToInt();
		}
	}


	public override void OnStartClient()
	{
		base.OnStartClient();
	}

	public Size GetSize()
	{
		return attributes ? attributes.Size : Size.Huge;
	}

	public float GetWeight()
	{
		return SizeToWeight(GetSize());
	}

	public virtual void OnEnable()
	{
	}

	public virtual void OnDisable()
	{
	}

	public struct PullData
	{
		public UniversalObjectPhysics NewPulling;
		public bool WasCausedByClient;

		public override bool Equals(object? obj)
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


	public struct Vector3WithData
	{
		public Vector3 Vector3;
		public uint ByClient;


		public bool Equals(Vector3WithData other) => Equals(Vector3, other.Vector3) && ByClient == other.ByClient;

		public override bool Equals(object? obj)
		{
			return obj is Vector3WithData other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Vector3, ByClient);
		}
	}

	public void SyncMovementSpeed(float old, float Newmove)
	{
		tileMoveSpeed = Newmove;
	}


	public void SyncLocalTarget(Vector3WithData OLDLocalTarget, Vector3WithData NewLocalTarget)
	{
		synchLocalTargetPosition = NewLocalTarget;
		if (isServer) return;
		if (LocalTargetPosition == NewLocalTarget.Vector3) return;
		if (isLocalPlayer || PulledBy.HasComponent) return;
		if (NewLocalTarget.ByClient != NetId.Empty && NewLocalTarget.ByClient != NetId.Invalid
				&& NetworkIdentity.spawned.ContainsKey(NewLocalTarget.ByClient)
				&& NetworkIdentity.spawned[NewLocalTarget.ByClient].gameObject == PlayerManager.LocalPlayerObject) return;

		SetLocalTarget = NewLocalTarget;

		if (IsFlyingSliding)
		{
			IsFlyingSliding = false;
			airTime = 0;
			slideTime = 0;
			UpdateManager.Remove(CallbackType.UPDATE, FlyingUpdateMe);
		}

		if (Animating == false && transform.localPosition != NewLocalTarget.Vector3)
			UpdateManager.Add(CallbackType.UPDATE, AnimationUpdateMe);
	}


	public void StoreTo(ObjectContainer NewParent)
	{
		if (NewParent.OrNull()?.gameObject == this.gameObject)
		{
			Chat.AddGameWideSystemMsgToChat(
				" Something doesn't feel right? **BBBBBBBBBBBBBOOOOOOOOOOOOOOOOOOOMMMMMMMMMMMEMMEEEEE** ");
			Logger.LogError("Tried to store object within itself");
			return; //Storing something inside of itself what?
		}

		PullSet(null,false); //Presume you can't Pulling stuff inside container
		//TODO Handle non-disappearing containers like Cart riding

		if (NewParent == null)
		{
			parentContainer = NetId.Empty;
		}
		else
		{
			parentContainer = NewParent.registerTile.netId;
		}

		CashedContainedInContainer = NewParent;
		if (NewParent == null)
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
		//Logger.LogError("ClientRpc Tile push for " + transform.name + " Direction " + WorldDirection.ToString());
		SetMatrixCash.ResetNewPosition(transform.position, registerTile);
		ForceTilePush(worldDirection, Pushing, causedByClient, speed);
	}


	[ClientRpc]
	public void UpdateClientMomentum(Vector3 ReSetToLocal, Vector2 NewMomentum, float InairTime, float InslideTime,
		int MatrixID, float InspinFactor, bool ForceOverride, uint DoNotUpdateThisClient)
	{
		if (isServer) return;
		if (DoNotUpdateThisClient != NetId.Empty && DoNotUpdateThisClient != NetId.Invalid
			&& NetworkIdentity.spawned.ContainsKey(DoNotUpdateThisClient)
			&& NetworkIdentity.spawned[DoNotUpdateThisClient].gameObject == PlayerManager.LocalPlayerObject) return;


		//if (isLocalPlayer) return; //We are updating other Objects than the player on the client //TODO Also block if being pulled by local player //Why we need this?
		newtonianMovement = NewMomentum;
		airTime = InairTime;
		slideTime = InslideTime;
		spinMagnitude = InspinFactor;
		SetMatrix(MatrixManager.Get(MatrixID).Matrix);

		registerTile.ServerSetLocalPosition(ReSetToLocal.RoundToInt());
		registerTile.ClientSetLocalPosition(ReSetToLocal.RoundToInt());

		if (IsFlyingSliding) //If we flying try and smooth it
		{
			LocalDifferenceNeeded = ReSetToLocal - transform.localPosition;
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
				Vector3 = ReSetToLocal,
				ByClient = NetId.Empty
			};
			transform.localPosition = ReSetToLocal;
		}

		if (Animating == false && IsFlyingSliding == false)
		{
			IsFlyingSliding = true;
			UpdateManager.Add(CallbackType.UPDATE, FlyingUpdateMe);
		}
	}

	private void SynchronisedoNotApplyMomentumOnTarget(bool Oldvariable, bool Newvariable)
	{
		doNotApplyMomentumOnTarget = Newvariable;
	}

	private void SynchroniseVisibility(bool OldVisibility, bool NewVisibility)
	{
		isVisible = NewVisibility;
		if (isVisible)
		{
			var Sprites = GetComponentsInChildren<SpriteRenderer>();
			foreach (var Sprite in Sprites)
			{
				Sprite.enabled = true;
			}
		}
		else
		{
			var Sprites = GetComponentsInChildren<SpriteRenderer>();
			foreach (var Sprite in Sprites)
			{
				Sprite.enabled = false;
			}

			transform.localPosition = TransformState.HiddenPos;
			registerTile.ServerSetLocalPosition(TransformState.HiddenPos);
			registerTile.ClientSetLocalPosition(TransformState.HiddenPos);
			ResetEverything();
		}
	}

	private void SynchroniseParent(uint Old, uint NewPulling)
	{
		parentContainer = NewPulling;
		//Visibility is handled by Server with Synchronised var
	}

	public void SynchroniseUpdatePulling(PullData Old, PullData NewPulling)
	{
		ThisPullData = NewPulling;
		if (NewPulling.WasCausedByClient && isLocalPlayer) return;
		PullSet(NewPulling.NewPulling, false, true);
	}

	public void AppearAtWorldPositionServer(Vector3 WorldPOS, bool Smooth = false)
	{
		SynchroniseVisibility(isVisible, true);
		var Matrix = MatrixManager.AtPoint(WorldPOS, isServer);
		ForceSetLocalPosition(WorldPOS.ToLocal(Matrix), Vector2.zero, Smooth, Matrix.Id);
	}

	public void DropAtAndInheritMomentum(UniversalObjectPhysics DroppedFrom)
	{
		SynchroniseVisibility(isVisible, true);
		ForceSetLocalPosition(DroppedFrom.transform.localPosition, DroppedFrom.newtonianMovement, false,
			DroppedFrom.registerTile.Matrix.Id);
	}

	public void DisappearFromWorld()
	{
		SynchroniseVisibility(isVisible, false);
	}

	public void ForceSetLocalPosition(Vector3 ReSetToLocal, Vector2 Momentum, bool Smooth, int MatrixID,
		bool UpdateClient = true, float Rotation = 0, NetworkConnection Client = null)
	{
		transform.localRotation = Quaternion.Euler(new Vector3(0, 0, Rotation));
		if (isServer && UpdateClient)
		{
			isVisible = true;
			if (Client != null)
			{
				RPCForceSetPosition(Client, ReSetToLocal, Momentum, Smooth, MatrixID, Rotation);
			}
			else
			{
				RPCForceSetPosition(ReSetToLocal, Momentum, Smooth, MatrixID, Rotation);
			}
		}

		SetMatrix(MatrixManager.Get(MatrixID).Matrix);


		if (Smooth)
		{
			if (IsFlyingSliding)
			{
				newtonianMovement = Momentum;
				LocalDifferenceNeeded = ReSetToLocal - transform.localPosition;
				registerTile.ServerSetLocalPosition(ReSetToLocal.RoundToInt());
				registerTile.ClientSetLocalPosition(ReSetToLocal.RoundToInt());
				if (CorrectingCourse == false)
				{
					CorrectingCourse = true;
					UpdateManager.Add(CallbackType.UPDATE, FloatingCourseCorrection);
				}
			}
			else
			{
				newtonianMovement = Momentum;
				SetLocalTarget = new Vector3WithData()
				{
					Vector3 = ReSetToLocal,
					ByClient = NetId.Empty
				};
				registerTile.ServerSetLocalPosition(ReSetToLocal.RoundToInt());
				registerTile.ClientSetLocalPosition(ReSetToLocal.RoundToInt());

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
				newtonianMovement = Momentum;
				transform.localPosition = ReSetToLocal;
				registerTile.ServerSetLocalPosition(ReSetToLocal.RoundToInt());
				registerTile.ClientSetLocalPosition(ReSetToLocal.RoundToInt());
			}
			else
			{
				newtonianMovement = Momentum;

				SetLocalTarget = new Vector3WithData()
				{
					Vector3 = ReSetToLocal,
					ByClient = NetId.Empty
				};
				transform.localPosition = ReSetToLocal;
				registerTile.ServerSetLocalPosition(ReSetToLocal.RoundToInt());
				registerTile.ClientSetLocalPosition(ReSetToLocal.RoundToInt());
			}
		}
	}

	[ClientRpc]
	public void RPCForceSetPosition(Vector3 ReSetToLocal, Vector2 Momentum, bool Smooth, int MatrixID, float Rotation)
	{
		ForceSetLocalPosition(ReSetToLocal, Momentum, Smooth, MatrixID, false, Rotation);
	}

	[TargetRpc]
	public void RPCForceSetPosition(NetworkConnection target, Vector3 ReSetToLocal, Vector2 Momentum, bool Smooth,
		int MatrixID, float Rotation)
	{
		ForceSetLocalPosition(ReSetToLocal, Momentum, Smooth, MatrixID, false, Rotation);
	}


	//Warning only update clients!!
	public void ResetLocationOnClients(bool Smooth = false)
	{
		RPCForceSetPosition(transform.localPosition, newtonianMovement, Smooth, registerTile.Matrix.Id,
			transform.localRotation.eulerAngles.z);

		if (Pulling.HasComponent)
		{
			Pulling.Component.ResetLocationOnClients(Smooth);
		}
		//Update client to server state
	}

	//Warning only update client!!

	public void ResetLocationOnClient(NetworkConnection Client, bool Smooth = false)
	{
		isVisible = true;

		RPCForceSetPosition(Client, transform.localPosition, newtonianMovement, Smooth, registerTile.Matrix.Id,
			transform.localRotation.eulerAngles.z);

		if (Pulling.HasComponent)
		{
			Pulling.Component.ResetLocationOnClient(Client, Smooth);
		}
		//Update client to server state
	}

	[PlayModeOnly] public Vector2 LocalDifferenceNeeded;

	[PlayModeOnly] public bool CorrectingCourse = false;


	public void FloatingCourseCorrection()
	{
		CorrectingCourse = true;
		var position = transform.localPosition;
		var NewPosition = this.MoveTowards(position, (position + LocalDifferenceNeeded.To3()),
			(newtonianMovement.magnitude + 4) * Time.deltaTime);
		LocalDifferenceNeeded -= (NewPosition - position).To2();

		transform.localPosition = NewPosition;

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

	public void SetIsNotPushable(bool NewState)
	{
		isNotPushable = NewState;
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
		transform.position = TransformCash;
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
		if (isNotPushable) return false;
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
		var From = transform.position;
		if (IsMoving) //We are moving combined targets
		{
			From = LocalTargetPosition.ToWorld(registerTile.Matrix);
		}


		if (IsMoving)
		{
			SetMatrixCash.ResetNewPosition(From);
		}
		else
		{
			SetMatrixCash.ResetNewPosition(From, registerTile);
		}


		if (MatrixManager.IsPassableAtAllMatricesV2(From,
			    From + worldDirection.To3Int(), SetMatrixCash, this,
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
		UniversalObjectPhysics pushedBy = null, bool overridePull = false)
	{
		if (isVisible == false) return;
		if (pushedBy == this) return;
		if (CanPush(worldDirection))
		{
			ForceTilePush(worldDirection, Pushing, byClient, speed, pushedBy: pushedBy, overridePull: overridePull);
		}
	}

	public void ForceTilePush(Vector2Int worldDirection, List<UniversalObjectPhysics> inPushing, GameObject byClient,
		float speed = Single.NaN, bool isWalk = false,
		UniversalObjectPhysics pushedBy = null, bool overridePull = false) //PushPull TODO Change to physics object
	{
		if (isVisible == false) return;
		if (ForcedPushedFrame == Time.frameCount)
		{
			return;
		}

		ForcedPushedFrame = Time.frameCount;
		if (isNotPushable) return;
		//Nothing is pushing this (  mainly because I left it on player And forgot to turn it off ), And it was hard to tell it was on
		doNotApplyMomentumOnTarget = false;
		if (float.IsNaN(speed))
		{
			speed = TileMoveSpeed;
		}

		if (inPushing.Count > 0) //Has to push stuff
		{
			//Push Object
			foreach (var push in inPushing)
			{
				if (push == this) continue;
				if (push == pushedBy) continue;
				if (Pulling.HasComponent && Pulling.Component == push) continue;
				if (PulledBy.HasComponent && PulledBy.Component == push) continue;
				if (pushedBy == null)
				{
					pushedBy = this;
				}

				var PushDirection = -1 * (this.transform.position - push.transform.position).To2Int();
				if (PushDirection == Vector2Int.zero)
				{
					PushDirection = worldDirection;
				}

				push.TryTilePush(PushDirection, byClient, speed, pushedBy);
			}
		}

		var CachedPosition = transform.position;
		if (IsMoving) //We are moving combined targets
		{
			CachedPosition = LocalTargetPosition.ToWorld(registerTile.Matrix);
		}

		var NewWorldPosition = CachedPosition + worldDirection.To3Int();

		if ((NewWorldPosition - transform.position).magnitude > 1.45f) return; //Is pushing too far

		var movetoMatrix = SetMatrixCash.GetforDirection(worldDirection.To3Int()).Matrix;


		if (registerTile.Matrix != movetoMatrix)
		{
			SetMatrix(movetoMatrix);
		}

		if (ChangesDirectionPush)
		{
			rotatable.OrNull()?.SetFaceDirectionLocalVictor(worldDirection);
		}


		var LocalPosition = (NewWorldPosition).ToLocal(movetoMatrix);

		registerTile.ServerSetLocalPosition(LocalPosition.RoundToInt());
		registerTile.ClientSetLocalPosition(LocalPosition.RoundToInt());

		SetLocalTarget = new Vector3WithData()
		{
			Vector3 = LocalPosition.RoundToInt(),
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
			var InDirection = CachedPosition - Pulling.Component.transform.position;
			if (InDirection.magnitude > 2f && (isServer || isLocalPlayer))
			{
				PullSet(null, false); //TODO maybe remove
				if (isLocalPlayer && isServer == false) CmdStopPulling();
			}
			else
			{
				Pulling.Component.TryTilePush(InDirection.NormalizeTo2Int(), byClient, speed, pushedBy);
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
		var Matrix = MatrixManager.AtPoint(pos, isServer);
		var LocalPOs = pos.ToLocal(Matrix);
		ForceSetLocalPosition(LocalPOs, impulse * Random.Range(0.2f, 2f), false, Matrix.Id);
	}

	public void CheckMatrixSwitch()
	{
		var newMatrix =
			MatrixManager.AtPoint(transform.position, true, registerTile.Matrix.MatrixInfo);
		if (registerTile.Matrix.Id != newMatrix.Id)
		{
			SetMatrix(newMatrix.Matrix);
			ResetLocationOnClients();
		}
	}

	public void NewtonianNewtonPush(Vector2 WorldDirection, float Newtonians = Single.NaN, float INairTime = Single.NaN,
		float INslideTime = Single.NaN, BodyPartType inaim = BodyPartType.Chest,
		GameObject inthrownBy = null,
		float spinFactor = 0) //Collision is just naturally part of Newtonian push
	{
		var speed = Newtonians / SizeToWeight(GetSize());
		NewtonianPush(WorldDirection, speed, INairTime, INslideTime, inaim, inthrownBy, spinFactor);
	}

	public void PullSet(UniversalObjectPhysics ToPull, bool ByClient, bool Synced = false)
	{
		if (ToPull != null && ContainedInContainer != null) return; //Can't pull stuff inside of objects)

		if (isServer && Synced == false)
			SynchroniseUpdatePulling(ThisPullData, new PullData() {NewPulling = ToPull, WasCausedByClient = ByClient});

		if (ToPull != null)
		{
			if (PulledBy.HasComponent)
			{
				if (ToPull == PulledBy.Component)
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

			Pulling.DirectSetComponent(ToPull);
			ToPull.PulledBy.DirectSetComponent(this);
			ContextGameObjects[1] = ToPull.gameObject;
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


	public void NewtonianPush(Vector2 worldDirection, float speed = Single.NaN, float nairTime = Single.NaN,
		float InSlideTime = Single.NaN, BodyPartType inAim = BodyPartType.Chest, GameObject inThrownBy = null,
		float spinFactor = 0,
		GameObject DoNotUpdateThisClient = null) //Collision is just naturally part of Newtonian push
	{
		if (isVisible == false) return;
		if (isNotPushable) return;

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


		if (float.IsNaN(nairTime) == false || float.IsNaN(InSlideTime) == false)
		{
			worldDirection.Normalize();
			if (newtonianMovement.magnitude > 0)
			{
				//Logger.LogError("  momentum boost");
			}

			newtonianMovement += worldDirection * speed;
			if (newtonianMovement.magnitude > MAX_SPEED)
			{
				newtonianMovement *= (MAX_SPEED / newtonianMovement.magnitude);
			}

			if (float.IsNaN(nairTime) == false)
			{
				if (nairTime < 0)
				{
					Logger.LogError(nairTime.ToString());
				}

				airTime = nairTime;
			}

			if (float.IsNaN(InSlideTime) == false)
			{
				this.slideTime = InSlideTime;
			}
		}
		else
		{
			if (stickyMovement && IsFloating() == false)
			{
				return;
			}

			worldDirection.Normalize();
			if (newtonianMovement.magnitude > 0)
			{
				//Logger.LogError("  momentum boost");
			}

			newtonianMovement += worldDirection * speed;
			if (newtonianMovement.magnitude > MAX_SPEED)
			{
				newtonianMovement *= (MAX_SPEED / newtonianMovement.magnitude);
			}
		}

		OnThrowStart.Invoke(this);
		if (newtonianMovement.magnitude > 0.01f)
		{
			//It's moving add to update manager
			if (IsFlyingSliding == false)
			{
				IsFlyingSliding = true;
				UpdateManager.Add(CallbackType.UPDATE, FlyingUpdateMe);
				if (isServer)
				{
					LastUpdateClientFlying = NetworkTime.time;
					UpdateClientMomentum(transform.localPosition, newtonianMovement, airTime, this.slideTime,
						registerTile.Matrix.Id, spinFactor, true, DoNotUpdateThisClient.NetId());
				}
			}
		}
	}

	public void AppliedFriction(float FrictionCoefficient)
	{
		var SpeedLossDueToFriction = FrictionCoefficient * Time.deltaTime;

		var oldMagnitude = newtonianMovement.magnitude;

		var NewMagnitude = oldMagnitude - SpeedLossDueToFriction;

		if (NewMagnitude <= 0)
		{
			newtonianMovement *= 0;
		}
		else
		{
			newtonianMovement *= (NewMagnitude / oldMagnitude);
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
		var LocalPOS = transform.localPosition;

		IsMoving = (LocalPOS - LocalTargetPosition).magnitude > 0.001;
		if (IsMoving)
		{
			if (TileMoveSpeedOverride > 0)
			{
				transform.localPosition = this.MoveTowards(LocalPOS, LocalTargetPosition,
					TileMoveSpeedOverride * Time.deltaTime); //* transform.localPosition.SpeedTo(targetPos)
			}
			else
			{
				transform.localPosition = this.MoveTowards(LocalPOS, LocalTargetPosition,
					TileMoveSpeed * Time.deltaTime); //* transform.localPosition.SpeedTo(targetPos)
			}

			LastDifference = transform.localPosition - LocalPOS;
		}
		else
		{
			var Cash = TileMoveSpeedOverride;
			if (TileMoveSpeedOverride == 0)
			{
				Cash = TileMoveSpeed;
			}

			TileMoveSpeedOverride = 0;
			Animating = false;

			OnLocalTileReached.Invoke(transform.localPosition);
			MoveIsWalking = false;

			if (IsFloating() && PulledBy.HasComponent == false && doNotApplyMomentumOnTarget == false)
			{
				if (newtonianMovement.magnitude > 0)
				{
					//Logger.LogError("  momentum boost");
				}

				newtonianMovement += (Vector2) LastDifference.normalized * Cash;
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

	[PlayModeOnly] public bool IsFlyingSliding = false; //Is animating with space flying

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

		IsFlyingSliding = true;
		MoveIsWalking = false;

		if (this == null)
		{
			UpdateManager.Remove(CallbackType.UPDATE, FlyingUpdateMe);
		}

		if (PulledBy.HasComponent)
		{
			return; //It is recursively handled By parent
		}

		if (airTime > 0)
		{
			airTime -= Time.deltaTime; //Doesn't matter if it goes under zero
		}
		else if (slideTime > 0)
		{
			slideTime -= Time.deltaTime; //Doesn't matter if it goes under zero
			var Floating = IsFloating();
			if (Floating == false)
			{
				AppliedFriction(DEFAULT_SLIDE_FRICTION);
			}
		}
		else if (stickyMovement)
		{
			var Floating = IsFloating();
			if (Floating == false)
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
			var Floating = IsFloating();
			if (Floating == false)
			{
				AppliedFriction(DEFAULT_Friction);
			}
		}


		var position = this.transform.position;
		var Newposition = position + (newtonianMovement.To3() * Time.deltaTime);

		var intposition = position.RoundToInt();
		var intNewposition = Newposition.RoundToInt();

		transform.Rotate(new Vector3(0, 0, spinMagnitude * newtonianMovement.magnitude * Time.deltaTime));

		if (intposition != intNewposition)
		{
			// if (Collider != null) Collider.enabled = false;
			//
			// var hit = MatrixManager.Linecast(position,
			// 	LayerTypeSelection.Walls | LayerTypeSelection.Grills | LayerTypeSelection.Windows,
			// 	defaultInteractionLayerMask, Newposition, true);
			// if (hit.ItHit)
			// {
			// 	var Offset = (0.1f * hit.Normal);
			// 	Newposition = hit.HitWorld + Offset.To3();
			// 	newtonianMovement *= 0;
			// }
			// if (Collider != null) Collider.enabled = true;
			//

			if (newtonianMovement.magnitude > 0)
			{
				var CashednewtonianMovement = newtonianMovement;
				SetMatrixCash.ResetNewPosition(intposition, registerTile);
				Pushing.Clear();
				Bumps.Clear();
				Hits.Clear();
				if (MatrixManager.IsPassableAtAllMatricesV2(intposition,
					    intNewposition, SetMatrixCash, this,
					    Pushing, Bumps, Hits) == false)
				{
					foreach (var Bump in Bumps) //Bump Whatever we Bumped into
					{
						if (isServer)
						{
							Bump.OnBump(this.gameObject, null);
						}
					}


					var Normal = (intposition - intNewposition).ToNonInt3();
					Newposition = position;
					newtonianMovement -= 2 * (newtonianMovement * Normal) * Normal;
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

					var Normal = (intposition - intNewposition).ToNonInt3();
					Newposition = position;
					if (CashednewtonianMovement.magnitude < 10)
					{
						newtonianMovement -= 2 * (newtonianMovement * Normal) * Normal;
						spinMagnitude *= -1;
					}

					newtonianMovement *= 0.5f;
				}

				var IAV2 = (attributes as ItemAttributesV2);
				if (IAV2 != null)
				{
					foreach (var hit in Hits)
					{
						//Integrity
						//LivingHealthMasterBase
						//TODO DamageTile( goal,Matrix.Matrix.TilemapsDamage);

						if (hit.gameObject == thrownBy) continue;
						if (CashednewtonianMovement.magnitude > IAV2.ThrowSpeed * 0.75f)
						{
							//Remove cast to int when moving health values to float
							var damage = (IAV2.ServerThrowDamage);


							if (hit.TryGetComponent<Integrity>(out var Integrity))
							{
								Integrity.ApplyDamage(damage, AttackType.Melee, IAV2.ServerDamageType);
							}

							if (hit.TryGetComponent<LivingHealthMasterBase>(out var LivingHealthMasterBase))
							{
								var hitZone = aim.Randomize();
								LivingHealthMasterBase.ApplyDamageToBodyPart(thrownBy, damage, AttackType.Melee,
									DamageType.Brute,
									hitZone);
								Chat.AddThrowHitMsgToChat(gameObject, LivingHealthMasterBase.gameObject, hitZone);
							}

							AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: 1f);
							SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.GenericHit, transform.position,
								audioSourceParameters, sourceObj: gameObject);
						}
					}
				}
			}
		}

		if (isVisible == false) return;

		var movetoMatrix = MatrixManager.AtPoint(Newposition.RoundToInt(), isServer).Matrix;

		var CachedPosition = this.transform.position;

		this.transform.position = Newposition;

		if (registerTile.Matrix != movetoMatrix)
		{
			SetMatrix(movetoMatrix);
		}

		var LocalPosition = (Newposition).ToLocal(movetoMatrix);

		registerTile.ServerSetLocalPosition(LocalPosition.RoundToInt());
		registerTile.ClientSetLocalPosition(LocalPosition.RoundToInt());

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
			var localPosition = transform.localPosition;
			SetLocalTarget = new Vector3WithData()
			{
				Vector3 = localPosition,
				ByClient = NetId.Empty
			};

			OnLocalTileReached.Invoke(localPosition);
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
			if (OnThrowEndResetRotation)
			{
				transform.localRotation = Quaternion.Euler(0, 0, 0);
			}

			UpdateManager.Remove(CallbackType.UPDATE, FlyingUpdateMe);
		}


		if (Pulling.HasComponent)
		{
			var InDirection = CachedPosition - Pulling.Component.transform.position;
			if (InDirection.magnitude > 2f && (isServer || isLocalPlayer))
			{
				PullSet(null, false); //TODO maybe remove
				if (isLocalPlayer && isServer == false) CmdStopPulling();
			}
			else
			{
				Pulling.Component.ProcessNewtonianPull(newtonianMovement, Newposition);
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
		var Newmove = InNewtonianMovement;
		Vector3 Newposition = Vector3.zero;
		Newmove.Normalize();
		Vector3 TargetFollowLocation = PullerPosition + (Newmove * -1);
		if (Vector2.Distance(TargetFollowLocation, transform.position) > 0.1f)
		{
			Newposition = this.MoveTowards(position, TargetFollowLocation,
				(InNewtonianMovement.magnitude + 4) * Time.deltaTime);
		}
		else
		{
			Newposition = position + (InNewtonianMovement.To3() * Time.deltaTime);
		}


		//Check collision?
		this.transform.position = Newposition;
		var movetoMatrix = MatrixManager.AtPoint(Newposition.RoundToInt(), isServer).Matrix;
		if (registerTile.Matrix != movetoMatrix)
		{
			SetMatrix(movetoMatrix);
		}

		var LocalPosition = (Newposition).ToLocal(movetoMatrix);

		registerTile.ServerSetLocalPosition(LocalPosition.RoundToInt());
		registerTile.ClientSetLocalPosition(LocalPosition.RoundToInt());

		if (Pulling.HasComponent)
		{
			Pulling.Component.ProcessNewtonianPull(InNewtonianMovement, Newposition);
		}
	}


	protected MatrixCash SetMatrixCash = new MatrixCash();

	public bool IsFloating()
	{
		if (stickyMovement)
		{
			SetMatrixCash.ResetNewPosition(registerTile.WorldPosition, registerTile);
			//TODO good way to Implement
			if (MatrixManager.IsFloatingAtV2Tile(transform.position, CustomNetworkManager.IsServer,
				    SetMatrixCash))
			{
				if (MatrixManager.IsFloatingAtV2Objects(ContextGameObjects, registerTile.WorldPosition,
					    CustomNetworkManager.IsServer, SetMatrixCash))
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
			if (registerTile.Matrix.HasGravity) //Presuming Register tile has the correct matrix
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


	public RightClickableResult GenerateRightClickOptions()
	{
		//check if our local player can reach this
		var initiator = PlayerManager.LocalPlayerScript.GetComponent<UniversalObjectPhysics>();
		if (initiator == null) return null;
		//if it's pulled by us
		if (PulledBy.HasComponent && PulledBy.Component == initiator)
		{
			//already pulled by us, but we can stop pulling
			return RightClickableResult.Create()
				.AddElement("StopPull", TryTogglePull);
		}
		else
		{
			// Check if in range for pulling, not trying to pull itself and it can be pulled.
			if (Validations.IsReachableByRegisterTiles(initiator.registerTile, registerTile, false,
				    context: gameObject) &&
			    isNotPushable == false && initiator != this)
			{
				return RightClickableResult.Create()
					.AddElement("Pull", TryTogglePull);
			}
		}

		return RightClickableResult.Create();
	}

	public virtual void OnDestroy()
	{
		if (Animating) UpdateManager.Remove(CallbackType.UPDATE, AnimationUpdateMe);
		if (IsFlyingSliding) UpdateManager.Remove(CallbackType.UPDATE, FlyingUpdateMe);
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
		if (Validations.CanApply(clientWhoAsked.Script, gameObject, NetworkSide.Server) == false)
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


	public class ForceEvent : UnityEvent<UniversalObjectPhysics>
	{
	}
}
