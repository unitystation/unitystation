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
	public const float DEFAULT_Friction = 0.01f;
	public const float DEFAULT_SLIDE_FRICTION = 0.003f;


	public BoxCollider2D Collider;

	public float TileMoveSpeed = 1;

	public float TileMoveSpeedOverride = 0;


	[SyncVar(hook = nameof(SyncLocalTarget))]
	public Vector3 LocalTargetPosition;

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
		LocalTargetPosition = transform.localPosition;
	}

	public virtual void OnEnable()
	{

	}

	public virtual void OnDisable()
	{

	}

	[TargetRpc]
	public void ForceSetPosition(Vector3 ReSetToLocal, Vector2 Momentum, bool Smooth)
	{

		if (Smooth)
		{
			if (IsFlyingSliding)
			{
				newtonianMovement = Momentum;
				LocalDifferenceNeeded = transform.localPosition - ReSetToLocal;

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
		LocalDifferenceNeeded -= (position - NewPosition).To2();

		transform.localPosition = NewPosition;

		if (LocalDifferenceNeeded.magnitude < 0.01f)
		{
			CorrectingCourse = false;
			UpdateManager.Remove(CallbackType.UPDATE, FloatingCourseCorrection);
		}
	}


	public List<PushPull> Pushing = new List<PushPull>();
	public List<IBumpableObject> Bumps = new List<IBumpableObject>();

	[TargetRpc]
	private void UpdateClientMomentum( Vector2 NewMomentum)
	{
		if (newtonianMovement != NewMomentum) return;
		if (isLocalPlayer) return; //We are updating other Objects than the player on the client
		newtonianMovement = NewMomentum;
		if (IsFlyingSliding == false) UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void SyncLocalTarget(Vector3 OLDLocalTarget, Vector3 NewLocalTarget)
	{
		if (LocalTargetPosition != NewLocalTarget) return;
		if (isLocalPlayer) return;
		LocalTargetPosition = NewLocalTarget;
		if (Animating == false && transform.localPosition != NewLocalTarget) UpdateManager.Add(CallbackType.UPDATE, AnimationUpdateMe);
	}

	private void SyncIsNotPushable(bool wasNotPushable, bool isNowNotPushable)
	{
		isNotPushable = isNowNotPushable;
	}


	public void TryTilePush(Vector2Int WorldDirection, float speed = Single.NaN)
	{
		Pushing.Clear();
		Bumps.Clear();
		SetMatrixCash.ResetNewPosition(transform.position);
		if (MatrixManager.IsPassableAtAllMatricesV2(transform.position,
			    transform.position+ WorldDirection.To3Int(), SetMatrixCash, this.gameObject,
			    Pushing, Bumps)) //Validate
		{
			ForceTilePush(WorldDirection, Pushing, speed);
		}
	}

	public void ForceTilePush(Vector2Int WorldDirection, List<PushPull> InPushing, float speed = Single.NaN)
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
			var TransformCash = transform.position;
			registerTile.ServerSetNetworkedMatrixNetID(movetoMatrix.NetworkedMatrix.MatrixSync.netId);
			transform.position = TransformCash;
		}


		var LocalPosition = (NewWorldPosition).ToLocal(movetoMatrix);

		registerTile.ServerSetLocalPosition(LocalPosition.RoundToInt());
		registerTile.ClientSetLocalPosition(LocalPosition.RoundToInt());

		LocalTargetPosition = LocalPosition.RoundToInt();
		if (Animating == false)
		{
			Animating = true;
			UpdateManager.Add(CallbackType.UPDATE, AnimationUpdateMe);
			TileMoveSpeedOverride = speed;
		}

		if (Pulling.HasComponent)
		{
			var InDirection = CachedPosition - Pulling.Component.transform.position;
			if (InDirection.magnitude > 2f)
			{
				PullSet(null); //TODO maybe remove
			}
			else
			{
				Pulling.Component.TryTilePush(InDirection.NormalizeTo2Int(), speed);
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


	[NaughtyAttributes.Button()][RightClickMethod()]
	public void DisplayStats()
	{
		Logger.LogError("registerTile.WorldPosition "  + registerTile.WorldPosition.ToString() );
		Logger.LogError("transform.position "  + transform.position.ToString() );

		Logger.LogError("registerTile.LocalPosition "  + registerTile.LocalPosition.ToString() );
		Logger.LogError("transform.localPosition"  + transform.localPosition.ToString() );

		Logger.LogError("registerTile.Matrix "  + registerTile.Matrix.ToString() );
		Logger.LogError("transform.parent.parent"  + transform.parent.parent.ToString() );

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

	[RightClickMethod()]
	public void PullReset()
	{
		PullSet(null);
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
				if (isServer) UpdateClientMomentum(newtonianMovement);
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

	private bool Animating = false;

	public void AnimationUpdateMe()
	{
		if (IsFlyingSliding)
		{
			return;//Animation handled by UpdateMe
		}

		Animating = true;
		var LocalPOS = transform.localPosition;

		IsMoving = LocalPOS != LocalTargetPosition;

		if (IsMoving)
		{
			if (TileMoveSpeedOverride > 0)
			{
				transform.localPosition = this.MoveTowards(LocalPOS, LocalTargetPosition,
					TileMoveSpeedOverride * Time.deltaTime ); //* transform.localPosition.SpeedTo(targetPos)
			}
			else
			{
				transform.localPosition = this.MoveTowards(LocalPOS, LocalTargetPosition,
					TileMoveSpeed * Time.deltaTime ); //* transform.localPosition.SpeedTo(targetPos)
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
					if (isServer) UpdateClientMomentum(newtonianMovement);
				}
			}
		}
	}

	public Vector3 MoveTowards(
		Vector3 current,
		Vector3 target,
		float maxDistanceDelta)
	{
		if ((current - target).magnitude > 3)
		{
			maxDistanceDelta *= 10;
		}

		return Vector3.MoveTowards(current, target,
			maxDistanceDelta);
	}

	public bool IsFlyingSliding = false;

	public bool IsMoving = false;

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
			var TransformCash = transform.position;
			registerTile.ServerSetNetworkedMatrixNetID(movetoMatrix.NetworkedMatrix.MatrixSync.netId);
			transform.position = TransformCash;
		}

		var LocalPosition = (Newposition).ToLocal(movetoMatrix);

		registerTile.ServerSetLocalPosition(LocalPosition.RoundToInt());
		registerTile.ClientSetLocalPosition(LocalPosition.RoundToInt());

		if (newtonianMovement.magnitude < 0.01f) //Has slowed down enough
		{
			if (onStationMovementsRound)
			{
				LocalTargetPosition = registerTile.LocalPosition;
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
			if (InDirection.magnitude > 2f)
			{
				PullSet(null); //TODO maybe remove
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
			var TransformCash = transform.position;
			registerTile.ServerSetNetworkedMatrixNetID(movetoMatrix.NetworkedMatrix.MatrixSync.netId);
			transform.position = TransformCash;
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
				initiator.CmdStopPulling();
			}
			else
			{
				initiator.PullSet(this);
				initiator.CmdPullObject(gameObject);
			}
		}
		else
		{
			initiator.PullSet(null);
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