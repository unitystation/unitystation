using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

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
[RequireComponent(typeof(RegisterTile))][ExecuteInEditMode]
public class Directional : NetworkBehaviour, IMatrixRotation
{
	[Tooltip("Direction of this object in the scene, used as the initial direction when the map loads.")]
	public OrientationEnum InitialDirection = OrientationEnum.Down;

	private OrientationEnum editorInitialDirection;
	public UnityEvent onEditorDirectionChange;

	/// <summary>
	/// Initial direction as an Orientation rather than enum.
	/// </summary>
	public Orientation InitialOrientation => Orientation.FromEnum(InitialDirection);

	[SyncVar(hook = nameof(SyncServerDirection))]
	private Orientation serverDirection;

	private Orientation clientDirection;

	/// <summary>
	/// If true, direction will be changed at the end of
	/// matrix rotation to match the matrix rotation that occurred. If false,
	/// direction will not be changed regardless of matrix rotation.
	/// </summary>
	[Tooltip("If true, direction will be changed at the end of " +
	         "matrix rotation to match the matrix rotation that occurred. If false," +
	         " direction will not be changed regardless of matrix rotation.")]
	public bool ChangeDirectionWithMatrix = true;

	[Tooltip("If true this component will ignore all SyncVar updates. Useful if you just want to use" +
	         "this component for easy direction changing at edit time")]
	public bool DisableSyncing = false;

	/// <summary>
	/// Whether this component is on the local player object, which has special handling because the local
	/// player controls their own object.
	/// </summary>
	private bool IsLocalPlayer => PlayerManager.LocalPlayer == gameObject;

	/// <summary>
	/// NOTE: Has no effect on local player - local player is always predictive.
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
				ForceClientDirectionFromServer();
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
			ForceClientDirectionFromServer();
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
	public Orientation CurrentDirection {

		get
		{
			if (Application.isPlaying)
			{
				return (isServer || (!IsLocalPlayer && !IgnoreServerUpdates) ? serverDirection : clientDirection);
			}
			else
			{
				return InitialOrientation;
			}
		}
	}

void OnDrawGizmosSelected()
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

	#if UNITY_EDITOR
	void Update()
	{
		if (!Application.isPlaying)
		{
			if (editorInitialDirection != InitialDirection)
			{
				editorInitialDirection = InitialDirection;
				if(onEditorDirectionChange != null) onEditorDirectionChange.Invoke();
			}
		}
	}
	#endif

	public override void OnStartServer()
	{
		var registerTile = GetComponent<RegisterTile>();
		registerTile.WaitForMatrixInit(WaitForMatrixLoad);
	}

	private void WaitForMatrixLoad(MatrixInfo matrixInfo)
	{
		serverDirection = new Orientation(InitialOrientation.Degrees);
	}

    public override void OnStartClient()
    {
	    if (DisableSyncing) return;
	    SyncServerDirection(serverDirection, serverDirection);
	    ForceClientDirectionFromServer();
    }

    /// <summary>
    /// Cause the object to face the specified direction. On server, syncs the direction to all clients.
    /// On client, if this object IsLocalPlayer, requests the direction change from the server and
    /// locally predicts it. On a client object that is not the local player, just locally predicts the change
    ///
    /// No effect if LockDirection = true;
    /// </summary>
    /// <param name="newDir"></param>
    public void FaceDirection(Orientation newDir)
    {
	    if (LockDirection) return;

	    Logger.LogTraceFormat("{0} FaceDirection newDir {1} IsLocalPlayer {2} ignoreServerUpdates {3} clientDir {4} serverDir {5} ", Category.Direction,
		    gameObject.name, newDir, IsLocalPlayer, IgnoreServerUpdates, clientDirection, serverDirection);
	    if (isServer)
	    {
		    serverDirection = new Orientation(newDir.Degrees);
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
	    if (target == null) return;
	    if (target.connectionId == -1) return;

	    TargetForceSyncDirection(target, serverDirection);
    }

    [TargetRpc]
    private void TargetForceSyncDirection(NetworkConnection target, Orientation direction)
    {
	    SyncServerDirection(serverDirection, direction);
	    ForceClientDirectionFromServer();
    }

    public void OnMatrixRotate(MatrixRotationInfo rotationInfo)
    {
	    if (rotationInfo.IsEnding)
	    {
		    if (ChangeDirectionWithMatrix && !LockDirection)
		    {
			    if (rotationInfo.IsClientside && !isServer && rotationInfo.MatrixMove.Initialized)
			    {
				    //predict
				    clientDirection = CurrentDirection.Rotate(rotationInfo.RotationOffset);
				    OnDirectionChange.Invoke(clientDirection);
			    }
			    else if (rotationInfo.IsServerside && isServer)
			    {
				    //change server side and sync to clients
				    serverDirection = new Orientation(CurrentDirection.Rotate(rotationInfo.RotationOffset).Degrees);
				    OnDirectionChange.Invoke(CurrentDirection);
			    }
		    }
	    }
    }

    /// <summary>
    ///Force this object to face the direction currently set on the server for this object.
    /// </summary>
    private void ForceClientDirectionFromServer()
    {
	    //reset rotation offset since we are getting explicit absolute direction from server
	    clientDirection = serverDirection;
	    OnDirectionChange.Invoke(clientDirection);
    }


    //client requests the server to change serverDirection
    [Command]
    private void CmdChangeDirection(Orientation direction)
    {
	    serverDirection = new Orientation(direction.Degrees);
    }

    //syncvar hook invoked when server sends a client the new direction for this object
    private void SyncServerDirection(Orientation oldDir, Orientation dir)
    {
	    serverDirection = new Orientation(dir.Degrees);
	    //we only change our direction if we're not local player (local player is always predictive)
	    //and not explicitly ignoring server updates.
	    if (!IgnoreServerUpdates && !IsLocalPlayer)
	    {
		    clientDirection = dir;
		    OnDirectionChange.Invoke(serverDirection);
	    }
    }
}

/// <summary>
/// Event which indicates a direction change has occurred.
/// </summary>
public class DirectionChangeEvent : UnityEvent<Orientation>{}
