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
	//TODO Push objects being Reset our client
	//TODO Tile push to clients if in pulling conga line?



	//TODO parentContainer Need to test

	//TODO Maybe work on conveyor belts and players a bit more


	//TODO Sometime Combine buckling and object storage

	//TODO pulling Isn't updating the pulling icon properly on Rejoin of client

	//=================================== Maybe
	//TODO Client gets double update of space speed! Could be editor only issue, couldn't replicate on build???
	//TODO Slipping into locker Causes locker to move on client but not on server, Sometimes???

	//=================================== balance Maybe
	//=============================================== TODO some time
	//TODO Make space Movement perfect ( Is pretty much good now )
	//=============================================== Definitely
	//TODO Implement swap if you're dragging someone

	public const float DEFAULT_PUSH_SPEED = 6;

	/// <summary>
	/// Maximum speed player can reach by throwing stuff in space
	/// </summary>
	public const float MAX_NEWTONIAN_SPEED = 12;

	public const int HIGH_SPEED_COLLISION_THRESHOLD = 15;

	//public const float DEFAULT_Friction = 9999999999999f;
	public const float DEFAULT_Friction = 15f;
	public const float DEFAULT_SLIDE_FRICTION = 9f;

	public bool DEBUG = false;

	[PrefabModeOnly]
	public bool SnapToGridOnStart = false;

	private BoxCollider2D Collider; //TODO Checked component

	private float TileMoveSpeedOverride = 0;


	private FloorDecal floorDecal; // Used to make sure some objects are not causing gravity

	[PlayModeOnly]
	public Vector3 LocalTargetPosition;

	protected Rotatable rotatable;

	[PrefabModeOnly]
	public bool ChangesDirectionPush = false;

	public Vector3 SetLocalTarget
	{
		get { return LocalTargetPosition; }
		set
		{
			if (CustomNetworkManager.IsServer && synchLocalTargetPosition != value)
			{
				SyncLocalTarget(synchLocalTargetPosition, value);
			}
			//Logger.LogError("local target position set of "  + value + " On matrix " + registerTile.Matrix);
			LocalTargetPosition = value;
		}
	}



	public bool InitialLocationSynchronised;

	public bool IsVisible => isVisible;

	[SyncVar(hook = nameof(SyncMovementSpeed))]
	public float tileMoveSpeed = 1;

	public float TileMoveSpeed => tileMoveSpeed;

	[SyncVar(hook = nameof(SyncLocalTarget))]
	private Vector3 synchLocalTargetPosition;

	public Vector3 SynchLocalTargetPosition => synchLocalTargetPosition;

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
					CashedContainedInContainer = NetworkIdentity.spawned[parentContainer].GetComponent<ObjectContainer>();
				}
				else
				{
					if (CashedContainedInContainer.registerTile.netId != parentContainer)
					{
						CashedContainedInContainer = NetworkIdentity.spawned[parentContainer].GetComponent<ObjectContainer>();
					}
				}

				return CashedContainedInContainer;
			}
		}
	}

	[PlayModeOnly]
	public Vector2 newtonianMovement; //* attributes.Size -> weight

	[PlayModeOnly]
	public float airTime; //Cannot grab onto anything so no friction

	[PlayModeOnly]
	public float slideTime;
	//Reduced friction during this time, if stickyMovement Just has normal friction vs just grabbing

	[PlayModeOnly]
	public GameObject thrownBy;

	[PlayModeOnly]
	public BodyPartType aim;

	[PlayModeOnly]
	public float spinMagnitude = 0;

	[PrefabModeOnly]
	public bool stickyMovement = false;
	//If this thing likes to grab onto stuff such as like a player

	[PrefabModeOnly]
	public float maximumStickSpeed = 1.5f;
	//Speed In tiles per second that, The thing would able to be stop itself if it was sticky

	[PrefabModeOnly]
	public bool onStationMovementsRound;

	[HideInInspector]
	public Attributes attributes;

	[HideInInspector]
	public RegisterTile registerTile;

	protected LayerMask defaultInteractionLayerMask;

	[HideInInspector]
	public GameObject[] ContextGameObjects = new GameObject[2];

	[PlayModeOnly]
	public bool IsCurrentlyFloating;


	[PlayModeOnly]
	public CheckedComponent<UniversalObjectPhysics> Pulling = new CheckedComponent<UniversalObjectPhysics>();
	[PlayModeOnly]
	public CheckedComponent<UniversalObjectPhysics> PulledBy = new CheckedComponent<UniversalObjectPhysics>();

	#region Events

	[PlayModeOnly]
	public ForceEvent OnThrowStart = new ForceEvent();

	[PlayModeOnly]
	public ForceEvent OnThrowEnd = new ForceEvent();

	[PlayModeOnly]
	public Vector3Event OnLocalTileReached = new Vector3Event();

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
				return synchLocalTargetPosition;
			}
			return transform.localPosition;
		}
	}

	public void Start()
	{
		if (isServer)
		{
			SetLocalTarget = transform.localPosition;
		}
		else
		{
			transform.localPosition = synchLocalTargetPosition;
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
	}


	public void SyncMovementSpeed(float old, float Newmove)
	{
		tileMoveSpeed = Newmove;
	}


	public void SyncLocalTarget(Vector3 OLDLocalTarget, Vector3 NewLocalTarget)
	{
		synchLocalTargetPosition = NewLocalTarget;
		if (isServer) return;
		if (LocalTargetPosition == NewLocalTarget) return;
		if (isLocalPlayer || PulledBy.HasComponent) return;
		SetLocalTarget = NewLocalTarget;
		if (IsFlyingSliding)
		{
			IsFlyingSliding = false;
			airTime = 0;
			slideTime = 0;
			UpdateManager.Remove(CallbackType.UPDATE, FlyingUpdateMe);
		}

		if (Animating == false && transform.localPosition != NewLocalTarget)
			UpdateManager.Add(CallbackType.UPDATE, AnimationUpdateMe);
	}


	public void StoreTo(ObjectContainer NewParent)
	{
		if (NewParent == this) return; //Storing something inside of itself what?

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
	public void RPCClientTilePush(Vector2Int WorldDirection, float speed, bool CausedByClient)
	{
		if (isServer) return;
		if (isLocalPlayer && CausedByClient) return;
		if (PulledBy.HasComponent) return;
		Pushing.Clear();
		//Logger.LogError("ClientRpc Tile push for " + transform.name + " Direction " + WorldDirection.ToString());
		SetMatrixCash.ResetNewPosition(transform.position);
		ForceTilePush(WorldDirection, Pushing, CausedByClient, speed);
	}


	[ClientRpc]
	public void UpdateClientMomentum(Vector3 ReSetToLocal, Vector2 NewMomentum, float InairTime, float InslideTime,
		int MatrixID, float InspinFactor, bool ForceOverride)
	{
		if (isServer) return;
		//if (isLocalPlayer) return; //We are updating other Objects than the player on the client //TODO Also block if being pulled by local player //Why we need this?
		newtonianMovement = NewMomentum;
		airTime = InairTime;
		slideTime = InslideTime;
		spinMagnitude = InspinFactor;
		SetMatrix(MatrixManager.Get(MatrixID).Matrix);

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
			SetLocalTarget = ReSetToLocal;
			transform.localPosition = ReSetToLocal;
			registerTile.ServerSetLocalPosition(ReSetToLocal.RoundToInt());
			registerTile.ClientSetLocalPosition(ReSetToLocal.RoundToInt());
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
		transform.localRotation = Quaternion.Euler(new Vector3(0,0,  Rotation));
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

				if (CorrectingCourse == false)
				{
					CorrectingCourse = true;
					UpdateManager.Add(CallbackType.UPDATE, FloatingCourseCorrection);
				}
			}
			else
			{
				newtonianMovement = Momentum;
				SetLocalTarget = ReSetToLocal;

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
				SetLocalTarget = ReSetToLocal;
				transform.localPosition = ReSetToLocal;
				registerTile.ServerSetLocalPosition(ReSetToLocal.RoundToInt());
				registerTile.ClientSetLocalPosition(ReSetToLocal.RoundToInt());
			}
		}
	}

	[ClientRpc]
	public void RPCForceSetPosition(Vector3 ReSetToLocal, Vector2 Momentum, bool Smooth, int MatrixID, float Rotation)
	{
		Logger.LogError("Reset!!");
		ForceSetLocalPosition(ReSetToLocal, Momentum, Smooth, MatrixID, false, Rotation);
	}

	[TargetRpc]
	public void RPCForceSetPosition(NetworkConnection target, Vector3 ReSetToLocal, Vector2 Momentum, bool Smooth, int MatrixID, float Rotation)
	{
		ForceSetLocalPosition(ReSetToLocal, Momentum, Smooth, MatrixID, false, Rotation);
	}


	//Warning only update clients!!
	public void ResetLocationOnClients(bool Smooth = false)
	{
		RPCForceSetPosition(transform.localPosition, newtonianMovement, Smooth, registerTile.Matrix.Id, transform.localRotation.eulerAngles.z);;
		if (Pulling.HasComponent)
		{
			Pulling.Component.ResetLocationOnClients(Smooth);
		}
		//Update client to server state
	}

	//Warning only update client!!

	public void ResetLocationOnClient(NetworkConnection Client ,bool Smooth = false)
	{
		isVisible = true;

		RPCForceSetPosition(Client, transform.localPosition, newtonianMovement, Smooth, registerTile.Matrix.Id, transform.localRotation.eulerAngles.z);

		if (Pulling.HasComponent)
		{
			Pulling.Component.ResetLocationOnClient(Client, Smooth);
		}
		//Update client to server state
	}

	[PlayModeOnly]
	public Vector2 LocalDifferenceNeeded;

	[PlayModeOnly]
	public bool CorrectingCourse = false;


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


	[HideInInspector]
	public List<UniversalObjectPhysics> Pushing = new List<UniversalObjectPhysics>();

	[HideInInspector]
	public List<IBumpableObject> Bumps = new List<IBumpableObject>();

	[HideInInspector]
	public List<UniversalObjectPhysics> Hits = new List<UniversalObjectPhysics>();

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
		var TransformCash = transform.position;
		registerTile.ServerSetNetworkedMatrixNetID(movetoMatrix.NetworkedMatrix.MatrixSync.netId);
		registerTile.FinishNetworkedMatrixRegistration(movetoMatrix.NetworkedMatrix);
		transform.position = TransformCash;
		LocalDifferenceNeeded = Vector2.zero;
		SetLocalTarget = transform.localPosition;
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

	public bool CanPush(Vector2Int WorldDirection) //NOTE: It's presumed that If true one time the rest universal physics objects will return true to , manually checks for isNotPushable
	{
		if (isNotPushable) return false;

		//TODO Secured stuff
		Pushing.Clear();
		Bumps.Clear();
		SetMatrixCash.ResetNewPosition(transform.position);
		if (MatrixManager.IsPassableAtAllMatricesV2(transform.position,
			    transform.position + WorldDirection.To3Int(), SetMatrixCash, this,
			    Pushing, Bumps)) //Validate
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public void TryTilePush(Vector2Int WorldDirection, bool ByClient, float speed = Single.NaN)
	{
		if (CanPush(WorldDirection))
		{
			ForceTilePush(WorldDirection, Pushing, ByClient, speed);
		}
	}

	public void ForceTilePush(Vector2Int WorldDirection, List<UniversalObjectPhysics> InPushing, bool ByClient,
		float speed = Single.NaN, bool IsWalk = false) //PushPull TODO Change to physics object
	{
		if (isNotPushable) return; //Nothing is pushing this (  mainly because I left it on player And forgot to turn it off ), And it was hard to tell it was on
		doNotApplyMomentumOnTarget = false;
		if (float.IsNaN(speed))
		{
			speed = TileMoveSpeed;
		}

		if (InPushing.Count > 0) //Has to push stuff
		{
			//Push Object
			foreach (var push in InPushing)
			{
				if (push == this) continue;
				push.TryTilePush(WorldDirection, false, speed);
			}
		}

		var CachedPosition = transform.position;
		var NewWorldPosition = CachedPosition + WorldDirection.To3Int();

		var movetoMatrix = SetMatrixCash.GetforDirection(WorldDirection.To3Int()).Matrix;


		if (registerTile.Matrix != movetoMatrix)
		{
			SetMatrix(movetoMatrix);
		}

		if (ChangesDirectionPush)
		{
			rotatable.OrNull()?.SetFaceDirectionLocalVictor(WorldDirection);
		}


		var LocalPosition = (NewWorldPosition).ToLocal(movetoMatrix);

		registerTile.ServerSetLocalPosition(LocalPosition.RoundToInt());
		registerTile.ClientSetLocalPosition(LocalPosition.RoundToInt());

		SetLocalTarget = LocalPosition.RoundToInt();

		MoveIsWalking = IsWalk;


		if (Animating == false)
		{
			Animating = true;
			UpdateManager.Add(CallbackType.UPDATE, AnimationUpdateMe);
			TileMoveSpeedOverride = speed;
		}

		if (isServer && PulledBy.HasComponent == false)
			RPCClientTilePush(WorldDirection, speed, ByClient); //TODO Local direction

		if (Pulling.HasComponent)
		{
			var InDirection = CachedPosition - Pulling.Component.transform.position;
			if (InDirection.magnitude > 2f && (isServer || isLocalPlayer))
			{
				PullSet(null,false ); //TODO maybe remove
				if (isLocalPlayer && isServer == false) CmdStopPulling();
			}
			else
			{
				Pulling.Component.TryTilePush(InDirection.NormalizeTo2Int(), ByClient, speed);
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
		SetLocalTarget = transform.localPosition;
		newtonianMovement = Vector2.zero;
		airTime = 0;
		slideTime = 0;
		IsFlyingSliding = false;
		Animating = false;
	}

	[PlayModeOnly]
	public float Speed = 1;

	[PlayModeOnly]
	public float AIR = 3;

	[PlayModeOnly]
	public float SLIDE = 4;


	[NaughtyAttributes.Button()]
	[RightClickMethod()]
	public void DisplayStats()
	{
		Logger.LogError("registerTile.WorldPosition " + registerTile.WorldPosition.ToString());
		Logger.LogError("transform.position " + transform.position.ToString());

		Logger.LogError("registerTile.LocalPosition " + registerTile.LocalPosition.ToString());
		Logger.LogError("transform.localPosition" + transform.localPosition.ToString());

		Logger.LogError("registerTile.Matrix " + registerTile.Matrix.ToString());
		Logger.LogError("transform.parent.parent" + transform.parent.parent.ToString());
	}

	[RightClickMethod()]
	public void ThrowNoSlide()
	{
		NewtonianPush(PlayerManager.LocalPlayer.GetComponent<Rotatable>().CurrentDirection.ToLocalVector2Int(), Speed,
			AIR);
	}

	[RightClickMethod()]
	public void ThrowWithSlide()
	{
		NewtonianPush(PlayerManager.LocalPlayer.GetComponent<Rotatable>().CurrentDirection.ToLocalVector2Int(), Speed,
			AIR, SLIDE);
	}

	[RightClickMethod()]
	public void Slide()
	{
		NewtonianPush(PlayerManager.LocalPlayer.GetComponent<Rotatable>().CurrentDirection.ToLocalVector2Int(), Speed,
			INslideTime: SLIDE);
	}

	[RightClickMethod()]
	public void Push()
	{
		NewtonianPush(PlayerManager.LocalPlayer.GetComponent<Rotatable>().CurrentDirection.ToLocalVector2Int(), Speed);
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
		if (isServer && Synced == false) SynchroniseUpdatePulling(ThisPullData, new PullData() {NewPulling = ToPull, WasCausedByClient = ByClient});

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


	public void NewtonianPush(Vector2 WorldDirection, float speed = Single.NaN, float INairTime = Single.NaN,
		float INslideTime = Single.NaN, BodyPartType inaim = BodyPartType.Chest, GameObject inthrownBy = null,
		float spinFactor = 0) //Collision is just naturally part of Newtonian push
	{
		if (isNotPushable) return;

		aim = inaim;
		thrownBy = inthrownBy;
		if (Random.Range(0, 2) == 1)
		{
			spinMagnitude = spinFactor * 1;
		}
		else
		{
			spinMagnitude = spinFactor * -1;
		}


		if (float.IsNaN(INairTime) == false || float.IsNaN(INslideTime) == false)
		{
			WorldDirection.Normalize();
			if (newtonianMovement.magnitude > 0)
			{
				//Logger.LogError("  momentum boost");
			}
			newtonianMovement += WorldDirection * speed;
			if (float.IsNaN(INairTime) == false)
			{
				if (INairTime < 0)
				{
					Logger.LogError(INairTime.ToString());
				}
				airTime = INairTime;
			}

			if (float.IsNaN(INslideTime) == false)
			{
				slideTime = INslideTime;
			}
		}
		else
		{
			if (stickyMovement && IsFloating() == false)
			{
				return;
			}

			WorldDirection.Normalize();
			if (newtonianMovement.magnitude > 0)
			{
				//Logger.LogError("  momentum boost");
			}
			newtonianMovement += WorldDirection * speed;
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
					UpdateClientMomentum(transform.localPosition, newtonianMovement, airTime, slideTime,
						registerTile.Matrix.Id, spinFactor, true);
				}
			}
		}
	}

	public void AppliedFriction(float FrictionCoefficient)
	{
		var SpeedLossDueToFriction = FrictionCoefficient* Time.deltaTime;

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

	[PlayModeOnly]
	public bool Animating = false;

	[PlayModeOnly]
	private Vector3 LastDifference = Vector3.zero;

	public void AnimationUpdateMe()
	{
		if (this == null)
		{
			MoveIsWalking = false;
			UpdateManager.Remove(CallbackType.UPDATE, AnimationUpdateMe);
		}

		if (name == "DEBUG")
		{
			Logger.LogError("o3o");
		}


		Animating = true;
		var LocalPOS = transform.localPosition;

		IsMoving = LocalPOS != LocalTargetPosition;
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
				newtonianMovement += (Vector2)LastDifference.normalized * Cash;
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
							registerTile.Matrix.Id,spinMagnitude, false);
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

	[PlayModeOnly]
	public bool IsFlyingSliding = false;

	[PlayModeOnly]
	public bool IsMoving = false;

	public bool IsWalking => MoveIsWalking && IsMoving;

	[PlayModeOnly]
	public bool MoveIsWalking = false;

	[PlayModeOnly]
	public double LastUpdateClientFlying = 0; //NetworkTime.time

	public void FlyingUpdateMe()
	{
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

		transform.Rotate (new Vector3 (0, 0, spinMagnitude * newtonianMovement.magnitude* Time.deltaTime));

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
				SetMatrixCash.ResetNewPosition(intposition);
				Pushing.Clear();
				Bumps.Clear();
				Hits.Clear();
				if (MatrixManager.IsPassableAtAllMatricesV2(intposition,
					    intNewposition, SetMatrixCash, this,
					    Pushing, Bumps, Hits) == false)
				{
					foreach (var Bump in Bumps) //Bump Whatever we Bumped into
					{
						if (isServer)  Bump.OnBump(this.gameObject);
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
					newtonianMovement -= 2 * (newtonianMovement * Normal) * Normal;
					newtonianMovement *= 0.5f;
					spinMagnitude *= -1;
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
								LivingHealthMasterBase.ApplyDamageToBodyPart(thrownBy, damage, AttackType.Melee, DamageType.Brute,
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
					registerTile.Matrix.Id, spinMagnitude, true);
			}
		}


		if (newtonianMovement.magnitude < 0.01f) //Has slowed down enough
		{
			OnLocalTileReached.Invoke(transform.localPosition);
			if (onStationMovementsRound)
			{
				doNotApplyMomentumOnTarget = true;
				SetLocalTarget = registerTile.LocalPosition;
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
			SetMatrixCash.ResetNewPosition(registerTile.WorldPosition);
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

		return null;
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

		ConnectedPlayer clientWhoAsked = PlayerList.Instance.Get(gameObject);
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