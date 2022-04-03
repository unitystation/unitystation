using System;
using System.Collections;
using System.Collections.Generic;
using Items;
using Mirror;
using Objects;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Util;
using Random = UnityEngine.Random;

public class UniversalObjectPhysics : NetworkBehaviour, IRightClickable
{
	//TODO Tile push to clients if in pulling conga line
	//TODO if new pulling cancel old Pulling target
	//TODO Do more testing/Polishing with pulling in space with client/server
	//TODO PulledBy isn't getting synchronised to client properly on start

	//TODO Snap to LocalTargetPosition When triggered for client viewing other clients

	//TODO parentContainer!!
	//TODO check Conveyor belts with player movement


	public const float DEFAULT_Friction = 0.01f;
	public const float DEFAULT_SLIDE_FRICTION = 0.003f;


	public bool SnapToGridOnStart = false;

	public BoxCollider2D Collider;

	public float TileMoveSpeed = 1;

	public float TileMoveSpeedOverride = 0;


	public Vector3 LocalTargetPosition;


	public Vector3 SetLocalTarget
	{
		get { return LocalTargetPosition; }
		set
		{
			if (isServer) SynchLocalTargetPosition = value;
			//Logger.LogError("local target position set of "  + value + " On matrix " + registerTile.Matrix);
			LocalTargetPosition = value;
		}
	}

	public bool IsVisible => isVisible;

	[SyncVar(hook = nameof(SyncLocalTarget))]
	public Vector3 SynchLocalTargetPosition;

	[SyncVar(hook = nameof(SynchroniseVisibility))]
	private bool isVisible = true;

	[SyncVar(hook = nameof(SyncIsNotPushable))]
	private bool isNotPushable;

	[SyncVar(hook = nameof(SynchroniseUpdatePulling))]
	public PullData ThisPullData;

	[SyncVar(hook = nameof(SynchroniseParent))]
	private UniversalObjectPhysics parentContainer = null;

	public UniversalObjectPhysics ContainedInContainer => parentContainer;

	public Vector2 newtonianMovement; //* attributes.Size -> weight

	public float airTime; //Cannot grab onto anything so no friction

	public float slideTime;
	//Reduced friction during this time, if stickyMovement Just has normal friction vs just grabbing

	public GameObject thrownBy;

	public BodyPartType aim;

	public float spinMagnitude = 0; //TODO Multiplied by magnitude of movement Kind of a proportion would

	public bool stickyMovement = false;
	//If this thing likes to grab onto stuff such as like a player

	public float maximumStickSpeed = 1.5f;
	//Speed In tiles per second that, The thing would able to be stop itself if it was sticky

	public bool onStationMovementsRound;


	public bool IsNotPushable => isNotPushable;

	private Attributes attributes;
	public RegisterTile registerTile;

	public LayerMask defaultInteractionLayerMask;

	public GameObject[] ContextGameObjects = new GameObject[2];

	public bool IsCurrentlyFloating;


	public CheckedComponent<UniversalObjectPhysics> Pulling = new CheckedComponent<UniversalObjectPhysics>();
	public CheckedComponent<UniversalObjectPhysics> PulledBy = new CheckedComponent<UniversalObjectPhysics>();

	#region Events

	public ForceEvent OnThrowStart = new ForceEvent();
	public ForceEvent OnThrowEnd = new ForceEvent();

	public Vector3Event OnLocalTileReached = new Vector3Event();

	#endregion


	public virtual void Awake()
	{
		Collider = this.GetComponent<BoxCollider2D>();
		ContextGameObjects[0] = gameObject;
		defaultInteractionLayerMask = LayerMask.GetMask("Furniture", "Walls", "Windows", "Machines", "Players",
			"Door Closed",
			"HiddenWalls", "Objects");
		attributes = GetComponent<Attributes>();
		registerTile = GetComponent<RegisterTile>();
		SetLocalTarget = transform.localPosition;
	}

