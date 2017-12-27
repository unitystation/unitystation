using System;
using Tilemaps.Scripts;
using Tilemaps.Scripts.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

// ReSharper disable CompareOfFloatsByEqualityOperator

public struct TransformState
{
	public bool Active;
	public float Speed;
	public Vector2 Impulse; //Should always be prepared via PrepareImpulseVector
	public Vector3 Position; //localPosition!

	public Vector3 position
	{
		get
		{
			if ( Position == CustomNetTransform.InvalidPos )
			{
				return Position;
			}
			return CustomNetTransform.localToWorld(Position);
		}
		set
		{
			if ( value == CustomNetTransform.InvalidPos )
			{
				Position = value;
			}
			else
			{
				Position = CustomNetTransform.worldToLocal(value);
			}
		}
	}
	public Vector3 localPos
	{
		get
		{
			return Position;
		}
		set { Position = value; }
	}
}

public class CustomNetTransform : ManagedNetworkBehaviour //see UpdateManager
{
	public static readonly Vector3Int InvalidPos = new Vector3Int(0, 0, -100), deOffset = new Vector3Int(-1, -1, 0);

	public float SpeedMultiplier = 1; //Multiplier for flying/lerping speed, could indicate weight, for example

	private RegisterTile registerTile;

	private TransformState serverTransformState; //used for syncing with players, matters only for server
	private TransformState transformState;

	private Matrix matrix;

	public override void OnStartServer()
	{
		InitServerState();
		base.OnStartServer();
	}

	protected override void OnEnable()
	{
		base.OnEnable();
	}

	public TransformState State => serverTransformState;

	private void InitServerState()
	{
		if ( isServer )
		{
			serverTransformState.Speed = SpeedMultiplier;
			if ( !transform.localPosition.Equals(Vector3.zero) )
			{
				serverTransformState.Active = true;
				serverTransformState.localPos =
					Vector3Int.RoundToInt(new Vector3(transform.localPosition.x, transform.localPosition.y, 0));
			}
			else
			{
				//For stuff hidden on spawn, like player equipment
				serverTransformState.Active = false;
				serverTransformState.localPos = InvalidPos;
			}
		}
	}

	/// Manually set an item to a specific position
	[Server]
	public void SetPosition(Vector3 pos, bool notify = true)
	{
		serverTransformState.localPos = pos;
		if ( notify )
		{
			NotifyPlayers();
		}
	}


	//    /// Overwrite server state with a completely new one. (Are you sure you really want that?)
//    [Server]
//    public void SetState(TransformState state)
//    {
//        serverTransformState = state;
//        NotifyPlayers();
//    }

	private Vector2 PrepareImpulseVector(Vector2 impulse)
	{
		var newImpulse = impulse.normalized;// * 1.5f;
//		Debug.LogFormat($"{impulse}->{impulse.normalized}");//->{newImpulse}
		return newImpulse;
	}
	
	//FIXME: deOffset is a temp solution to this weird matrix 0.5;0.5 offset
	public static Vector3 localToWorld(Vector3 localVector3)
	{
		return localVector3 - deOffset;
	}
	
	public static Vector3 worldToLocal(Vector3 worldVector3)
	{
		return worldVector3 + deOffset;
	}

	/// <summary>
	/// Dropping with some force, in random direction. For space floating demo purposes.
	/// </summary>
	[Server]
	public void ForceDrop(Vector3 pos)
	{
		GetComponentInChildren<SpriteRenderer>().color = Color.white;
		serverTransformState.Active = true;
		serverTransformState.position = pos;
		Vector2	impulse = PrepareImpulseVector(Random.insideUnitCircle);//PrepareImpulseVector(new Vector2(0.8f,1)); 
		//don't apply impulses if item isn't going to float in that direction
		var newGoal = Vector3Int.RoundToInt(serverTransformState.localPos + ( Vector3 ) impulse);
		if ( CanDriftTo(newGoal) )
		{
			Debug.LogFormat($"ForceDrop success: from {pos} to {localToWorld(newGoal)}");
			serverTransformState.Impulse = impulse;
			serverTransformState.Speed = Random.Range(1f, 2f);
		}
		else
		{
			Debug.LogWarningFormat($"ForceDrop fail: from {pos} to {localToWorld(newGoal)}");			
		}
		NotifyPlayers();
	}

	[Server]
	public void DisappearFromWorldServer()
	{
		serverTransformState.Active = false;
		serverTransformState.localPos = InvalidPos;
		NotifyPlayers();
	}

	[Server]
	public void AppearAtPositionServer(Vector3 pos)
	{
		serverTransformState.Active = true;
		SetPosition(pos);
	}

	/// <summary>
	/// Method to substitute transform.parent = x stuff.
	/// You shouldn't really use it anymore, 
	/// as there are high level methods that should suit your needs better.
	/// Server-only, client is not being notified
	/// </summary>
	[Server]
	public void SetParent(Transform pos)
	{
		transform.parent = pos;
	}

	/// <summary>
	/// Convenience method to make stuff disappear at position.
	/// For CLIENT prediction purposes.
	/// </summary>
	public void DisappearFromWorld()
	{
		transformState.Active = false;
		transformState.position = InvalidPos;
		updateActiveStatus();
	}

