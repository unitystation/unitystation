using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

/// <summary>
/// Component which allows an object to have an orientation (facing) which is synced over the network, supports client
/// prediction, responds to matrix rotation events, and allows setting an initial direction
/// in editor (useful for mapping).
///
/// It's up to other components to decide how to update their display when direction changes,
/// by subscribing to the OnDirectionChange UnityEvent. Client prediction / server sync is entirely handled
/// within this component, so components which react to direction changes don't need to think about it - just subscribe to the event.
///
/// Note that sprite rotation is handled in the SpriteRotation component
///
/// This component should be used for all components which have some sort of directional behavior.
/// </summary>
[RequireComponent(typeof(RegisterTile))]
public class Directional : NetworkBehaviour
{

	[Tooltip("Direction of this object in the scene, used as the initial direction when the map loads.")]
	public OrientationEnum InitialDirection = OrientationEnum.Down;

	/// <summary>
	/// Initial direction as an Orientation rather than enum.
	/// </summary>
	public Orientation InitialOrientation => Orientation.FromEnum(InitialDirection);

	[SyncVar(hook = nameof(ServerDirectionChangeHook))]
	private Orientation serverDirection;
	private Orientation clientDirection;

	//tracks the current offset due to matrix rotations - entirely client side so that
	//rotations are displayed at the correct moment for the client (at the end of matrix rotation) and
	//don't occur too late / early due to lag from server telling us the new direction.
	//Any time our direction is set to a specific value, this is reset to RotationOffset.Same to maintain
	//sync with the server.
	private RotationOffset clientMatrixRotationOffset = RotationOffset.Same;

	/// <summary>
	/// If true, direction will be changed at the end of
	/// matrix rotation to match the matrix rotation that occurred. If false,
	/// direction will not be changed regardless of matrix rotation.
	/// </summary>
	[Tooltip("If true, direction will be changed at the end of " +
	         "matrix rotation to match the matrix rotation that occurred. If false," +
	         " direction will not be changed regardless of matrix rotation.")]
	public bool ChangeDirectionWithMatrix = true;

	/// <summary>
	/// Whether this component is on the local player object, which has special handling because the local
	/// player controls their own object.
	/// </summary>
	private bool IsLocalPlayer => PlayerManager.LocalPlayer == gameObject;

	/// <summary>
	/// Turn this on when doing client prediction - this Directional will completely ignore server
	/// direction updates and only perform direction changes when they are made locally. When this
	/// is turned back off, direction will be synced with server.
	/// </summary>
	public bool IgnoreServerUpdates
	{
		get => ignoreServerUpdates;
		set
		{
			if (!value)
			{
				SyncDirection();
			}
			ignoreServerUpdates = value;
		}
	}

	/// <summary>
	/// When true, direction changes are no longer allowed, player will be stuck in their current direction
	/// </summary>
	public bool LockDirection
	{
		get => lockDirection;
		set
		{
			lockDirection = value;
			SyncDirection();
		}
	}
	private bool lockDirection;

	private bool ignoreServerUpdates = false;

	/// <summary>
	/// Invoked when this object's sprites should be updated to indicate it is facing the
	/// specified direction. Components listening for this event don't need to worry about
	/// client prediction or server sync, just update sprites and assume this is the correct direction.
	/// </summary>
	public DirectionChangeEvent OnDirectionChange = new DirectionChangeEvent();

	/// <summary>
	/// Current direction the object should be shown facing.
	/// </summary>
	public Orientation CurrentDirection =>
		(isServer || (!IsLocalPlayer && !IgnoreServerUpdates) ? serverDirection : clientDirection)
		.Rotate(clientMatrixRotationOffset);

	// cached registertile
	private RegisterTile registerTile;
	//cached spriteRenderers of this gameobject
	protected SpriteRenderer[] spriteRenderers;

	private void Awake()
	{
		this.registerTile = GetComponent<RegisterTile>();
		//subscribe to matrix rotations (even on client side so we can predict them)
		registerTile.OnMatrixWillChange.AddListener(OnMatrixWillChange);
		OnMatrixWillChange(registerTile.Matrix);
	}

	//invoked when our parent matrix is being changed or initially set
	private void OnMatrixWillChange(Matrix newMatrix)
	{
		//add our listeners
		//unsub from old matrix
		if (registerTile.Matrix != null)
		{
			var move = registerTile.Matrix.GetComponentInParent<MatrixMove>();
			if (move != null)
			{
				move.OnRotateEnd.RemoveListener(OnMatrixRotationEnd);
			}
		}

		//sub to new matrix
		if (newMatrix != null)
		{
			var newMove = newMatrix.GetComponentInParent<MatrixMove>();
			if (newMove != null)
			{
				newMove.OnRotateEnd.AddListener(OnMatrixRotationEnd);
			}
		}
	}

