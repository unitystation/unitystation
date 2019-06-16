using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

/// <summary>
/// Component which allows an object to have an orientation (facing) which is synced over the network and supports client
/// prediction. It's up to other components to decide how to update their display when direction changes,
/// by subscribing to the OnDirectionChange UnityEvent. Client prediction / server sync is entirely handled
/// within this component, so components which react to direction changes don't need to think about it - just subscribe to the event.
///
/// This component is intended to eventually be used for all objects which have some sort of directional behavior,
/// not just players.
/// </summary>
public class Directional : NetworkBehaviour
{
	[SyncVar(hook = nameof(ServerDirectionChangeHook))]
	private Orientation serverDirection;
	private Orientation clientDirection;

	private bool IsLocalPlayer => PlayerManager.LocalPlayer == gameObject;

	/// <summary>
	/// Turn this on when doing client prediction - this Directional will completely ignore server
	/// direction updates and only perform direction changes when they are made locally.
	/// </summary>
	public bool IgnoreServerUpdates = false;

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
		isServer || (!IsLocalPlayer && !IgnoreServerUpdates) ? serverDirection : clientDirection;

	public override void OnStartServer()
    {
	    ServerDirectionChangeHook(Orientation.Down);
    }

    public override void OnStartClient()
    {
	    StartCoroutine(WaitForLoad());
    }

    private IEnumerator WaitForLoad()
    {
	    yield return WaitFor.EndOfFrame;
	    if (PlayerManager.LocalPlayer == gameObject)
	    {
		    //we ignore server updates (unless forced) for our local player
		    IgnoreServerUpdates = true;
		    SyncDirection();
	    }
    }

    /// <summary>
    ///Force this object to face the direction currently set on the server for this object.
    /// </summary>
    private void SyncDirection()
    {
	    clientDirection = serverDirection;
	    OnDirectionChange.Invoke(clientDirection);
    }

    /// <summary>
    /// Cause the player to face the specified direction. On server, syncs the direction to all clients.
    /// On client, if this object IsLocalPlayer, requests the direction change from the server and
    /// locally predicts it. On a client object that is not the local player, this locally predicts the change (when
    /// the next value is received from server it will switch to that)
    /// </summary>
    /// <param name="newDir"></param>
    public void FaceDirection(Orientation newDir)
    {
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
    /// Force the client to face the specified direction, even if IgnoreServerUpdates = true
    /// </summary>
    /// <param name="newDir"></param>
    [TargetRpc]
    public void TargetForceDirection(NetworkConnection target, Orientation newDir)
    {
	    FaceDirection(newDir);
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
		    OnDirectionChange.Invoke(serverDirection);
	    }
    }
}

/// <summary>
/// Event which indicates a direction change has occurred.
/// </summary>
public class DirectionChangeEvent : UnityEvent<Orientation>{}
