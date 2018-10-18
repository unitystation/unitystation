using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public enum SpinMode {
	None,
	Clockwise,
	CounterClockwise
}

public struct ThrowInfo
{
	/// Null object, means that there's no throw in progress
	public static readonly ThrowInfo NoThrow =
		new ThrowInfo{ OriginPos = TransformState.HiddenPos, TargetPos = TransformState.HiddenPos };
	public Vector3 OriginPos;
	public Vector3 TargetPos;
	public GameObject ThrownBy;
	public BodyPartType Aim;
	public float InitialSpeed;
	public SpinMode SpinMode;
	public Vector3 Trajectory => TargetPos - OriginPos;

	public override string ToString() {
		return Equals(NoThrow) ? "[No throw]" :
			$"[{nameof( OriginPos )}: {OriginPos}, {nameof( TargetPos )}: {TargetPos}, {nameof( ThrownBy )}: {ThrownBy}, " +
			$"{nameof( Aim )}: {Aim}, {nameof( InitialSpeed )}: {InitialSpeed}, {nameof( SpinMode )}: {SpinMode}]";
	}
}

public partial class CustomNetTransform {
	private PushPull pushPull;
	private int PushSpeed = 6;
	public PushPull PushPull => pushPull ? pushPull : ( pushPull = GetComponent<PushPull>() );

	/// Containers and other objects meant to be snapped by tile
	public bool IsTileSnap => registerTile.ObjectType == ObjectType.Object;

//	public bool IsInSpace => MatrixManager.IsEmptyAt( Vector3Int.RoundToInt( transform.position ) );
	public bool IsFloatingServer => serverState.Impulse != Vector2.zero && serverState.Speed > 0f;
	public bool IsFloatingClient => clientState.Impulse != Vector2.zero && clientState.Speed > 0f;
	public bool IsBeingThrown => !serverState.ActiveThrow.Equals( ThrowInfo.NoThrow );

	private LayerMask tileDmgMask;


	/// (Server) Did the flying item reach the planned landing point?
	private bool ShouldStopThrow {
		get {
			if ( !IsBeingThrown ) {
				return true;
			}

			bool shouldStop =
				Vector3.Distance( serverState.ActiveThrow.OriginPos, serverState.WorldPosition ) >= serverState.ActiveThrow.Trajectory.magnitude;
//			if ( shouldStop ) {
//				Logger.Log( $"Should stop throw: {Vector3.Distance( serverState.ActiveThrow.OriginPos, serverState.WorldPosition )}" +
//				           $" >= {trajectory.magnitude}" );
//			}
			return shouldStop;
		}
	}

	[Server]
	public bool Push( Vector2Int direction ) {
		Vector2 target = ( Vector2 ) serverState.WorldPosition + direction;
		serverState.Speed = PushSpeed; //?
		if (MatrixManager.IsEmptyAt( Vector3Int.RoundToInt(target) )) {
			serverState.Impulse = direction;
		}

		SetPosition( target );
		return true;
	}

	public bool PredictivePush( Vector2Int direction ) {
//		return false;
		Vector2 target = ( Vector2 ) clientState.WorldPosition + direction;
		clientState.Speed = PushSpeed;
		if (MatrixManager.IsEmptyAt( Vector3Int.RoundToInt(target) )) {
			clientState.Impulse = direction;
		}

		clientState.WorldPosition = target;
		return true;
	}

	public bool CanPredictPush => !IsClientLerping;
	public bool IsClientLerping => transform.localPosition != MatrixManager.WorldToLocal( clientState.WorldPosition, MatrixManager.Get( matrix ) );

