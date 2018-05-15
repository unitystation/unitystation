using System;
using System.Collections.Generic;
using PlayGroup;
using Tilemaps;
using Tilemaps.Behaviours.Objects;
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

// ReSharper disable CompareOfFloatsByEqualityOperator
public struct TransformState {
	public bool Active => Position != HiddenPos;
	public float speed; //public in order to serialize :\
	public float Speed {
		get { return speed; }
		set {
			if ( value < 0 ) {
				speed = 0;
			} else {
				speed = value;
			}
		}
	}
	
	///Direction of throw
	public Vector2 Impulse;

	/// Server-only active throw information
	[NonSerialized]
	public ThrowInfo ActiveThrow;

	public int MatrixId;
	public Vector3 Position;

	public float Rotation;
	public sbyte SpinFactor; 
	
	/// Means that this object is hidden
	public static readonly Vector3Int HiddenPos = new Vector3Int(0, 0, -100);
	/// Means that this object is hidden
	public static readonly TransformState HiddenState = 
		new TransformState{ Position = HiddenPos, ActiveThrow = ThrowInfo.NoThrow, MatrixId = 0};

	public Vector3 WorldPosition
	{
		get
		{
			if ( !Active )
			{
				return HiddenPos;
			}
			MatrixInfo matrix = MatrixManager.Get( MatrixId );
			return MatrixManager.LocalToWorld( Position, matrix );
		}
		set
		{
			if (value == HiddenPos) {
				this = HiddenState;
			}
			else
			{
				MatrixInfo matrix = MatrixManager.Get( MatrixId );
				Position = MatrixManager.WorldToLocal( value, matrix );
			}
		}
	}

	public override string ToString()
	{
		return Equals( HiddenState ) ? "[Hidden]" : $"[{nameof( Position )}: {(Vector2)Position}, {nameof( WorldPosition )}: {(Vector2)WorldPosition}, " +
		       $"{nameof( Speed )}: {Speed}, {nameof( Impulse )}: {Impulse}, {nameof( Rotation )}: {Rotation}, {nameof( SpinFactor )}: {SpinFactor}, " +
		                                            $" {nameof( MatrixId )}: {MatrixId}]";
	}
}

public class CustomNetTransform : ManagedNetworkBehaviour //see UpdateManager
{
	private RegisterTile registerTile;
	private ItemAttributes itemAttributes;

	private TransformState serverState = TransformState.HiddenState; //used for syncing with players, matters only for server

	private TransformState clientState = TransformState.HiddenState; //client's transform, can get dirty/predictive

	private Matrix matrix => registerTile.Matrix;
	
	public TransformState ServerState => serverState;
	public TransformState ClientState => clientState;

//	[SyncVar]
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

	private void Start()
	{
		clientState = TransformState.HiddenState;
		registerTile = GetComponent<RegisterTile>();
		itemAttributes = GetComponent<ItemAttributes>();
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		InitServerState();
	}

	[Server]
	private void InitServerState()
	{ //todo: fix runtime spawn matrixid init nagging
//		isPushing = false;
//		predictivePushing = false;

		serverState.Speed = 0;
		//If object is supposed to be hidden, keep it that way
		if ( IsHiddenOnInit ) {
			return;
		}

		serverState.Position =
			Vector3Int.RoundToInt(new Vector3(transform.localPosition.x, transform.localPosition.y, 0));
		serverState.Rotation = 0f;
		serverState.SpinFactor = 0;
		registerTile = GetComponent<RegisterTile>();
		if ( registerTile && registerTile.Matrix && MatrixManager.Instance ) {
			serverState.MatrixId = MatrixManager.Get( matrix ).Id;
		} else {
			Debug.LogWarning( "Matrix id init failure" );
		}
	}