	public void Start()
	{
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


	private void SyncLocalTarget(Vector3 OLDLocalTarget, Vector3 NewLocalTarget)
	{
		if (isServer) return;
		if (LocalTargetPosition == NewLocalTarget) return;
		if (isLocalPlayer || PulledBy.HasComponent) return;
		LocalTargetPosition = NewLocalTarget;
		Logger.LogError(" new position for " + transform.name);
		if (IsFlyingSliding)
		{
			IsFlyingSliding = false;
			airTime = 0;
			slideTime = 0;
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		if (Animating == false && transform.localPosition != NewLocalTarget)
			UpdateManager.Add(CallbackType.UPDATE, AnimationUpdateMe);
	}


	public void StoreTo(UniversalObjectPhysics NewParent)
	{
		if (NewParent == this) return; //Storing something inside of itself what?

		parentContainer = NewParent; //TODO Disappear
		if (NewParent == null)
		{
			isVisible = true;
		}
		else
		{
			isVisible = false;
		}
	}
	//TODO Handle stuff like cart riding
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
		int MatrixID)
	{
		if (isLocalPlayer)
			return; //We are updating other Objects than the player on the client //TODO Also block if being pulled by local player
		newtonianMovement = NewMomentum;
		airTime = InairTime;
		slideTime = InslideTime;
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
			LocalTargetPosition = ReSetToLocal;
			transform.localPosition = ReSetToLocal;
			registerTile.ServerSetLocalPosition(ReSetToLocal.RoundToInt());
			registerTile.ClientSetLocalPosition(ReSetToLocal.RoundToInt());
		}

		if (Animating == false && IsFlyingSliding == false) UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
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

			ResetEverything();
		}
	}

	private void SynchroniseParent(UniversalObjectPhysics Old, UniversalObjectPhysics NewPulling)
	{
		parentContainer = NewPulling;
		//Visibility is handled by Server with Synchronised var
	}

	public void SynchroniseUpdatePulling(PullData Old, PullData NewPulling)
	{
		ThisPullData = NewPulling;
		if (NewPulling.WasCausedByClient && isLocalPlayer) return;
		PullSet(NewPulling.NewPulling);
	}

	public void AppearAtWorldPositionServer(Vector3 WorldPOS, bool Smooth = false)
	{
		var Matrix = MatrixManager.AtPoint(WorldPOS, isServer);
		ForceSetLocalPosition(WorldPOS.ToLocal(Matrix), Vector2.zero, Smooth, Matrix.Id);
	}

	public void DropAtAndInheritMomentum(UniversalObjectPhysics DroppedFrom)
	{
		ForceSetLocalPosition(DroppedFrom.transform.localPosition, DroppedFrom.newtonianMovement, false,
			DroppedFrom.registerTile.Matrix.Id);
	}

	public void DisappearFromWorld()
	{
		isVisible = false;
	}

	public void ForceSetLocalPosition(Vector3 ReSetToLocal, Vector2 Momentum, bool Smooth, int MatrixID,
		bool UpdateClient = true)
	{
		if (isServer && UpdateClient) RPCForceSetPosition(ReSetToLocal, Momentum, Smooth, MatrixID);
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
				LocalTargetPosition = ReSetToLocal;

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
				LocalTargetPosition = ReSetToLocal;
				transform.localPosition = ReSetToLocal;
				registerTile.ServerSetLocalPosition(ReSetToLocal.RoundToInt());
				registerTile.ClientSetLocalPosition(ReSetToLocal.RoundToInt());
			}
		}
	}

	[ClientRpc]
	public void RPCForceSetPosition(Vector3 ReSetToLocal, Vector2 Momentum, bool Smooth, int MatrixID)
	{
		ForceSetLocalPosition(ReSetToLocal, Momentum, Smooth, MatrixID, false);
	}


	public void ResetLocationOnClients(bool Smooth = false)
	{
		ForceSetLocalPosition(LocalTargetPosition, newtonianMovement, Smooth, registerTile.Matrix.Id);
		//Update client to server state
	}

	public Vector2 LocalDifferenceNeeded;
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


	public List<UniversalObjectPhysics> Pushing = new List<UniversalObjectPhysics>();
	public List<IBumpableObject> Bumps = new List<IBumpableObject>();

	private void SyncIsNotPushable(bool wasNotPushable, bool isNowNotPushable)
	{
		isNotPushable = isNowNotPushable;
	}


	public void SetMatrix(Matrix movetoMatrix)
	{
		var TransformCash = transform.position;
		registerTile.FinishNetworkedMatrixRegistration(movetoMatrix.NetworkedMatrix);
		transform.position = TransformCash;
		LocalDifferenceNeeded = Vector2.zero;
		LocalTargetPosition = transform.localPosition;
	}

	public bool CanPush(Vector2Int WorldDirection)
	{
		Pushing.Clear();
		Bumps.Clear();
		SetMatrixCash.ResetNewPosition(transform.position);
		if (MatrixManager.IsPassableAtAllMatricesV2(transform.position,
			    transform.position + WorldDirection.To3Int(), SetMatrixCash, this.gameObject,
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
		Pushing.Clear();
		Bumps.Clear();
		SetMatrixCash.ResetNewPosition(transform.position);
		if (MatrixManager.IsPassableAtAllMatricesV2(transform.position,
			    transform.position + WorldDirection.To3Int(), SetMatrixCash, this.gameObject,
			    Pushing, Bumps)) //Validate
		{
			ForceTilePush(WorldDirection, Pushing, ByClient, speed);
		}
	}

	public void ForceTilePush(Vector2Int WorldDirection, List<UniversalObjectPhysics> InPushing, bool ByClient,
		float speed = Single.NaN) //PushPull TODO Change to physics object
	{
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


		var LocalPosition = (NewWorldPosition).ToLocal(movetoMatrix);

		registerTile.ServerSetLocalPosition(LocalPosition.RoundToInt());
		registerTile.ClientSetLocalPosition(LocalPosition.RoundToInt());

		SetLocalTarget = LocalPosition.RoundToInt();
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
				PullSet(null); //TODO maybe remove
				if (isServer)
					ThisPullData = new PullData() {NewPulling = null, WasCausedByClient = false};
				else if (isLocalPlayer) CmdStopPulling();
			}
			else
			{
				Pulling.Component.TryTilePush(InDirection.NormalizeTo2Int(), ByClient, speed);
			}
		}
	}


	public void ResetEverything()
	{
		if (IsFlyingSliding) UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		if (Animating) UpdateManager.Remove(CallbackType.UPDATE, AnimationUpdateMe);
		if (CorrectingCourse) UpdateManager.Remove(CallbackType.UPDATE, FloatingCourseCorrection);


		LocalTargetPosition = transform.localPosition;
		newtonianMovement = Vector2.zero;
		airTime = 0;
		slideTime = 0;
		IsFlyingSliding = false;
		Animating = false;
	}


	public float Speed = 1;
	public float AIR = 3;
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
		GameObject inthrownBy = null) //Collision is just naturally part of Newtonian push
	{
		var speed = Newtonians / SizeToWeight(GetSize());
		NewtonianPush(WorldDirection, speed, INairTime, INslideTime, inaim, inthrownBy);
	}

	public void PullSet(UniversalObjectPhysics ToPull)
	{
		if (ToPull != null)
		{
			Pulling.DirectSetComponent(ToPull);
			ToPull.PulledBy.DirectSetComponent(this);
			ContextGameObjects[1] = ToPull.gameObject;
		}
		else
		{
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
		aim = inaim;
		thrownBy = inthrownBy;
		spinMagnitude = spinFactor;
		if (float.IsNaN(INairTime) == false || float.IsNaN(INslideTime) == false)
		{
			WorldDirection.Normalize();
			newtonianMovement += WorldDirection * speed;
			if (float.IsNaN(INairTime) == false)
			{
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
			newtonianMovement += WorldDirection * speed;
		}

		OnThrowStart.Invoke(this);
		if (newtonianMovement.magnitude > 0.01f)
		{
			//It's moving add to update manager
			if (IsFlyingSliding == false)
			{
				IsFlyingSliding = true;
				UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
				if (isServer)
					UpdateClientMomentum(transform.localPosition, newtonianMovement, airTime, slideTime,
						registerTile.Matrix.Id);
			}
		}
	}

	public void AppliedFriction(float FrictionCoefficient)
	{
		var Weight = SizeToWeight(GetSize());
		var SpeedLossDueToFriction = FrictionCoefficient * Weight;

		var NewMagnitude = newtonianMovement.magnitude - SpeedLossDueToFriction;

		if (NewMagnitude <= 0)
		{
			newtonianMovement *= 0;
		}
		else
		{
			newtonianMovement *= (NewMagnitude / newtonianMovement.magnitude);
		}
	}

	public bool Animating = false;

	public Vector3 LastDifference = Vector3.zero;

	public void AnimationUpdateMe()
	{
		if (IsFlyingSliding)
		{
			return; //Animation handled by UpdateMe
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

			if (IsFloating())
			{
				Logger.Log(LastDifference.normalized.ToString());
				NewtonianPush(LastDifference.normalized,Cash);
				LastDifference = Vector3.zero;
			}

			UpdateManager.Remove(CallbackType.UPDATE, AnimationUpdateMe);

			if (newtonianMovement.magnitude > 0.01f)
			{
				//It's moving add to update manager
				if (IsFlyingSliding == false)
				{
					IsFlyingSliding = true;
					UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
					if (isServer)
						UpdateClientMomentum(transform.localPosition, newtonianMovement, airTime, slideTime,
							registerTile.Matrix.Id);
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

	public bool IsFlyingSliding = false;

	public bool IsMoving = false;

	public double LastUpdateClientFlying = 0; //NetworkTime.time

	public void UpdateMe()
	{
		IsFlyingSliding = true;
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

		if (intposition != intNewposition)
		{
			// Collider.enabled = false;
			var hit = MatrixManager.Linecast(position,
				LayerTypeSelection.Walls | LayerTypeSelection.Grills | LayerTypeSelection.Windows,
				defaultInteractionLayerMask, Newposition, true);
			if (hit.ItHit)
			{
				var Offset = (0.1f * hit.Normal);
				Newposition = hit.HitWorld + Offset.To3();
				newtonianMovement *= 0;
			}
			// Collider.enabled = true;
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
					registerTile.Matrix.Id);
			}
		}


		if (newtonianMovement.magnitude < 0.01f) //Has slowed down enough
		{
			OnLocalTileReached.Invoke(transform.localPosition);
			if (onStationMovementsRound)
			{
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
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}


		if (Pulling.HasComponent)
		{
			var InDirection = CachedPosition - Pulling.Component.transform.position;
			if (InDirection.magnitude > 2f && (isServer || isLocalPlayer))
			{
				PullSet(null); //TODO maybe remove
				if (isServer) ThisPullData = new PullData() {NewPulling = null, WasCausedByClient = false};
				else if (isLocalPlayer) CmdStopPulling();
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
			if (MatrixManager.IsFloatingAtV2Tile(registerTile.WorldPosition, CustomNetworkManager.IsServer,
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
				initiator.PullSet(null);
				if (isServer) initiator.ThisPullData = new PullData() {NewPulling = null, WasCausedByClient = true};
				initiator.CmdStopPulling();
			}
			else
			{
				initiator.PullSet(this);
				if (isServer) initiator.ThisPullData = new PullData() {NewPulling = this, WasCausedByClient = true};
				initiator.CmdPullObject(gameObject);
			}
		}
		else
		{
			initiator.PullSet(null);
			if (isServer) initiator.ThisPullData = new PullData() {NewPulling = null, WasCausedByClient = true};
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

			PullSet(null);
			if (isServer) ThisPullData = new PullData() {NewPulling = null, WasCausedByClient = true};
		}

		ConnectedPlayer clientWhoAsked = PlayerList.Instance.Get(gameObject);
		if (Validations.CanApply(clientWhoAsked.Script, gameObject, NetworkSide.Server) == false)
		{
			return;
		}

		if (Validations.IsReachableByRegisterTiles(pullable.registerTile, this.registerTile, true,
			    context: pullableObject))
		{
			PullSet(pullable);
			if (isServer) ThisPullData = new PullData() {NewPulling = pullable, WasCausedByClient = true};
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.ThudSwoosh, pullable.transform.position,
				sourceObj: pullableObject);
			//TODO Update the UI
		}
	}

	/// Client requests to stop pulling any objects
	[Command]
	public void CmdStopPulling()
	{
		PullSet(null);
		if (isServer) ThisPullData = new PullData() {NewPulling = null, WasCausedByClient = true};
	}

	public void StopPulling()
	{
		CmdStopPulling();
		PullSet(null);
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