	/// Predictive client movement
	/// Mimics server collision checks for obviously impassable things.
	/// That prevents objects going through walls if server doesn't respond in time
	private void CheckFloatingClient() {
		CheckFloatingClient(TransformState.HiddenPos);
	}
	private void CheckFloatingClient(Vector3 goal) {
		if ( !IsFloatingClient ) {
			return;
		}
		bool isRecursive = goal != TransformState.HiddenPos;
		Vector3Int intOrigin = Vector3Int.RoundToInt( clientState.WorldPosition );

		Vector3 moveDelta;
		if ( !isRecursive ) { //Normal delta if not recursive
			moveDelta = ( Vector3 ) clientState.Impulse * clientState.Speed * Time.deltaTime;
		} else { //Artificial delta if recursive
			moveDelta = goal - clientState.WorldPosition;
		}

		float distance = moveDelta.magnitude;
		Vector3 newGoal;

		if ( distance > 1 ) {
			//limit goal to just one tile away and run this method recursively afterwards
			newGoal = clientState.WorldPosition + ( Vector3 ) clientState.Impulse;
		} else {
			newGoal = clientState.WorldPosition + moveDelta;
		}
		Vector3Int intGoal = Vector3Int.RoundToInt( newGoal );

		bool isWithinTile = intOrigin == intGoal; //same tile, no need to validate stuff
		if ( isWithinTile || CanDriftTo( intOrigin, intGoal ) ) {
			//advance
			clientState.WorldPosition += moveDelta;
		} else {
			//stop
			Logger.Log( $"{gameObject.name}: predictive stop @ {clientState.WorldPosition} to {intGoal}" );
//			clientState.Speed = 0f;
			clientState.Impulse = Vector2.zero;
			clientState.SpinFactor = 0;
		}

		if ( distance > 1 ) {
			CheckFloatingClient(isRecursive ? goal : newGoal);
		}
	}

	/// Clientside lerping (transform to clientState position)
	private void Lerp() {
		Vector3 targetPos = MatrixManager.WorldToLocal( clientState.WorldPosition, MatrixManager.Get( matrix ) );
		//Set position immediately if not moving
		if ( clientState.Speed.Equals( 0 ) ) {
			transform.localPosition = targetPos;
			onClientTileReached.Invoke( Vector3Int.RoundToInt(clientState.WorldPosition) );
			return;
		}
		transform.localPosition =
			Vector3.MoveTowards( transform.localPosition, targetPos, clientState.Speed * Time.deltaTime );
		if ( transform.localPosition == targetPos ) {
			onClientTileReached.Invoke( Vector3Int.RoundToInt(clientState.WorldPosition) );
		}
	}
	/// Serverside lerping
	private void ServerLerp() {
		Vector3 targetPos = MatrixManager.WorldToLocal( serverState.WorldPosition, MatrixManager.Get( matrix ) );
		//Set position immediately if not moving
		if ( serverState.Speed.Equals( 0 ) ) {
			serverLerpState = serverState;
			onTileReached.Invoke( Vector3Int.RoundToInt(serverState.WorldPosition) );
			return;
		}
		serverLerpState.Position =
			Vector3.MoveTowards( serverLerpState.Position, targetPos, serverState.Speed * Time.deltaTime );

		if ( serverLerpState.Position == targetPos ) {
			onTileReached.Invoke( Vector3Int.RoundToInt(serverState.WorldPosition) );
		}
	}

	/// Drop with some inertia.
	/// Currently used by players dropping something while space floating
	[Server]
	public void InertiaDrop( Vector3 initialPos, float speed, Vector2 impulse ) {
		SetPosition( initialPos, false );
		serverState.Impulse = impulse;
		serverState.Speed = Random.Range( -0.3f, 0f ) + speed;
		NotifyPlayers();
	}

	/// Throw object using data provided in ThrowInfo.
	/// Range will be limited by itemAttributes
	[Server]
	public void Throw( ThrowInfo info ) {
		SetPosition( info.OriginPos, false );

		float throwSpeed = ItemAttributes.throwSpeed * 10; //tiles per second
		float throwRange = ItemAttributes.throwRange;

		Vector2 impulse = info.Trajectory.normalized;

		var correctedInfo = info;
		//limit throw range here
		if ( Vector2.Distance( info.OriginPos, info.TargetPos ) > throwRange ) {
			correctedInfo.TargetPos = info.OriginPos + ( ( Vector3 ) impulse * throwRange );
//			Logger.Log( $"Throw distance clamped to {correctedInfo.Trajectory.magnitude}, " +
//			           $"target changed {info.TargetPos}->{correctedInfo.TargetPos}" );
		}

		//add player momentum
		float playerMomentum = 0f;
		//If throwing nearby, do so at 1/2 speed (looks clunky otherwise)
		float speedMultiplier = Mathf.Clamp( correctedInfo.Trajectory.magnitude / (throwRange <= 0 ? 1 : throwRange), 0.6f, 1f );
		serverState.Speed = ( Random.Range( -0.2f, 0.2f ) + throwSpeed + playerMomentum ) * speedMultiplier;
		correctedInfo.InitialSpeed = serverState.Speed;

		serverState.Impulse = impulse;
		if ( info.SpinMode != SpinMode.None ) {
			serverState.SpinFactor = ( sbyte ) ( Mathf.Clamp( throwSpeed * (2f / (int)ItemAttributes.size + 1), sbyte.MinValue, sbyte.MaxValue )
			                                     * ( info.SpinMode == SpinMode.Clockwise ? 1 : -1 ) );
		}
		serverState.ActiveThrow = correctedInfo;
//		Logger.Log( $"Throw:{correctedInfo} {serverState}" );
		NotifyPlayers();
	}