	/// Is it supposed to be hidden? (For init purposes)
	private bool IsHiddenOnInit {
		get {
			if ( Vector3Int.RoundToInt( transform.position ).Equals( TransformState.HiddenPos ) ||
			     Vector3Int.RoundToInt( transform.localPosition ).Equals( TransformState.HiddenPos ) ) {
				return true;
			}
			VisibleBehaviour visibleBehaviour = GetComponent<VisibleBehaviour>();
			return visibleBehaviour && !visibleBehaviour.visibleState;
		}
	}

	/// Intended for runtime spawning, so that CNT could initialize accordingly
	[Server]
	public void ReInitServerState()
	{
		InitServerState();
	//	Debug.Log($"{name} reInit: {serverTransformState}");
	}

	private void Synchronize()
	{
		if (!clientState.Active)
		{
			return;
		}

		if (isServer)
		{
			CheckFloating();
//			//Sync the pushing state to all players
//			//this makes sure that players with high pings cannot get too
//			//far with prediction
//			if(isPushing){
//				if(clientState.Position == transform.localPosition){
//					isPushing = false;
//					predictivePushing = false;
//				}
//			}
		} 

		if (IsFloatingClient)
		{
			SimulateFloating();
			//fixme: don't simulate moving through solid stuff on client
		}

		if (clientState.Position != transform.localPosition)
		{
			Lerp();
		}

		if ( clientState.SpinFactor != 0 ) {
			transform.Rotate( Vector3.forward, Time.deltaTime * clientState.Speed * clientState.SpinFactor );
		}

		//Checking if we should change matrix once per tile
		if (isServer && registerTile.Position != Vector3Int.RoundToInt(serverState.Position) ) {
			//Todo: optimize usage, checking every tile is kind of expensive, and all things flying into abyss are going to do that
			CheckMatrixSwitch();
		}
		//Registering
		if (registerTile.Position != Vector3Int.RoundToInt(clientState.Position) )//&& !isPushing && !predictivePushing)
		{
//			Debug.LogFormat($"registerTile updating {registerTile.WorldPosition}->{Vector3Int.RoundToInt(clientState.WorldPosition)} ");
			RegisterObjects();
		}
	}

	/// Manually set an item to a specific position. Use WorldPosition!
	[Server]
	public void SetPosition(Vector3 worldPos, bool notify = true/*, float speed = 4f, bool _isPushing = false*/) {
//		Only allow one movement at a time if it is currently being pushed
//		if(isPushing || predictivePushing){
//			if(predictivePushing && _isPushing){
//				This is if the server player is pushing because the predictive flag
				//will be set early we still need to notify the players so call it here:
//				UpdateServerTransformState(pos, notify, speed);
				//And then set the isPushing flag:
//				isPushing = true;
//			}
//			return;
//		}
		var pos = (Vector2) worldPos; //Cut z-axis
		serverState.MatrixId = MatrixManager.AtPoint( Vector3Int.RoundToInt( worldPos ) ).Id;
//		serverState.Speed = speed;
		serverState.WorldPosition = pos;
		if (notify) {
			NotifyPlayers();
		}
		//Set it to being pushed if it is a push net action
//		if(_isPushing){
			//This is synced via syncvar with all players
//			isPushing = true;
//		}
	}

	/// Apply impulse while setting position
	[Server]
	public void PushTo(Vector3 pos, Vector2 impulseDir, bool notify = true, float speed = 4f, bool _isPushing = false)
	{
//		if (IsInSpace()) {
//			serverTransformState.Impulse = impulseDir;
//		} else {
//			SetPosition(pos, notify, speed, _isPushing);
//		}
	}

	[Server]
	private void SyncMatrix() {
		registerTile.ParentNetId = MatrixManager.Get( serverState.MatrixId ).NetId;
	}

