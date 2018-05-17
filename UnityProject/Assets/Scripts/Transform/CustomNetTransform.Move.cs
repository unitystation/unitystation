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
	public bool isPushing;
	public bool predictivePushing = false;
	public bool IsInSpace => MatrixManager.IsSpaceAt( Vector3Int.RoundToInt( transform.position ) );
	public bool IsFloatingServer => serverState.Impulse != Vector2.zero && serverState.Speed > 0f;
	public bool IsFloatingClient => clientState.Impulse != Vector2.zero && clientState.Speed > 0f;
	public bool IsBeingThrown => !serverState.ActiveThrow.Equals( ThrowInfo.NoThrow );

	private bool ShouldStopThrow {
		get {
			if ( !IsBeingThrown ) {
				return true;
			}

			var trajectory = serverState.ActiveThrow.Trajectory;
			var shouldStop =
				Vector3.Distance( serverState.ActiveThrow.OriginPos, serverState.WorldPosition ) >= trajectory.magnitude;
//			if ( shouldStop ) {
//				Debug.Log( $"Should stop throw: {Vector3.Distance( serverState.ActiveThrow.OriginPos, serverState.WorldPosition )}" +
//				           $" >= {trajectory.magnitude}" );
//			}
			return shouldStop;
		}
	}

	/// Apply impulse while setting position
	[Server]
	public void PushTo( Vector3 pos, Vector2 impulseDir, bool notify = true, float speed = 4f, bool _isPushing = false ) {
//		if (IsInSpace()) {
//			serverTransformState.Impulse = impulseDir;
//		} else {
//			SetPosition(pos, notify, speed, _isPushing);
//		}
	}

	/// Client side prediction for pushing
	/// This allows instant pushing reaction to a pushing event
	/// on the client who instigated it. The server then validates
	/// the transform position and returns it if it is illegal
	public void PushToPosition( Vector3 pos, float speed, PushPull pushComponent ) {
//		if(pushComponent.pushing || predictivePushing){
//			return;
//		}
//		TransformState newState = clientState;
//		newState.Active = true;
//		newState.Speed = speed;
//		newState.Position = pos;
//		UpdateClientState(newState);
//		predictivePushing = true;
//		pushComponent.pushing = true;
	}

	///predictive perpetual flying
	private void SimulateFloating() {
		clientState.Position +=
			( Vector3 ) clientState.Impulse * clientState.Speed * Time.deltaTime;
	}

	private void Lerp() {
		Vector3 targetPos = MatrixManager.WorldToLocal( clientState.WorldPosition, MatrixManager.Get( matrix ) );
		if ( clientState.Speed.Equals( 0 ) ) {
			transform.localPosition = targetPos;
			return;
		}
		transform.localPosition =
			Vector3.MoveTowards( transform.localPosition, targetPos, clientState.Speed * Time.deltaTime );
	}

	[Server]
	public void InertiaDrop( Vector3 initialPos, float speed, Vector2 impulse ) {
		SetPosition( initialPos, false );
		serverState.Impulse = impulse;
		serverState.Speed = Random.Range( -0.3f, 0f ) + speed;
		NotifyPlayers();
	}

	[Server]
	public void Throw( ThrowInfo info ) {
		SetPosition( info.OriginPos, false );

		var throwSpeed = itemAttributes.throwSpeed * 10; //tiles per second
		var throwRange = itemAttributes.throwRange;

		//Calculate impulse
		Vector2 impulse = ( info.TargetPos - info.OriginPos ).normalized;

		var correctedInfo = info;
		//limit throw range here
		if ( Vector2.Distance( info.OriginPos, info.TargetPos ) > throwRange ) {
			correctedInfo.TargetPos = info.OriginPos + ( ( Vector3 ) impulse * throwRange );
//			Debug.Log( $"Throw distance clamped to {correctedInfo.Trajectory.magnitude}, " +
//			           $"target changed {info.TargetPos}->{correctedInfo.TargetPos}" );
		}

		//add player momentum
		float playerMomentum = 0f;
		//If throwing nearby, do so at 1/2 speed (looks clunky otherwise)
		float speedMultiplier = Mathf.Clamp( correctedInfo.Trajectory.magnitude / throwRange, 0.3f, 1f );
		serverState.Speed = ( Random.Range( -0.2f, 0.2f ) + throwSpeed + playerMomentum ) * speedMultiplier;
		correctedInfo.InitialSpeed = serverState.Speed;

		serverState.Impulse = impulse;
		if ( info.SpinMode != SpinMode.None ) {
			serverState.SpinFactor = ( sbyte ) ( Mathf.Clamp( throwSpeed * 2, sbyte.MinValue, sbyte.MaxValue )
			                                     * ( info.SpinMode == SpinMode.Clockwise ? 1 : -1 ) );
		}

		serverState.ActiveThrow = correctedInfo;
		Debug.Log( $"Throw:{correctedInfo} {serverState}" );
		//todo: add counter-impulse to player if he's in space
		NotifyPlayers();
	}

	/// Dropping with some force, in random direction. For space floating demo purposes.
	[Server]
	public void ForceDrop( Vector3 pos ) {
//		GetComponentInChildren<SpriteRenderer>().color = Color.white;
		SetPosition( pos, false );
		Vector2 impulse = Random.insideUnitCircle.normalized;
		//don't apply impulses if item isn't going to float in that direction
		Vector3Int newGoal = RoundWithContext( serverState.WorldPosition + ( Vector3 ) impulse, impulse );
		if ( CanDriftTo( newGoal ) ) {
			serverState.Impulse = impulse;
			serverState.Speed = Random.Range( 0.2f, 2f );
		}

		NotifyPlayers();
	}

	///     Space drift detection is serverside only
	[Server]
	private void CheckFloating() {
		if ( !IsFloatingServer || matrix == null ) {
			return;
		}

		Vector3 newGoal = serverState.WorldPosition +
		                  ( Vector3 ) serverState.Impulse * serverState.Speed * Time.deltaTime;
		Vector3Int intOrigin = Vector3Int.RoundToInt( serverState.WorldPosition );
		Vector3Int intGoal = //Vector3Int.RoundToInt( newGoal );
			RoundWithContext( newGoal, serverState.Impulse ); //?

		int distance = ( int ) Vector3Int.Distance( intOrigin, intGoal );
		if ( distance > 1 ) {
			//limit goal to just one tile away and run this method recursively afterwards
			newGoal = serverState.WorldPosition + ( Vector3 ) serverState.Impulse;
		}

		bool isWithinTile = intOrigin == intGoal; //same tile, no need to validate stuff
		if ( ValidateFloating( serverState.WorldPosition, newGoal ) || isWithinTile ) {
			AdvanceMovement( serverState.WorldPosition, newGoal );
		} else {
			StopFloating();
		}

		if ( distance > 1 ) {
			CheckFloating();
		}
	}

	[Server]
	private void AdvanceMovement( Vector3 tempOrigin, Vector3 tempGoal ) {
		if ( !IsFloatingServer ) {
			Debug.LogWarning( $"Not advancing {tempOrigin}->{tempGoal}" );
			return;
		}

		//Natural throw ending
		if ( IsBeingThrown && ShouldStopThrow ) {
			Debug.Log( $"{gameObject.name}: Throw ended at {serverState.WorldPosition}" );
			serverState.ActiveThrow = ThrowInfo.NoThrow;
			//Change spin when we hit the ground. Zero was kinda dull
			serverState.SpinFactor = ( sbyte ) ( -serverState.SpinFactor * 0.2f );
			//todo: ground hit sound
		}

		serverState.WorldPosition = tempGoal;
		//Spess drifting is perpetual, but speed decreases each tile if object is flying on non-empty (floor assumed) tiles
		if ( !IsBeingThrown && !MatrixManager.IsEmptyAt( Vector3Int.RoundToInt( tempOrigin ) ) ) {
			//on-ground resistance
			//serverState.Speed -= 0.5f;
			serverState.Speed = serverState.Speed - ( serverState.Speed * 0.10f ) - 0.5f;
			if ( serverState.Speed <= 0.05f ) {
				StopFloating();
			} else {
				NotifyPlayers();
			}
		}
	}

	[Server]
	private bool ValidateFloating( Vector3 origin, Vector3 goal ) {
		Debug.Log( $"Check {origin}->{goal}. Speed={serverState.Speed}" );
		Vector3Int intOrigin = Vector3Int.RoundToInt( origin );
		Vector3Int intGoal = Vector3Int.RoundToInt( goal );
		List<HealthBehaviour> hitDamageables;
		if ( CanDriftTo( intOrigin, intGoal ) & !HittingSomething( intGoal, out hitDamageables ) ) {
			return true;
		}

		var info = serverState.ActiveThrow;
		//Hurting what we can
		if ( hitDamageables != null && hitDamageables.Count > 0 && !Equals( info, ThrowInfo.NoThrow ) ) {
			for ( var i = 0; i < hitDamageables.Count; i++ ) {
				//Remove cast to int when moving health values to float
				hitDamageables[i].ApplyDamage( info.ThrownBy, ( int ) ( itemAttributes.throwForce * 2 ), DamageType.BRUTE, info.Aim );
			}

			//todo:hit sound
		}

		return false;
//				RpcForceRegisterUpdate();
	}

	///Stopping drift, killing impulse
	[Server]
	private void StopFloating() {
		Debug.Log( $"{gameObject.name} stopped floating" );
		serverState.Impulse = Vector2.zero;
		serverState.Speed = 0;
		serverState.Rotation = transform.rotation.eulerAngles.z;
		serverState.SpinFactor = 0;
		serverState.ActiveThrow = ThrowInfo.NoThrow;
		NotifyPlayers();
		RegisterObjects();
	}

	///Special rounding for collision detection
	///returns V3Int of next tile
	public static Vector3Int RoundWithContext( Vector3 roundable, Vector2 impulseContext ) {
		float x = impulseContext.x;
		float y = impulseContext.y;
		return new Vector3Int(
			x < 0 ? ( int ) Math.Floor( roundable.x ) : ( int ) Math.Ceiling( roundable.x ),
			y < 0 ? ( int ) Math.Floor( roundable.y ) : ( int ) Math.Ceiling( roundable.y ),
			0 );
	}

	/// Can it drift to given pos?
	private bool CanDriftTo( Vector3Int targetPos ) {
		return CanDriftTo( Vector3Int.RoundToInt( serverState.WorldPosition ), targetPos );
	}

	private bool CanDriftTo( Vector3Int originPos, Vector3Int targetPos ) {
		return MatrixManager.IsPassableAt( originPos, targetPos );
	}

	private bool HittingSomething( Vector3Int atPos, out List<HealthBehaviour> victims ) {
		//Not damaging anything at launch tile
		if ( Vector3Int.RoundToInt( serverState.ActiveThrow.OriginPos ) == atPos ) {
			victims = null;
			return false;
		}
		var objectsOnTile = MatrixManager.GetAt<HealthBehaviour>( atPos );
		if ( objectsOnTile != null ) {
			var damageables = new List<HealthBehaviour>();
			foreach ( HealthBehaviour obj in objectsOnTile ) {
				//We don't want to hit dead bodies
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