	/// Dropping with some force, in random direction. For space floating demo purposes.
	[Server]
	public void ForceDrop( Vector3 pos ) {
//		GetComponentInChildren<SpriteRenderer>().color = Color.white;
		SetPosition( pos, false );
		Vector2 impulse = Random.insideUnitCircle.normalized;
		//don't apply impulses if item isn't going to float in that direction
		Vector3Int newGoal = CeilWithContext( serverState.WorldPosition, impulse );
		if ( MatrixManager.IsNoGravityAt( newGoal ) ) {
			serverState.Impulse = impulse;
			serverState.Speed = Random.Range( 0.2f, 2f );
		}

		NotifyPlayers();
	}

	/// Server movement checks
	[Server]
	private void CheckFloatingServer() {
		CheckFloatingServer(TransformState.HiddenPos);
	}
	[Server]
	private void CheckFloatingServer(Vector3 goal) {
		if ( !IsFloatingServer || matrix == null ) {
			return;
		}
		bool isRecursive = goal != TransformState.HiddenPos;

		Vector3 moveDelta;
		if ( !isRecursive ) {//Normal delta if not recursive
			moveDelta = ( Vector3 ) serverState.Impulse * serverState.Speed * Time.deltaTime;
		} else {//Artificial delta if recursive
			moveDelta = goal - serverState.WorldPosition;
		}

		Vector3Int intOrigin = Vector3Int.RoundToInt( serverState.WorldPosition );
		float distance = moveDelta.magnitude;
		Vector3 newGoal;

		if ( distance > 1 ) {
			//limit goal to just one tile away and run this method recursively afterwards
			newGoal = serverState.WorldPosition + ( Vector3 ) serverState.Impulse;
		} else {
			newGoal = serverState.WorldPosition + moveDelta;
		}
		Vector3Int intGoal = Vector3Int.RoundToInt( newGoal );

		bool isWithinTile = intOrigin == intGoal; //same tile, no need to validate stuff
		if ( isWithinTile || ValidateFloating( serverState.WorldPosition, newGoal ) ) {
			AdvanceMovement( serverState.WorldPosition, newGoal );
		} else {
			StopFloating();
		}

		if ( distance > 1 ) {
			CheckFloatingServer(isRecursive ? goal : newGoal);
		}
	}

	[Server]
	private void AdvanceMovement( Vector3 tempOrigin, Vector3 tempGoal ) {
		//Natural throw ending
		if ( IsBeingThrown && ShouldStopThrow ) {
//			Logger.Log( $"{gameObject.name}: Throw ended at {serverState.WorldPosition}" );
			serverState.ActiveThrow = ThrowInfo.NoThrow;
			//Change spin when we hit the ground. Zero was kinda dull
			serverState.SpinFactor = ( sbyte ) ( -serverState.SpinFactor * 0.2f );
			//todo: ground hit sound
		}

		serverState.WorldPosition = tempGoal;
		//Spess drifting is perpetual, but speed decreases each tile if object has landed (no throw) on the floor
		if ( !IsBeingThrown && !MatrixManager.IsEmptyAt( Vector3Int.RoundToInt( tempOrigin ) ) ) {
			//on-ground resistance

			//no slide inertia for tile snapped objects like closets
			if ( IsTileSnap ) {
				StopFloating();
				return;
			}
			serverState.Speed = serverState.Speed - ( serverState.Speed * 0.10f ) - 0.5f;
			if ( serverState.Speed <= 0.05f ) {
				StopFloating();
			} else {
				NotifyPlayers();
			}
		}
	}

