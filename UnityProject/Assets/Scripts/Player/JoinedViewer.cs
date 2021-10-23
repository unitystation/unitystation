using System;
using System.Collections;
using System.Net.NetworkInformation;
using Systems;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using Messages.Server;
using Messages.Client;
using Messages.Client.NewPlayer;
using UI;

/// <summary>
/// This is the Viewer object for a joined player.
/// Once they join they will have local ownership of this object until a job is determined
/// and then they are spawned as player entity
/// </summary>
public class JoinedViewer : NetworkBehaviour
{
	public override void OnStartLocalPlayer()
	{
		base.OnStartLocalPlayer();
		RequestObserverRefresh.Send(SceneManager.GetActiveScene().name);
		PlayerManager.SetViewerForControl(this);

		CmdServerSetupPlayer(GetNetworkInfo(),
			PlayerManager.CurrentCharacterSettings.Username, DatabaseAPI.ServerData.UserID, GameData.BuildNumber,
			DatabaseAPI.ServerData.IdToken);
	}

	[Command]
	private void CmdServerSetupPlayer(string unverifiedClientId, string unverifiedUsername,
		string unverifiedUserid, int unverifiedClientVersion, string unverifiedToken)
	{
		ServerSetUpPlayer(unverifiedClientId, unverifiedUsername, unverifiedUserid, unverifiedClientVersion, unverifiedToken);
	}

	[Server]
	private async void ServerSetUpPlayer(
		string unverifiedClientId,
		string unverifiedUsername,
		string unverifiedUserid,
		int unverifiedClientVersion,
		string unverifiedToken)
	{
		Logger.LogFormat("A joinedviewer called CmdServerSetupPlayer on this server, Unverified ClientId: {0} Unverified Username: {1}",
			Category.Connections,
			unverifiedClientId, unverifiedUsername);

		// Register player to player list (logging code exists in PlayerList so no need for extra logging here)
		var unverifiedConnPlayer = PlayerList.Instance.AddOrUpdate(new ConnectedPlayer
		{
			Connection = connectionToClient,
			GameObject = gameObject,
			Username = unverifiedUsername,
			Job = JobType.NULL,
			ClientId = unverifiedClientId,
			UserId = unverifiedUserid
		});

		// this validates Userid and Token
		var isValidPlayer = await PlayerList.Instance.ValidatePlayer(unverifiedClientId, unverifiedUsername,
			unverifiedUserid, unverifiedClientVersion, unverifiedConnPlayer, unverifiedToken);

		if (isValidPlayer == false)
		{
			Logger.LogWarning($"Set up new player: invalid player. For {unverifiedUsername}", Category.Connections);
			return;
		}

		//Send to client their job ban entries
		var jobBanEntries = PlayerList.Instance.ClientAskingAboutJobBans(unverifiedConnPlayer);
		PlayerList.ServerSendsJobBanDataMessage.Send(unverifiedConnPlayer.Connection, jobBanEntries);

		//Send to client the current crew job counts
		if (CrewManifestManager.Instance != null)
		{
			SetJobCountsMessage.SendToPlayer(CrewManifestManager.Instance.Jobs, unverifiedConnPlayer);
		}

		UpdateConnectedPlayersMessage.Send();

		// Only sync the pre-round countdown if it's already started.
		if (GameManager.Instance.CurrentRoundState == RoundState.PreRound)
		{
			if (GameManager.Instance.waitForStart)
			{
				TargetSyncCountdown(connectionToClient, GameManager.Instance.waitForStart, GameManager.Instance.CountdownEndTime);
			}
			else
			{
				GameManager.Instance.CheckPlayerCount();
			}
		}

		// Check if they have a player to rejoin before creating a new ConnectedPlayer
		var loggedOffPlayer = PlayerList.Instance.RemovePlayerbyClientId(unverifiedClientId, unverifiedUserid, unverifiedConnPlayer);
		var checkForViewer = loggedOffPlayer?.GameObject.GetComponent<JoinedViewer>();
		if (checkForViewer)
		{
			Destroy(loggedOffPlayer.GameObject);
			loggedOffPlayer = null;
		}

		// If there's a logged off player, we will force them to rejoin their body
		if (loggedOffPlayer == null)
		{
			TargetLocalPlayerSetupNewPlayer(connectionToClient, GameManager.Instance.CurrentRoundState);
		}
		else
		{
			StartCoroutine(WaitForLoggedOffObserver(loggedOffPlayer.GameObject));
		}

		PlayerList.Instance.CheckAdminState(unverifiedConnPlayer, unverifiedUserid);
		PlayerList.Instance.CheckMentorState(unverifiedConnPlayer, unverifiedUserid);
	}

