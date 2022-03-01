using System;
using System.Collections;
using System.Collections.Generic;
using Items;
using Mirror;
using Objects;
using UnityEngine;
using UnityEngine.Serialization;

public class UniversalObjectPhysics : NetworkBehaviour
{


	public const float DEFAULT_Friction = 0.01f;
	public const float DEFAULT_SLIDE_FRICTION = 0.003f;


	public BoxCollider2D Collider;


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

	private LayerMask defaultInteractionLayerMask;

	public GameObject[] ContextGameObjects = new GameObject[2];


	public virtual void Awake()
	{
		Collider = this.GetComponent<BoxCollider2D>();
		ContextGameObjects[0] = gameObject;
		defaultInteractionLayerMask = LayerMask.GetMask("Furniture", "Walls", "Windows", "Machines", "Players",
			"Door Closed",
			"HiddenWalls", "Objects");
		attributes = GetComponent<Attributes>();
		registerTile = GetComponent<RegisterTile>();
	}


	public List<PushPull> Pushing;
	public List<IBumpableObject> Bumps;

	private void SyncIsNotPushable(bool wasNotPushable, bool isNowNotPushable)
	{
		this.isNotPushable = isNowNotPushable;
	}


	public void TryTilePush(Vector2Int WorldDirection, float speed = Single.NaN)
	{
		Pushing.Clear();
		Bumps.Clear();
		SetMatrixCash.ResetNewPosition(registerTile.WorldPosition);
		if (MatrixManager.IsPassableAtAllMatricesV2(registerTile.WorldPosition,
			    registerTile.WorldPosition + WorldDirection.To3Int(), SetMatrixCash, this.gameObject,
			    Pushing, Bumps)) 	//Validate
		{
			ForceTilePush(WorldDirection,Pushing ,speed);
		}
	}

	public void ForceTilePush(Vector2Int WorldDirection,  List<PushPull> InPushing, float speed = Single.NaN)
	{
		if (InPushing.Count > 0) //Has to push stuff
		{
			//Push Object
			foreach (var pushPull in InPushing)
			{
				pushPull.TryPush(WorldDirection);
			}
		}

		var NewWorldPosition = registerTile.WorldPosition + WorldDirection.To3Int();

		var movetoMatrix = SetMatrixCash.GetforDirection(WorldDirection.To3Int()).Matrix;


		if (registerTile.Matrix != movetoMatrix)
		{
			registerTile.ServerSetNetworkedMatrixNetID(movetoMatrix.NetworkedMatrix.MatrixSync.netId);
		}

		registerTile.ServerSetLocalPosition((NewWorldPosition).ToLocal(movetoMatrix).RoundToInt());
		registerTile.ClientSetLocalPosition((NewWorldPosition).ToLocal(movetoMatrix).RoundToInt());

		transform.position = NewWorldPosition;

	}


	public void NewtonianNewtonPush(Vector2Int WorldDirection, float Newtons = Single.NaN, float airTime = Single.NaN,
		float slideTime = Single.NaN) //Collision is just naturally part of Newtonian push
	{
		//TODO
	}


	public float Speed = 1;
	public float AIR = 3;
	public float SLIDE = 4;


	[RightClickMethod()]

	public void ThrowNoSlide()
	{
		NewtonianPush(PlayerManager.LocalPlayer.GetComponent<Rotatable>().CurrentDirection.ToLocalVector2Int(), Speed, AIR);
	}

	[RightClickMethod()]
	public void ThrowWithSlide()
	{
		NewtonianPush(PlayerManager.LocalPlayer.GetComponent<Rotatable>().CurrentDirection.ToLocalVector2Int(), Speed, AIR, SLIDE);
	}

	[RightClickMethod()]
	public void Slide()
	{
		NewtonianPush(PlayerManager.LocalPlayer.GetComponent<Rotatable>().CurrentDirection.ToLocalVector2Int(), Speed, INslideTime: SLIDE);
	}

	[RightClickMethod()]
	public void Push()
	{
		NewtonianPush(PlayerManager.LocalPlayer.GetComponent<Rotatable>().CurrentDirection.ToLocalVector2Int(), Speed);
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
			UpdateManager.Add( CallbackType.UPDATE ,UpdateMe);
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


	public void UpdateMe()
	{
		if (airTime > 0)
		{
			airTime -= Time.deltaTime;  //Doesn't matter if it goes under zero
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
		var Newposition = position + newtonianMovement.To3();

		var intposition = position.RoundToInt();
		var intNewposition = Newposition.RoundToInt();

		if (intposition != intNewposition)
		{
			Collider.enabled = false;
			var hit = MatrixManager.Linecast(position,
				LayerTypeSelection.Walls | LayerTypeSelection.Grills | LayerTypeSelection.Windows,
				defaultInteractionLayerMask, Newposition, true);
			if (hit.ItHit)
			{
				var Offset = (0.1f * hit.Normal);
				Newposition = hit.HitWorld + Offset.To3();
				newtonianMovement *= 0;
			}
			Collider.enabled = true;
		}


		this.transform.position = Newposition;

		var movetoMatrix = MatrixManager.AtPoint(Newposition.RoundToInt(), isServer).Matrix;

		if (registerTile.Matrix != movetoMatrix)
		{
			registerTile.ServerSetNetworkedMatrixNetID(movetoMatrix.NetworkedMatrix.MatrixSync.netId);
		}

		registerTile.ServerSetLocalPosition(
			(Newposition).ToLocal(movetoMatrix).RoundToInt()); //TODO Past matrix //TODO Update.damn client

		registerTile.ClientSetLocalPosition((Newposition).ToLocal(movetoMatrix).RoundToInt());

		if (newtonianMovement.magnitude < 0.01f) //Has slowed down enough
		{
			if (onStationMovementsRound)
			{
				this.transform.localPosition = registerTile.LocalPosition;
			}

			airTime = 0;
			slideTime = 0;
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
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