	/// Verifies if we can proceed to the next tile and hurts objects if we can not
	/// This check works only for 2 adjacent tiles, that's why floating check is recursive
	[Server]
	private bool ValidateFloating( Vector3 origin, Vector3 goal ) {
//		Logger.Log( $"{gameObject.name} check {origin}->{goal}. Speed={serverState.Speed}" );
		Vector3Int intOrigin = Vector3Int.RoundToInt( origin );
		Vector3Int intGoal = Vector3Int.RoundToInt( goal );
		var info = serverState.ActiveThrow;
		List<HealthBehaviour> hitDamageables;
		if ( CanDriftTo( intOrigin, intGoal ) & !HittingSomething( intGoal, info.ThrownBy, out hitDamageables ) ) {
			return true;
		} else {
			//Can't drift to goal for some reason:
			//Check Tile damage from throw
			var hit2D = Physics2D.RaycastAll(origin, info.Trajectory.normalized, 1.5f, tileDmgMask);

			for(int i = 0; i < hit2D.Length; i++){
				//Debug.Log("THROW HIT: " + hit2D[i].collider.gameObject.name);

				//TilemapDamage automatically detects if a layer is below another damageable layer and won't affect it
				var tileDmg = hit2D[i].collider.gameObject.GetComponent<TilemapDamage>();
				if(tileDmg != null){
					var damage = ( int ) ( ItemAttributes.throwDamage * 2 );
					tileDmg.DoThrowDamage(intGoal, info, damage);
				}
			}
		}

		//Hurting what we can
		if ( hitDamageables != null && hitDamageables.Count > 0 && !Equals( info, ThrowInfo.NoThrow )) {
			for ( var i = 0; i < hitDamageables.Count; i++ ) {
				//Remove cast to int when moving health values to float
				var damage = ( int ) ( ItemAttributes.throwDamage * 2 );
				hitDamageables[i].ApplyDamage( info.ThrownBy, damage, DamageType.BRUTE, info.Aim );
				PostToChatMessage.SendThrowHitMessage( gameObject, hitDamageables[i].gameObject, damage, info.Aim );
			}
			//hit sound
			PlaySoundMessage.SendToAll("GenericHit", transform.position, 1f);
		}

		return false;
	}

	/// Stopping drift, killing impulse
	[Server]
	private void StopFloating() {
//		Logger.Log( $"{gameObject.name} stopped floating" );
		if ( IsTileSnap ) {
			serverState.Position = Vector3Int.RoundToInt( serverState.Position );
		} else {
			serverState.Speed = 0;
		}
		serverState.Impulse = Vector2.zero;
		serverState.Rotation = transform.rotation.eulerAngles.z;
		serverState.SpinFactor = 0;
		serverState.ActiveThrow = ThrowInfo.NoThrow;
		NotifyPlayers();
		RegisterObjects();
	}


	///Special rounding for collision detection
	///returns V3Int of next tile
	private static Vector3Int CeilWithContext( Vector3 roundable, Vector2 impulseContext ) {
		float x = impulseContext.x;
		float y = impulseContext.y;
		return new Vector3Int(
			x < 0 ? ( int ) Math.Floor( roundable.x ) : ( int ) Math.Ceiling( roundable.x ),
			y < 0 ? ( int ) Math.Floor( roundable.y ) : ( int ) Math.Ceiling( roundable.y ),
			0 );
	}

	/// <Summary>
	/// Can it drift to given pos?
	/// Use World positions
	/// </Summary>
	private bool CanDriftTo( Vector3Int targetPos ) {
		return CanDriftTo( Vector3Int.RoundToInt( serverState.WorldPosition ), targetPos );
	}

	/// <Summary>
	/// Use World positions
	/// </Summary>
	private bool CanDriftTo( Vector3Int originPos, Vector3Int targetPos ) {
		return MatrixManager.IsPassableAt( originPos, targetPos );
	}

	/// Lists objects to be damaged on given tile. Prob should be moved elsewhere
	private bool HittingSomething( Vector3Int atPos, GameObject thrownBy, out List<HealthBehaviour> victims ) {
		//Not damaging anything at launch tile
		if ( Vector3Int.RoundToInt( serverState.ActiveThrow.OriginPos ) == atPos ) {
			victims = null;
			return false;
		}
		var objectsOnTile = MatrixManager.GetAt<HealthBehaviour>( atPos );
		if ( objectsOnTile != null ) {
			var damageables = new List<HealthBehaviour>();
			foreach ( HealthBehaviour obj in objectsOnTile ) {
				//Skip thrower for now
				if ( obj.gameObject == thrownBy ) {
					Logger.Log( $"{thrownBy.name} not hurting himself", Category.Throwing );
					continue;
				}
				//Skip dead bodies
				if ( !obj.IsDead ) {
					damageables.Add( obj );
				}
			}
			if ( damageables.Count > 0 ) {
				victims = damageables;
				return true;
			}
		}

		victims = null;
		return false;
	}
}