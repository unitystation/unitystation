using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using Core.Networking;
using Systems;
using Messages.Server;
using Messages.Client;
using Messages.Client.NewPlayer;
using UI;

namespace Player
{
	/// <summary>
	/// This is the Viewer object for a joined player.
	/// Once they join they will have local ownership of this object until a job is determined
	/// and then they are spawned as player entity
	/// </summary>
	public class JoinedViewer : NetworkBehaviour
	{
		public bool IsValidPlayerAndWaitingOnLoad = false; //Note This class is reused for multiple Connections

		public string STUnverifiedClientId;
		public string STVerifiedUserid;
		public PlayerInfo STVerifiedConnPlayer;

		public override void OnStartLocalPlayer()
		{
			base.OnStartLocalPlayer();

			PlayerManager.SetViewerForControl(this);

			if (isServer)
			{
				RequestObserverRefresh.Send(SceneManager.GetActiveScene().name);
				HandleServerConnection();
			}
			else
			{
				CmdServerSetupPlayer(SceneManager.GetActiveScene().name);
			}
		}

		private void HandleServerConnection()
		{
			ServerSetUpPlayer(string.Empty);
			ClientFinishLoading();
		}

		[Command]
		private void CmdServerSetupPlayer(string currentScene)
		{
			ClearCache();
			ServerSetUpPlayer(currentScene);
		}

		[Server]
		private void ServerRequestLoadedScenes(string AlreadyLoaded)
		{
			List<SceneInfo> SceneS = new List<SceneInfo>();

			foreach (var Scene in SubSceneManager.Instance.loadedScenesList)
			{
				if (AlreadyLoaded == Scene.SceneName) continue;
				SceneS.Add(Scene);
			}

			RpcLoadScenes(JsonConvert.SerializeObject(SceneS), AlreadyLoaded);
		}

		[TargetRpc]
		void RpcLoadScenes(string Data, string OriginalScene)
		{
			if (isServer)
			{
				return;
			}

			SubSceneManager.Instance.LoadScenesFromServer(JsonConvert.DeserializeObject<List<SceneInfo>>(Data), OriginalScene, CMDFinishLoading);
		}

		[Server]
		private void ServerSetUpPlayer(string currentScene)
		{
			var authData = (AuthData) connectionToClient.authenticationData;

			// Sanity check in case Mirror does a surprising thing and allows commands from unauthenticated clients.
			if (connectionToClient.isAuthenticated == false)
			{
				Logger.Log($"A client attempted to set up their server player object but they haven't authenticated yet! Address: {connectionToClient.address}.");
				return;
			}

			Logger.LogTrace($"{authData.Username}'s {nameof(JoinedViewer)} called CmdServerSetupPlayer. ClientId: {authData.ClientId}.",
					Category.Connections);


			var Existingplayer = PlayerList.Instance.GetLoggedOffClient(authData.ClientId, authData.AccountId);

			if (BuildPreferences.isForRelease)
			{
				if (Existingplayer == null)
				{
					Existingplayer = PlayerList.Instance.GetLoggedOnClient(authData.ClientId, authData.AccountId);
				}
			}

			if (Existingplayer == null)
			{
				Existingplayer = new PlayerInfo
				{
					Connection = connectionToClient,
					GameObject = gameObject,
					Username = authData.Username,
					Job = JobType.NULL,
					ClientId = authData.ClientId,
					UserId = authData.AccountId,
					ConnectionIP = connectionToClient.address
				};
			}

			Existingplayer.Connection = connectionToClient;
			Existingplayer.ClientId = authData.ClientId;
			Existingplayer.UserId = authData.AccountId;
			Existingplayer.ConnectionIP = connectionToClient.address;
			// Register player to player list (logging code exists in PlayerList so no need for extra logging here)
			var player = PlayerList.Instance.AddOrUpdate(Existingplayer);

			// Check if they're admin / banned etc
			var isValidPlayer = PlayerList.Instance.TryLogIn(player);
			if (isValidPlayer == false)
			{
				ClearCache();
				PlayerList.Instance.Remove(player);
				Logger.LogWarning($"Set up new player: invalid player. For {authData.Username}", Category.Connections);
				return;
			}

			//Add player to the list of current round players
			PlayerList.Instance.AddToRoundPlayers(player);

			//Send to client their job ban entries
			var jobBanEntries = PlayerList.Instance.ClientAskingAboutJobBans(player);
			PlayerList.ServerSendsJobBanDataMessage.Send(player.Connection, jobBanEntries);

			//Send to client the current crew job counts
			if (CrewManifestManager.Instance != null)
			{
				SetJobCountsMessage.SendToPlayer(CrewManifestManager.Instance.Jobs, player);
			}

			UpdateConnectedPlayersMessage.Send();

			IsValidPlayerAndWaitingOnLoad = true;
			STUnverifiedClientId = authData.ClientId;
			STVerifiedUserid = authData.AccountId;
			STVerifiedConnPlayer = player;
			if (string.IsNullOrEmpty(currentScene) == false)
			{
				ServerRequestLoadedScenes(currentScene);
			}
		}