	[Server]
	private void CheckMatrixSwitch( bool notify = true ) {
		int newMatrixId = MatrixManager.AtPoint( Vector3Int.RoundToInt( serverState.WorldPosition ) ).Id;
		if ( serverState.MatrixId != newMatrixId ) {
			Debug.Log( $"{gameObject} matrix {serverState.MatrixId}->{newMatrixId}" );

			//It's very important to save World Pos before matrix switch and restore it back afterwards
			var worldPosToPreserve = serverState.WorldPosition;
			serverState.MatrixId = newMatrixId;
			serverState.WorldPosition = worldPosToPreserve;
			if ( notify ) {
				NotifyPlayers();
			}
		}
	}

	[Server]
	public void InertiaDrop( Vector3 initialPos, float speed, Vector2 impulse ) {
		SetPosition( initialPos, false );
		serverState.Impulse = impulse;
		serverState.Speed = Random.Range(-0.3f, 0f) + speed;
		NotifyPlayers();
	}

	[Server]
	public void Throw( ThrowInfo info ) {
		SetPosition( info.OriginPos, false );
		
		var throwSpeed = itemAttributes.throwSpeed * 10; //tiles per second
		var throwRange = itemAttributes.throwRange;
		
		//Calculate impulse
		Vector2 impulse = (info.TargetPos - info.OriginPos).normalized;
		
		var correctedInfo = info;
		//limit throw range here
		if ( Vector2.Distance( info.OriginPos, info.TargetPos ) > throwRange ) {
			correctedInfo.TargetPos = info.OriginPos + ( (Vector3)impulse * throwRange );
//			Debug.Log( $"Throw distance clamped to {correctedInfo.Trajectory.magnitude}, " +
//			           $"target changed {info.TargetPos}->{correctedInfo.TargetPos}" );
		}
		
		//add player momentum
		float playerMomentum = 0f;
		//If throwing nearby, do so at 1/2 speed (looks clunky otherwise)
		float speedMultiplier = Mathf.Clamp(correctedInfo.Trajectory.magnitude / throwRange, 0.3f, 1f);
		serverState.Speed = (Random.Range(-0.2f, 0.2f) + throwSpeed + playerMomentum) * speedMultiplier;
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
	public void ForceDrop(Vector3 pos)
	{
//		GetComponentInChildren<SpriteRenderer>().color = Color.white;
		SetPosition( pos, false );
		Vector2 impulse = Random.insideUnitCircle.normalized;
		//don't apply impulses if item isn't going to float in that direction
		Vector3Int newGoal = RoundWithContext(serverState.WorldPosition + (Vector3) impulse, impulse);
		if (CanDriftTo(newGoal))
		{
			serverState.Impulse = impulse;
			serverState.Speed = Random.Range(0.2f, 2f);
		}
		NotifyPlayers();
	}

	[Server]
	public void DisappearFromWorldServer()
	{
		serverState = TransformState.HiddenState;
		NotifyPlayers();
	}

	[Server]
	public void AppearAtPositionServer(Vector3 worldPos)
	{
		SetPosition(worldPos);
	}

//	///     Method to substitute transform.parent = x stuff.
//	///     You shouldn't really use it anymore,
//	///     as there are high level methods that should suit your needs better.
//	///     Server-only, client is not being notified
//	[Server]
//	public void SetParent(Transform pos)
//	{
//		transform.parent = pos;
//	}

	///     Convenience method to make stuff disappear at position.
	///     For CLIENT prediction purposes.
	public void DisappearFromWorld()
	{
		clientState = TransformState.HiddenState;
		updateActiveStatus();
	}

	///     Convenience method to make stuff appear at position
	///     For CLIENT prediction purposes.
	public void AppearAtPosition(Vector3 worldPos)
	{
		var pos = (Vector2) worldPos; //Cut z-axis
		clientState.MatrixId = MatrixManager.AtPoint( Vector3Int.RoundToInt( worldPos ) ).Id;
		clientState.WorldPosition = pos;
		transform.position = pos;
		updateActiveStatus();
	}

	/// Client side prediction for pushing
	/// This allows instant pushing reaction to a pushing event
	/// on the client who instigated it. The server then validates
	/// the transform position and returns it if it is illegal
	public void PushToPosition(Vector3 pos, float speed, PushPull pushComponent)
	{
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

	public void UpdateClientState(TransformState newState)
	{
		//FIXME: invesigate wrong clientState for microwaved food
		//todo: address rotation pickup issues + items should be upright when dropping
		//Don't lerp (instantly change pos) if active state was changed
		if (clientState.Active != newState.Active /*|| newState.Speed == 0*/)
		{
			transform.localPosition = newState.Position;
		}
		clientState = newState;
		updateActiveStatus();
//		transform.rotation = Quaternion.Euler( 0, 0, clientState.Rotation ); //?
	}

	private void updateActiveStatus()
	{
		if (clientState.Active)
		{
			RegisterObjects();
		}
		else
		{
			registerTile.Unregister();
		}
		//Consider moving VisibleBehaviour functionality to CNT. Currently VB doesn't allow predictive object hiding, for example. 
		Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].enabled = clientState.Active;
		}
	}

