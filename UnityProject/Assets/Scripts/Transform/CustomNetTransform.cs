using System;
using System.Collections.Generic;
using PlayGroup;
using Tilemaps;
using Tilemaps.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

// ReSharper disable CompareOfFloatsByEqualityOperator
/// Current state of transform, server modifies these and sends to clients.
/// Clients can modify these as well for prediction
public struct TransformState {
	public bool Active => Position != HiddenPos;
	///Don't set directly, use Speed instead.
	///public in order to be serialized :\
	public float speed; 
	public float Speed {
		get { return speed; }
		set { speed = value < 0 ? 0 : value; }
	}
	
	///Direction of throw
	public Vector2 Impulse;

	/// Server-only active throw information
	[NonSerialized]
	public ThrowInfo ActiveThrow;

	public int MatrixId;

	/// Local position on current matrix
	public Vector3 Position;
	/// World position, more expensive to use
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
				Position = HiddenPos;
			}
			else
			{
				MatrixInfo matrix = MatrixManager.Get( MatrixId );
				Position = MatrixManager.WorldToLocal( value, matrix );
			}
		}
	}
	public float Rotation;
	public bool IsLocalRotation;
	/// Spin direction and speed, if it should spin
	public sbyte SpinFactor; 
	
	/// Means that this object is hidden
	public static readonly Vector3Int HiddenPos = new Vector3Int(0, 0, -100);
	/// Means that this object is hidden
	public static readonly TransformState HiddenState = 
		new TransformState{ Position = HiddenPos, ActiveThrow = ThrowInfo.NoThrow, MatrixId = 0};

	public override string ToString()
	{
		return Equals( HiddenState ) ? "[Hidden]" : $"[{nameof( Position )}: {(Vector2)Position}, {nameof( WorldPosition )}: {(Vector2)WorldPosition}, " +
		       $"{nameof( Speed )}: {Speed}, {nameof( Impulse )}: {Impulse}, {nameof( Rotation )}: {Rotation}, {nameof( IsLocalRotation )}: {IsLocalRotation}, " +
		       $"{nameof( SpinFactor )}: {SpinFactor}, {nameof( MatrixId )}: {MatrixId}]";
	}
}

public partial class CustomNetTransform : ManagedNetworkBehaviour //see UpdateManager
{
	private RegisterTile registerTile;
	private ItemAttributes ItemAttributes {
		get {
			if ( itemAttributes == null ) {
				itemAttributes = GetComponent<ItemAttributes>();
			}
			return itemAttributes;
		}
	}
	private ItemAttributes itemAttributes;

	private TransformState serverState = TransformState.HiddenState; //used for syncing with players, matters only for server

	private TransformState clientState = TransformState.HiddenState; //client's transform, can get dirty/predictive

	private Matrix matrix => registerTile.Matrix;
	
	public TransformState ServerState => serverState;
	public TransformState ClientState => clientState;

