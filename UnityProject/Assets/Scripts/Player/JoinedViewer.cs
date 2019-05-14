using System.Collections;
using System.Collections.Generic;
using System.Threading;
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
	}

	public override void OnStartLocalPlayer()
	{
		base.OnStartLocalPlayer();

		// Send steamId to server for player setup.
		if (BuildPreferences.isSteamServer)
		{
			CmdServerSetupPlayer(Client.Instance.SteamId);
		}
		else
		{
			CmdServerSetupPlayer(0);
		}
	}

	[Command]
	private void CmdServerSetupPlayer(ulong steamId)
	{
		//Add player to player list
		PlayerList.Instance.Add(new ConnectedPlayer
		{
			Connection = connectionToClient,
			GameObject = gameObject,
			Job = JobType.NULL,
			SteamId = steamId
		});

		// If they have a player to rejoin send the client the player to rejoin, otherwise send a null gameobject.
		TargetLocalPlayerSetupPlayer(connectionToClient, PlayerList.Instance.TakeLoggedOffPlayer(steamId));
	}

	[TargetRpc]
	private void TargetLocalPlayerSetupPlayer(NetworkConnection target, GameObject loggedOffPlayer)
	{
		PlayerManager.SetViewerForControl(this);
		UIManager.ResetAllUI();
		UIManager.SetDeathVisibility(true);

		if (BuildPreferences.isSteamServer)
		{
			//Send request to be authenticated by the server
			StartCoroutine(WaitUntilServerInit());
		}

		// If player is joining for the first time let them pick faction and job, otherwise rejoin character.
		if (loggedOffPlayer == null)
		{
			UIManager.Display.DetermineGameMode();
		}
		else
		{
			CmdRejoin(loggedOffPlayer);
			loggedOffPlayer.GetComponent<PlayerSync>().setLocalPlayer();
			loggedOffPlayer.GetComponent<PlayerScript>().Init();
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
	public void CmdRequestJob(JobType jobType, CharacterSettings characterSettings)
	{
		var player = PlayerList.Instance.Get(connectionToClient);
		/// Verifies that the player has no job
		if (player.Job == JobType.NULL)
		{
			SpawnHandler.RespawnPlayer(connectionToClient, playerControllerId,
			GameManager.Instance.GetRandomFreeOccupation(jobType), characterSettings, gameObject);

		}
		/// Spawns in player if they have a job but aren't spawned
		else if (player.GameObject == null)
		{
			SpawnHandler.RespawnPlayer(connectionToClient, playerControllerId,
			GameManager.Instance.GetRandomFreeOccupation(player.Job), characterSettings, gameObject);

		}
		else
		{
			Logger.LogWarning("[Jobs] Request Job Failed: Already Has Job", Category.Jobs);


		}

	}

	/// <summary>
	/// Asks the server to let the client rejoin into a logged off character.
	/// </summary>
	/// <param name="loggedOffPlayer">The character to be rejoined into.</param>
	[Command]
	public void CmdRejoin(GameObject loggedOffPlayer)
	{
		SpawnHandler.TransferPlayer(connectionToClient, playerControllerId, loggedOffPlayer, gameObject, EVENT.PlayerSpawned, null);
		loggedOffPlayer.GetComponent<PlayerScript>().playerNetworkActions.ReenterBodyUpdates(loggedOffPlayer);
	}
}