	void OnDrawGizmos ()
	{
		Gizmos.color = Color.green;

		if (Application.isEditor && !Application.isPlaying)
		{
			DebugGizmoUtils.DrawArrow(transform.position, InitialOrientation.Vector);
		}
		else
		{
			DebugGizmoUtils.DrawArrow(transform.position, CurrentDirection.Vector);
		}
	}

	public override void OnStartServer()
    {
	    ServerDirectionChangeHook(InitialOrientation);
    }


    public override void OnStartClient()
    {
	    StartCoroutine(WaitForClientLoad());
    }

    private IEnumerator WaitForClientLoad()
    {
	    yield return WaitFor.EndOfFrame;
	    if (PlayerManager.LocalPlayer == gameObject)
	    {
		    //we ignore server updates (unless forced) for our local player
		    IgnoreServerUpdates = true;
	    }
	    SyncDirection();
    }



    private void OnDisable()
    {
	    if (registerTile.Matrix != null)
	    {
		    var move = registerTile.Matrix.GetComponentInParent<MatrixMove>();
		    if (move != null)
		    {
			    move.OnRotateEnd.RemoveListener(OnMatrixRotationEnd);
		    }
	    }
    }

    /// <summary>
    /// Invoked when receiving rotation event from our current matrix's matrixmove
    /// </summary>
    //invoked when matrix rotation is ending
    private void OnMatrixRotationEnd(RotationOffset fromCurrent, bool isInitialRotation)
    {
	    if (ChangeDirectionWithMatrix && !LockDirection)
	    {
		    //entirely client side - update rotation value stored on this client and
		    //display the new resulting rotation. Don't update any of our Orientation variables.
		    clientMatrixRotationOffset = clientMatrixRotationOffset.Rotate(fromCurrent);
		    OnDirectionChange.Invoke(CurrentDirection);
	    }
    }

    /// <summary>
    ///Force this object to face the direction currently set on the server for this object.
    /// </summary>
    private void SyncDirection()
    {
	    //reset rotation offset since we are getting explicit absolute direction from server
	    clientMatrixRotationOffset = RotationOffset.Same;
	    clientDirection = serverDirection;
	    OnDirectionChange.Invoke(clientDirection);
    }

    /// <summary>
    /// Cause the object to face the specified direction. On server, syncs the direction to all clients.
    /// On client, if this object IsLocalPlayer, requests the direction change from the server and
    /// locally predicts it. On a client object that is not the local player, this locally predicts the change (when
    /// the next value is received from server it will switch to that).
    ///
    /// No effect if LockDirection = true;
    /// </summary>
    /// <param name="newDir"></param>
    public void FaceDirection(Orientation newDir)
    {
	    if (LockDirection) return;

	    //reset rotation offset since we are being told to face an absolute direction
	    clientMatrixRotationOffset = RotationOffset.Same;
	    Logger.LogTraceFormat("{0} FaceDirection newDir {1} IsLocalPlayer {2} ignoreServerUpdates {3} clientDir {4} serverDir {5} ", Category.Movement,
		    gameObject.name, newDir, IsLocalPlayer, IgnoreServerUpdates, clientDirection, serverDirection);
	    if (isServer)
	    {
		    ServerDirectionChangeHook(newDir);
	    }
	    clientDirection = newDir;
	    if (IsLocalPlayer)
	    {
		    CmdChangeDirection(newDir);
	    }
	    OnDirectionChange.Invoke(clientDirection);
    }

    /// <summary>
    /// Force the clients object to face the current server direction, even if IgnoreServerUpdates = true
    /// </summary>
    public void TargetForceSyncDirection(NetworkConnection target)
    {
	    //note: doing it this way (internal TargetRpc passing serverDirection)
	    //so we are guaranteed that the client has the correct
	    //server direction when we force them to sync to it.
	    TargetForceSyncDirection(target, serverDirection);
    }

    [TargetRpc]
    private void TargetForceSyncDirection(NetworkConnection target, Orientation direction)
    {
	    ServerDirectionChangeHook(direction);
	    SyncDirection();
    }


    //client requests the server to change serverDirection
    [Command]
    private void CmdChangeDirection(Orientation direction)
    {
	    ServerDirectionChangeHook(direction);
    }

    //syncvar hook invoked when server sends a client the new direction for this object
    private void ServerDirectionChangeHook(Orientation dir)
    {
	    serverDirection = dir;
	    if (!IgnoreServerUpdates)
	    {
		    //reset rotation offset since we are being told to face an absolute direction
		    clientMatrixRotationOffset = RotationOffset.Same;
		    OnDirectionChange.Invoke(serverDirection);
	    }
    }
}

/// <summary>
/// Event which indicates a direction change has occurred.
/// </summary>
public class DirectionChangeEvent : UnityEvent<Orientation>{}