	///     Currently sending to everybody, but should be sent to nearby players only
	[Server]
	private void NotifyPlayers()
	{
		SyncMatrix();
		TransformStateMessage.SendToAll(gameObject, serverState);
	}

	///     Sync with new player joining
	/// <param name="playerGameObject"></param>
	[Server]
	public void NotifyPlayer(GameObject playerGameObject)
	{
		TransformStateMessage.Send(playerGameObject, gameObject, serverState);
	}

	//managed by UpdateManager
	public override void UpdateMe()
	{
		if (!registerTile)
		{
			registerTile = GetComponent<RegisterTile>();
		}
		Synchronize();
	}

	private void RegisterObjects() {
		//Register item pos in matrix
		registerTile.UpdatePosition();
	}

	///predictive perpetual flying
	private void SimulateFloating()
	{
		clientState.Position +=
			(Vector3) clientState.Impulse * clientState.Speed * Time.deltaTime;
	}

	private void Lerp()
	{
		Vector3 targetPos = MatrixManager.WorldToLocal(clientState.WorldPosition, MatrixManager.Get( matrix ));
		if ( clientState.Speed.Equals(0) )
		{
			transform.localPosition = targetPos;
			return;
		}
		transform.localPosition =
			Vector3.MoveTowards(transform.localPosition, targetPos, clientState.Speed * Time.deltaTime);
	}

	///     Space drift detection is serverside only
	[Server]
	private void CheckFloating()
	{ 
		if (IsFloatingServer && matrix != null)
		{
			Vector3 newGoal = serverState.WorldPosition +
			                                      (Vector3) serverState.Impulse * serverState.Speed * Time.deltaTime;
			Vector3Int intOrigin = Vector3Int.RoundToInt( serverState.WorldPosition );
			Vector3Int intGoal = Vector3Int.RoundToInt( newGoal );
			//RoundWithContext(newGoal, serverState.Impulse);
			
			//Natural throw ending
			if ( IsBeingThrown && ShouldStopThrow ) {
				serverState.ActiveThrow = ThrowInfo.NoThrow;
				//Change spin when we hit the ground. Zero was kinda dull
				serverState.SpinFactor = (sbyte) ( -serverState.SpinFactor * 0.2f );
				//todo: ground hit sound
			}
			
			if ( intOrigin == intGoal ) {
				//same tile, don't check
				serverState.WorldPosition = newGoal;
				Debug.Log( $"Same tile throw {newGoal}, not doing checks(?)" );
				return;
			}

			int distance = (int) Vector3Int.Distance( intOrigin, intGoal );
			//for every tile between origin and target
			for ( int i = 0; i < distance; i++ ) {
				Vector3 tempOrigin = serverState.WorldPosition;
				Vector3 tempGoal = 	 tempOrigin + (Vector3) serverState.Impulse * (i + 1);
				if ( CheckFloating( tempOrigin, tempGoal ) ) {
					serverState.WorldPosition = tempGoal;
					//Spess drifting is perpetual, but speed decreases each tile if object is flying on non-empty (floor assumed) tiles
					if ( !IsBeingThrown && !MatrixManager.IsEmptyAt( Vector3Int.RoundToInt( tempOrigin ) ) ) {
						//on-ground resistance
						//serverState.Speed -= 0.5f;
						serverState.Speed = serverState.Speed - ( serverState.Speed * 0.10f ) - 0.5f;
						if ( serverState.Speed <= 0.05f ) {
							StopFloating();
						} else {
							NotifyPlayers(); //?
						}
					}
				} else {
					StopFloating();
				}

			}
		}
	}