	/// <summary>
	/// Waits for the client to be an observer of the player before continuing
	/// </summary>
	private IEnumerator WaitForLoggedOffObserver(GameObject loggedOffPlayer)
	{
		TargetLocalPlayerRejoinUI(connectionToClient);
		// TODO: When we have scene network culling we will need to allow observers
		// for the whole specific scene and the body before doing the logic below:
		var netIdentity = loggedOffPlayer.GetComponent<NetworkIdentity>();
		if (netIdentity == null)
		{
			Logger.LogError($"No {nameof(NetworkIdentity)} component on {loggedOffPlayer}! " +
					"Cannot rejoin that player. Was original player object improperly created? "+
					"Did we get runtime error while creating it?", Category.Connections);
			// TODO: if this issue persists, should probably send the poor player a message about failing to rejoin.
			yield break;
		}

		while (netIdentity != null && connectionToClient != null && !netIdentity.observers.ContainsKey(this.connectionToClient.connectionId))
		{
			yield return WaitFor.EndOfFrame;
		}

		if (netIdentity != null && connectionToClient != null)
		{
			yield return WaitFor.EndOfFrame;
			TargetLocalPlayerRejoinUI(connectionToClient);
			PlayerSpawn.ServerRejoinPlayer(this, loggedOffPlayer);
		}
		else
		{
			Logger.LogError($"No {nameof(NetworkIdentity)} component on {loggedOffPlayer}! " +
			                "Turns out the NetID was destroyed for some reason while waiting for to be an observer" +
			                "of the logged off player", Category.Connections);
		}
	}

	[TargetRpc]
	private void TargetLocalPlayerRejoinUI(NetworkConnection target)
	{
		UIManager.Display.preRoundWindow.ShowRejoiningPanel();
	}

	/// <summary>
	/// Target which tells this joined viewer they are a new player, tells them what their ID is,
	/// and tells them what round state the game is on
	/// </summary>
	/// <param name="target">this connection</param>
	[TargetRpc]
	private void TargetLocalPlayerSetupNewPlayer(NetworkConnection target, RoundState roundState)
	{
		// clear our UI because we're about to change it based on the round state
		UIManager.ResetAllUI();

		// Determine what to do depending on the state of the round
		switch (roundState)
		{
			case RoundState.PreRound:
				// Round hasn't yet started, give players the pre-game screen
				UIManager.Display.SetScreenForPreRound();
				break;
			default:
				// Show the joining screen
				UIManager.Display.SetScreenForJoining();
				break;
		}
	}

	public void RequestJob(JobType job)
	{
		var jsonCharSettings = JsonConvert.SerializeObject(PlayerManager.CurrentCharacterSettings);

		if (PlayerList.Instance.ClientJobBanCheck(job) == false)
		{
			Logger.LogWarning($"Client failed local job-ban check for {job}.", Category.Jobs);
			UIManager.Display.jobSelectWindow.GetComponent<GUI_PlayerJobs>().ShowFailMessage(JobRequestError.JobBanned);
			return;
		}

		ClientRequestJobMessage.Send(job, jsonCharSettings, DatabaseAPI.ServerData.UserID);
	}

	public void Spectate()
	{
		var jsonCharSettings = JsonConvert.SerializeObject(PlayerManager.CurrentCharacterSettings);
		CmdSpectate(jsonCharSettings);
	}

	/// <summary>
	/// Command to spectate a round instead of spawning as a player
	/// </summary>
	[Command]
	public void CmdSpectate(string jsonCharSettings)
	{
		var characterSettings = JsonConvert.DeserializeObject<CharacterSettings>(jsonCharSettings);
		PlayerSpawn.ServerSpawnGhost(this, characterSettings);
	}

	/// <summary>
	/// Tells the client to start the countdown if it's already started
	/// </summary>
	[TargetRpc]
	private void TargetSyncCountdown(NetworkConnection target, bool started, double endTime)
	{
		Logger.Log("Syncing countdown!", Category.Round);
		UIManager.Display.preRoundWindow.GetComponent<GUI_PreRoundWindow>().SyncCountdown(started, endTime);
	}

	private string GetNetworkInfo()
	{
		var nics = NetworkInterface.GetAllNetworkInterfaces();
		foreach (var n in nics)
		{
			if (string.IsNullOrEmpty(n.GetPhysicalAddress().ToString()) == false)
			{
				return n.GetPhysicalAddress().ToString();
			}
		}

		return "";
	}

	/// <summary>
	/// Mark this joined viewer as ready for job allocation
	/// </summary>
	public void SetReady(bool isReady)
	{
		var jsonCharSettings = "";
		if (isReady)
		{
			jsonCharSettings = JsonConvert.SerializeObject(PlayerManager.CurrentCharacterSettings);
		}
		CmdPlayerReady(isReady, jsonCharSettings);
	}

	[Command]
	private void CmdPlayerReady(bool isReady, string jsonCharSettings)
	{
		var player = PlayerList.Instance.GetByConnection(connectionToClient);

		CharacterSettings charSettings = null;
		if (isReady)
		{
			charSettings = JsonConvert.DeserializeObject<CharacterSettings>(jsonCharSettings);
		}
		PlayerList.Instance.SetPlayerReady(player, isReady, charSettings);
	}
}