	private void Start()
	{
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
	{ 
//		isPushing = false;
//		predictivePushing = false;
		if ( IsHiddenOnInit ) {
			return;
		}

		//If object is supposed to be hidden, keep it that way
//		var worldPos = serverState.WorldPosition;//
		serverState.Speed = 0;
		serverState.Rotation = transform.rotation.eulerAngles.z;
		serverState.SpinFactor = 0;
		registerTile = GetComponent<RegisterTile>();
		
		//Matrix id init
		if ( registerTile && registerTile.Matrix ) {
			//pre-placed
			serverState.MatrixId = MatrixManager.Get( matrix ).Id;
			serverState.Position =
				Vector3Int.RoundToInt(new Vector3(transform.localPosition.x, transform.localPosition.y, 0));
		} else {
			//runtime-placed
			bool initError = !MatrixManager.Instance || !registerTile;
			if ( initError ) {
				serverState.MatrixId = 0;
				Debug.LogWarning( $"{gameObject.name}: unable to detect MatrixId!" );
			} else {
				serverState.MatrixId = MatrixManager.AtPoint( Vector3Int.RoundToInt(transform.position) ).Id;
			}
			serverState.WorldPosition = Vector3Int.RoundToInt((Vector2)transform.position);
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

	/// Essentially the Update loop
	private void Synchronize()
	{
		if (!clientState.Active)
		{
			return;
		}

		if ( isServer && !serverState.Active ) {
			return;
		}

		if (isServer)
		{
			CheckFloatingServer();
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
			CheckFloatingClient();
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
			CheckMatrixSwitch();
			RegisterObjects();
		}
		//Registering
		if (!isServer && registerTile.Position != Vector3Int.RoundToInt(clientState.Position) )
			//&& !isPushing && !predictivePushing)
		{
//			Debug.LogFormat($"registerTile updating {registerTile.WorldPosition}->{Vector3Int.RoundToInt(clientState.WorldPosition)} ");
			RegisterObjects();
		}
	}

	/// Manually set an item to a specific position. Use WorldPosition!
	[Server]
	public void SetPosition(Vector3 worldPos, bool notify = true, bool keepRotation = false/*, float speed = 4f, bool _isPushing = false*/) {
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
		Vector2 pos = worldPos; //Cut z-axis
		serverState.MatrixId = MatrixManager.AtPoint( Vector3Int.RoundToInt( worldPos ) ).Id;
//		serverState.Speed = speed;
		serverState.WorldPosition = pos;
		if ( !keepRotation ) {
			serverState.Rotation = 0;
		}
		if (notify) {
			NotifyPlayers();
		}
		//Set it to being pushed if it is a push net action
//		if(_isPushing){
			//This is synced via syncvar with all players
//			isPushing = true;
//		}
	}

	[Server]
	private void SyncMatrix() {
		if ( registerTile ) {
			registerTile.ParentNetId = MatrixManager.Get( serverState.MatrixId ).NetId;
		}
	}

	[Server]
	private void CheckMatrixSwitch( bool notify = true ) {
		var pos = Vector3Int.RoundToInt( serverState.WorldPosition );
//		Debug.Log( $"{gameObject.name} doing matrix switch check for {pos}" );
		int newMatrixId = MatrixManager.AtPoint( pos ).Id;
		if ( serverState.MatrixId != newMatrixId ) {
//			Debug.Log( $"{gameObject} matrix {serverState.MatrixId}->{newMatrixId}" );

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

	///     Convenience method to make stuff disappear at position.
	///     For CLIENT prediction purposes.
	public void DisappearFromWorld()
	{
		clientState = TransformState.HiddenState;
		UpdateActiveStatus();
	}

	///     Convenience method to make stuff appear at position
	///     For CLIENT prediction purposes.
	public void AppearAtPosition(Vector3 worldPos)
	{
		var pos = (Vector2) worldPos; //Cut z-axis
		clientState.MatrixId = MatrixManager.AtPoint( Vector3Int.RoundToInt( worldPos ) ).Id;
		clientState.WorldPosition = pos;
		transform.position = pos;
		UpdateActiveStatus();
	}

	/// Called from TransformStateMessage, applies state received from server to client
	public void UpdateClientState(TransformState newState)
	{
		//Don't lerp (instantly change pos) if active state was changed
		if (clientState.Active != newState.Active /*|| newState.Speed == 0*/)
		{
			transform.position = newState.WorldPosition;
		}
		clientState = newState;
		UpdateActiveStatus();
		//sync rotation if not spinning
		if ( clientState.SpinFactor != 0 ) {
			return;
		}

		var rotation = Quaternion.Euler( 0, 0, clientState.Rotation );
		if ( clientState.IsLocalRotation ) {
			transform.localRotation = rotation;
		} else {
			transform.rotation = rotation;
		}
	}

	/// Registers if unhidden, unregisters if hidden
	private void UpdateActiveStatus()
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
	public void NotifyPlayers()
	{
//		Debug.Log( $"{gameObject.name} Notified" );
		SyncMatrix();
		serverState.IsLocalRotation = false;
		TransformStateMessage.SendToAll(gameObject, serverState);
	}

	///     Sync with new player joining
	/// <param name="playerGameObject">Whom to notify</param>
	/// <param name="isLocalRotation">(for init) tells client to assign transform.localRotation instead of transform.rotation if true</param>
	[Server]
	public void NotifyPlayer(GameObject playerGameObject, bool isLocalRotation = false) {
		serverState.IsLocalRotation = isLocalRotation;
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

	/// Register item pos in matrix
	private void RegisterObjects() {
		registerTile.UpdatePosition();
	}

//	//For space drift, the server will confirm an update is required and inform the clients
//	[ClientRpc]
//	private void RpcForceRegisterUpdate(){
//		registerTile.UpdatePosition();
//	}
}