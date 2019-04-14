using System.Collections;
using System.Collections.Generic;
using Facepunch.Steamworks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// This is the Viewer object for a joined player. 
/// Once they join they will have local ownership of this object until a job is determined
/// and then they are spawned as player entity
/// </summary>
public class JoinedViewer : NetworkBehaviour
{
    public override void OnStartServer()
    {
        base.OnStartServer();
        //Add player to player list
        PlayerList.Instance.Add(new ConnectedPlayer
        {
            Connection = connectionToClient,
                GameObject = gameObject,
                Job = JobType.NULL
        });
    }
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        PlayerManager.SetViewerForControl(this);
        UIManager.ResetAllUI();
        UIManager.Display.DetermineGameMode();
        UIManager.SetDeathVisibility(true);

        if (BuildPreferences.isSteamServer)
        {
            //Send request to be authenticated by the server
            StartCoroutine(WaitUntilServerInit());
        }
    }

    //Just ensures connected player record is set on the server first before Auth req is sent
    IEnumerator WaitUntilServerInit()
    {
        yield return YieldHelper.EndOfFrame;
        if (Client.Instance != null)
        {
            Logger.Log("Client Requesting Auth", Category.Steam);
            // Generate authentication Ticket
            var ticket = Client.Instance.Auth.GetAuthSessionTicket();
            var ticketBinary = ticket.Data;
            // Send Clientmessage to authenticate
            RequestAuthMessage.Send(Client.Instance.SteamId, ticketBinary);
        }
        else
        {
            Logger.Log("Client NOT requesting auth", Category.Steam);
        }
    }

    /// <summary>
    /// At the moment players can choose their jobs on round start:
    /// </summary>
    [Command]
    public void CmdRequestJob(JobType jobType)
    {
        var player = PlayerList.Instance.Get(connectionToClient);
        /// Verifies that the player has no job
        if (player.Job == JobType.NULL)
        {
            SpawnHandler.RespawnPlayer(connectionToClient, playerControllerId,
            GameManager.Instance.GetRandomFreeOccupation(jobType));

        }
        /// Spawns in player if they have a job but aren't spawned
        else if (player.GameObject == null)
        {
            SpawnHandler.RespawnPlayer(connectionToClient, playerControllerId,
            GameManager.Instance.GetRandomFreeOccupation(player.Job));

        }
        else
        {
            Logger.LogWarning("[Jobs] Request Job Failed: Already Has Job");


        }
        
    }
}