		[Command]
		public void CMDFinishLoading()
		{
			if (IsValidPlayerAndWaitingOnLoad == false)
			{
				connectionToClient.Disconnect();
				return;
			}

			if (STVerifiedConnPlayer.Connection != connectionToClient)
			{
				connectionToClient.Disconnect();
				ClearCache();
				return;
			}
			ClientFinishLoading();
		}

		public void ClearCache()
		{
			IsValidPlayerAndWaitingOnLoad = false;
			STUnverifiedClientId = null;
			STVerifiedUserid = null;
			STVerifiedConnPlayer = null;
		}

		public void ClientFinishLoading()
		{
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

			PlayerInfo loggedOffPlayer = null;

			if (BuildPreferences.isForRelease == false)
			{
				// Check if they have a player to rejoin before creating a new ConnectedPlayer Doing it by a STUnverifiedClientId So multiple can connect with the same account for devs
				loggedOffPlayer = PlayerList.Instance.RemovePlayerbyClientId(STUnverifiedClientId, STVerifiedUserid, STVerifiedConnPlayer);
			}
			else
			{
				loggedOffPlayer = PlayerList.Instance.RemovePlayerbyUserId(STVerifiedUserid, STVerifiedConnPlayer);
			}


			var checkForViewer = loggedOffPlayer?.GameObject.OrNull()?.GetComponent<JoinedViewer>();
			if (checkForViewer)
			{
				NetworkServer.Destroy(loggedOffPlayer.GameObject);
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

			PlayerList.Instance.CheckAdminState(STVerifiedConnPlayer);
			PlayerList.Instance.CheckMentorState(STVerifiedConnPlayer, STVerifiedUserid);
			ClearCache();
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
								"Cannot rejoin that player. Was original player object improperly created? " +
								"Did we get runtime error while creating it?", Category.Connections);
				// TODO: if this issue persists, should probably send the poor player a message about failing to rejoin.
				yield break;
			}

			while (!netIdentity.observers.ContainsKey(connectionToClient.connectionId))
			{
				yield return WaitFor.EndOfFrame;
				if (connectionToClient == null)
				{
					//disconnected while we were waiting
					yield break;
				}
			}

			TargetLocalPlayerRejoinUI(connectionToClient);
			PlayerSpawn.ServerRejoinPlayer(this, loggedOffPlayer);
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
			var jsonCharSettings = JsonConvert.SerializeObject(PlayerManager.CurrentCharacterSheet);

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
			var jsonCharSettings = JsonConvert.SerializeObject(PlayerManager.CurrentCharacterSheet);
			CmdSpectate(jsonCharSettings);
		}

		/// <summary>
		/// Command to spectate a round instead of spawning as a player
		/// </summary>
		[Command]
		public void CmdSpectate(string jsonCharSettings)
		{
			var characterSettings = JsonConvert.DeserializeObject<CharacterSheet>(jsonCharSettings);
			PlayerSpawn.ServerNewPlayerSpectate(this, characterSettings);
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

		/// <summary>
		/// Mark this joined viewer as ready for job allocation
		/// </summary>
		public void SetReady(bool isReady)
		{
			var jsonCharSettings = "";
			if (isReady)
			{
				jsonCharSettings = JsonConvert.SerializeObject(PlayerManager.CurrentCharacterSheet);
			}
			CmdPlayerReady(isReady, jsonCharSettings);
		}

		[Command]
		private void CmdPlayerReady(bool isReady, string jsonCharSettings)
		{
			var player = PlayerList.Instance.GetOnline(connectionToClient);

			CharacterSheet charSettings = null;
			if (isReady)
			{
				charSettings = JsonConvert.DeserializeObject<CharacterSheet>(jsonCharSettings);
			}
			PlayerList.Instance.SetPlayerReady(player, isReady, charSettings);
		}
	}
}
