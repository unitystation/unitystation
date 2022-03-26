using System;
using System.Collections;
using System.Collections.Generic;
using Items;
using Mirror;
using Objects;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Serialization;
using Util;

public class UniversalObjectPhysics : NetworkBehaviour, IRightClickable
{
	//TODO Tile push to clients if in pulling conga line
	//TODO if new pulling cancel old Pulling target
	//TODO PulledBy isn't getting synchronised to client properly on start

	//TODO Snap to LocalTargetPosition When triggered for client viewing other clients
	public const float DEFAULT_Friction = 0.01f;
	public const float DEFAULT_SLIDE_FRICTION = 0.003f;


	public BoxCollider2D Collider;

	public float TileMoveSpeed = 1;

	public float TileMoveSpeedOverride = 0;


	public Vector3 LocalTargetPosition;

	[SyncVar(hook = nameof(SyncLocalTarget))]
	public Vector3 SynchLocalTargetPosition;

	public Vector3 SetLocalTarget
	{
		get
		{
			return LocalTargetPosition;
		}
		set
		{
			if (isServer) SynchLocalTargetPosition = value;
			//Logger.LogError("local target position set of "  + value + " On matrix " + registerTile.Matrix);
			LocalTargetPosition = value;
		}
	}


	public Vector2 newtonianMovement; //* attributes.Size -> weight

	public float airTime; //Cannot grab onto anything so no friction

	public float slideTime;
	//Reduced friction during this time, if stickyMovement Just has normal friction vs just grabbing

	public bool stickyMovement = false;
	//If this thing likes to grab onto stuff such as like a player

	public float maximumStickSpeed = 1.5f;
	//Speed In tiles per second that, The thing would able to be stop itself if it was sticky

	public bool onStationMovementsRound;

	[SyncVar(hook = nameof(SyncIsNotPushable))]
	private bool isNotPushable;

	private Attributes attributes;
	protected RegisterTile registerTile;

	public LayerMask defaultInteractionLayerMask;

	public GameObject[] ContextGameObjects = new GameObject[2];

	[SyncVar(hook = nameof(SynchroniseUpdatePulling))]
	public PullData ThisPullData;

	public CheckedComponent<UniversalObjectPhysics> Pulling = new CheckedComponent<UniversalObjectPhysics>();
	public CheckedComponent<UniversalObjectPhysics> PulledBy = new CheckedComponent<UniversalObjectPhysics>();

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

	public override void OnStartClient()
	{
		base.OnStartClient();

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



	[ClientRpc]
	public void RPCClientTilePush(Vector2Int WorldDirection, float speed, bool CausedByClient)
	{
		if (isLocalPlayer && CausedByClient) return;
		if (PulledBy.HasComponent) return;
		Pushing.Clear();
		//Logger.LogError("ClientRpc Tile push for " + transform.name + " Direction " + WorldDirection.ToString());
		SetMatrixCash.ResetNewPosition(transform.position);
		ForceTilePush(WorldDirection,Pushing, CausedByClient , speed);
	}


	[ClientRpc]
	public void UpdateClientMomentum(Vector3 ReSetToLocal,  Vector2 NewMomentum, float InairTime, float  InslideTime, int MatrixID )
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

	public void SynchroniseUpdatePulling(PullData Old, PullData NewPulling)
	{
		ThisPullData = NewPulling;
		if (NewPulling.WasCausedByClient && isLocalPlayer) return;
		PullSet(NewPulling.NewPulling);
	}

	[ClientRpc]
	public void ForceSetPosition(Vector3 ReSetToLocal, Vector2 Momentum, bool Smooth, int MatrixID )
	{
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


	public List<PushPull> Pushing = new List<PushPull>();
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

	public void ForceTilePush(Vector2Int WorldDirection, List<PushPull> InPushing, bool ByClient, float speed = Single.NaN) //PushPull TODO Change to physics object
	{
		if (float.IsNaN(speed))
		{
			speed = TileMoveSpeed;
		}

		if (InPushing.Count > 0) //Has to push stuff
		{
			//Push Object
			foreach (var pushPull in InPushing)
			{
				pushPull.TryPush(WorldDirection, speed);
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

		if (isServer && PulledBy.HasComponent == false) RPCClientTilePush(WorldDirection, speed,ByClient); //TODO Local direction

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


	public void NewtonianNewtonPush(Vector2Int WorldDirection, float Newtons = Single.NaN, float airTime = Single.NaN,
		float slideTime = Single.NaN) //Collision is just naturally part of Newtonian push
	{
		//TODO
		//Check performance of adding and removing from Update list
		//Work out the same speed for when flying and not
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

	public void PullSet(UniversalObjectPhysics ToPull)
	{
		if (ToPull != null)
		{
			Pulling.DirectSetComponent(ToPull);
			ToPull.PulledBy.DirectSetComponent(this);
		}
		else
		{
			if (Pulling.HasComponent)
			{
				Pulling.Component.PulledBy.SetToNull();
				Pulling.SetToNull();
			}
		}
	}


	public void NewtonianPush(Vector2 WorldDirection, float speed = Single.NaN, float INairTime = Single.NaN,
		float INslideTime = Single.NaN) //Collision is just naturally part of Newtonian push
	{
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

		if (newtonianMovement.magnitude > 0.01f)
		{
			//It's moving add to update manager
			if (IsFlyingSliding == false)
			{
				IsFlyingSliding = true;
				UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
				if (isServer) UpdateClientMomentum(transform.localPosition, newtonianMovement, airTime, slideTime, registerTile.Matrix.Id );
			}
		}
	}

	public void AppliedFriction(float FrictionCoefficient)
	{
		var Weight = SizeToWeight(attributes ? attributes.Size : Size.Huge);
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
		}
		else
		{
			TileMoveSpeedOverride = 0;
			Animating = false;
			UpdateManager.Remove(CallbackType.UPDATE, AnimationUpdateMe);
			if (newtonianMovement.magnitude > 0.01f)
			{
				//It's moving add to update manager
				if (IsFlyingSliding == false)
				{
					IsFlyingSliding = true;
					UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
					if (isServer) UpdateClientMomentum(transform.localPosition, newtonianMovement, airTime, slideTime, registerTile.Matrix.Id );
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
			if ( NetworkTime.time - LastUpdateClientFlying > 2)
			{
				LastUpdateClientFlying = NetworkTime.time;
				UpdateClientMomentum(transform.localPosition, newtonianMovement, airTime, slideTime, registerTile.Matrix.Id );
			}
		}


		if (newtonianMovement.magnitude < 0.01f) //Has slowed down enough
		{
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
					return true;
				}
			}

			return false;
		}
		else
		{
			if (registerTile.Matrix.HasGravity) //Presuming Register tile has the correct matrix
			{
				if (registerTile.Matrix.MetaTileMap.IsEmptyTileMap(registerTile.LocalPosition) == false)
				{
					return false;
				}
			}

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
				if (isServer)  initiator.ThisPullData = new PullData() {NewPulling = null, WasCausedByClient = true};
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
			Size.Tiny => 0.1f,
			Size.Small => 0.5f,
			Size.Medium => 1f,
			Size.Large => 3f,
			Size.Massive => 10f,
			Size.Humongous => 50f,
			_ => 1f
		};
	}
}