	/// <summary>
	/// Convenience method to make stuff appear at position
	/// For CLIENT prediction purposes.
	/// </summary>
	public void AppearAtPosition(Vector3 pos)
	{
		transformState.Active = true;
		transformState.position = pos;
		updateActiveStatus();
	}

	public void UpdateClientState(TransformState newState)
	{
		//Don't lerp (instantly change pos) if active state was changed or speed is zero
		if ( transformState.Active != newState.Active || newState.Speed == 0 )
		{
			transform.localPosition = newState.localPos;
		}
		transformState = newState;

		updateActiveStatus();
	}

	private void updateActiveStatus()
	{
		if ( transformState.Active )
		{
			RegisterObjects();
		}
		else
		{
			registerTile.Unregister();
		}
		//todo: Consider moving VisibleBehaviour functionality to CNT. Currently VB doesn't allow predictive object hiding, for example. 
		VisibleBehaviour vb = gameObject.GetComponent<VisibleBehaviour>();
		vb.visibleState = transformState.Active; //this a syncvar -> only works for server
		vb.UpdateState(transformState.Active); //Hack to make VB work with clientside prediction
	}

	/// <summary>
	/// Currently sending to everybody, but should be sent to nearby players only
	/// </summary>
	[Server]
	private void NotifyPlayers()
	{
		TransformStateMessage.SendToAll(gameObject, serverTransformState);
	}

	/// <summary>
	/// sync with new player joining
	/// </summary>
	/// <param name="playerGameObject"></param>
	[Server]
	public void NotifyPlayer(GameObject playerGameObject)
	{
		TransformStateMessage.Send(playerGameObject, gameObject, serverTransformState);
	}


	void Start()
	{
		registerTile = GetComponent<RegisterTile>();
		matrix = Matrix.GetMatrix(this);
	}

	//managed by UpdateManager

	public override void UpdateMe()
	{
		if ( !registerTile )
		{
			registerTile = GetComponent<RegisterTile>();
		}
		Synchronize();
	}

	private void RegisterObjects()
	{
		//Register item pos in matrix
		registerTile.UpdatePosition();
	}

	private void Synchronize()
	{
		if ( !transformState.Active )
		{
			return;
		}

		if ( isServer )
		{
			CheckSpaceDrift();
		}

//        if ( GameData.IsHeadlessServer )
//        {
//            return;
//        }

		if ( IsFloating() )
		{
			//predictive perpetual flying
			transformState.localPos += ( Vector3 ) transformState.Impulse * ( transformState.Speed * SpeedMultiplier ) * Time.deltaTime; 
		}

		if ( transformState.localPos != transform.localPosition )
		{
			Lerp();
		}

		//Registering
		if ( registerTilePos() != Vector3Int.FloorToInt(transformState.localPos) )
		{
			Debug.LogFormat($"registerTile updating {localToWorld(registerTilePos())}->{localToWorld(Vector3Int.FloorToInt(transform.localPosition))}, " +
			                $"ts={localToWorld(Vector3Int.FloorToInt(transformState.localPos))}");
			RegisterObjects();
		}
	}

	private Vector3Int registerTilePos()
	{
		return registerTile.Position /*- deOffset*/;
	}

	private void Lerp()
	{
		transform.localPosition = Vector3.MoveTowards(transform.localPosition, transformState.localPos, ( transformState.Speed * SpeedMultiplier ) * Time.deltaTime);
	}

	/// <summary>
	/// Space drift detection is serverside only
	/// </summary>
	[Server]
	private void CheckSpaceDrift()
	{
		if ( IsFloating() && matrix != null )
		{
			Vector3 newGoal = serverTransformState.localPos + 
			             ( Vector3 ) serverTransformState.Impulse * ( serverTransformState.Speed * SpeedMultiplier ) * Time.deltaTime;
			/*= Vector3.MoveTowards(serverTransformState.localPos, 
					serverTransformState.localPos + (Vector3) serverTransformState.Impulse, 
					serverTransformState.Speed * SpeedMultiplier * Time.deltaTime);*/
			Vector3Int intGoal = Vector3Int.RoundToInt(newGoal);

			if ( CanDriftTo(intGoal) )
			{
				//Spess drifting
				serverTransformState.localPos = newGoal;
			}
			else //Stopping drift
			{
//				serverTransformState.Position = newGoal - ( Vector3 ) serverTransformState.Impulse;//registerTilePos();
//				serverTransformState.Position += ( Vector3 ) serverTransformState.Impulse * ( serverTransformState.Speed * SpeedMultiplier ) * Time.deltaTime;
				Debug.LogFormat($"{gameObject.name}: not floating to {localToWorld(intGoal)}, stopped @{serverTransformState.position} (transform.pos={localToWorld(transform.localPosition)})");
				GetComponentInChildren<SpriteRenderer>().color = Color.red;
				serverTransformState.Impulse = Vector2.zero; //killing impulse, be aware when implementing throw!
				NotifyPlayers();
			}
		}
	}

	public bool IsFloating()
	{
		if ( isServer )
		{
			return serverTransformState.Impulse != Vector2.zero && serverTransformState.Speed != 0f;
		}
		return transformState.Impulse != Vector2.zero && transformState.Speed != 0f;
	}

	private bool CanDriftTo(Vector3Int goal)
	{
		var worldGoal = localToWorld(goal);
		bool canDriftTo = matrix != null && matrix.IsEmptyAt(goal);
		return canDriftTo;
	}
}