	private bool CheckFloating( Vector3 origin, Vector3 goal ) {
		Debug.Log( $"Check {origin}->{goal}" );
		Vector3Int intOrigin = Vector3Int.RoundToInt( origin );
		Vector3Int intGoal = Vector3Int.RoundToInt( goal );
		List<HealthBehaviour> hitDamageables;
		if ( CanDriftTo( intOrigin, intGoal ) & !HittingSomething( intOrigin, out hitDamageables ) ) {
			return true;
		} else {
			var info = serverState.ActiveThrow;
			//Hurting what we can
			if ( hitDamageables != null && hitDamageables.Count > 0 && !Equals( info, ThrowInfo.NoThrow ) ) {
				for ( var i = 0; i < hitDamageables.Count; i++ ) {
					//Remove cast to int when moving health values to float
					hitDamageables[i].ApplyDamage( info.ThrownBy, (int) ( itemAttributes.throwForce * 2 ), DamageType.BRUTE, info.Aim );
				}

				//todo:hit sound
			}
			return false;
//				RpcForceRegisterUpdate();
		}
	}

	///Stopping drift, killing impulse
	[Server]
	private void StopFloating() {
//		Debug.Log( $"{gameObject.name} stopped floating" );
		serverState.Impulse = Vector2.zero;
		serverState.Speed = 0;
		serverState.Rotation = transform.rotation.eulerAngles.z;
		serverState.SpinFactor = 0;
		serverState.ActiveThrow = ThrowInfo.NoThrow; 
		NotifyPlayers();
		RegisterObjects();
	}

//	//For space drift, the server will confirm an update is required and inform the clients
//	[ClientRpc]
//	private void RpcForceRegisterUpdate(){
//		registerTile.UpdatePosition();
//	}

	///Special rounding for collision detection
	///returns V3Int of next tile
	public static Vector3Int RoundWithContext(Vector3 roundable, Vector2 impulseContext)
	{
		float x = impulseContext.x;
		float y = impulseContext.y;
		return new Vector3Int(
			x < 0 ? (int) Math.Floor(roundable.x) : (int) Math.Ceiling(roundable.x),
			y < 0 ? (int) Math.Floor(roundable.y) : (int) Math.Ceiling(roundable.y),
			0);
	}

	/// Can it drift to given pos?
	private bool CanDriftTo( Vector3Int targetPos ) {
		return CanDriftTo( Vector3Int.RoundToInt( serverState.WorldPosition ), targetPos );
	}

	private bool CanDriftTo(Vector3Int originPos, Vector3Int targetPos)
	{
		return MatrixManager.IsPassableAt( originPos, targetPos );
	}

	private bool HittingSomething( Vector3Int atPos, out List<HealthBehaviour> victims ) {
		//Not damaging anything at launch tile
		if ( Vector3Int.RoundToInt(serverState.ActiveThrow.OriginPos) == atPos ) {
			victims = null;
			return false;
		}
		//todo: cross-matrix check, perhaps?
		var objectsOnTile = //matrix.Get<HealthBehaviour>( Vector3Int.RoundToInt(serverState.Position) );
		matrix.Get<HealthBehaviour>(MatrixManager.Instance.WorldToLocalInt( atPos, matrix ));
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