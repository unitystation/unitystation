using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Systems;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
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
				CmdServerSetupPlayer(GetNetworkInfo(),
					PlayerManager.CurrentCharacterSettings.Username, DatabaseAPI.ServerData.UserID, GameData.BuildNumber,
					DatabaseAPI.ServerData.IdToken, SceneManager.GetActiveScene().name);

			}
		}



		private async void HandleServerConnection()
		{
			await ServerSetUpPlayer(GetNetworkInfo(),
				PlayerManager.CurrentCharacterSettings.Username, DatabaseAPI.ServerData.UserID, GameData.BuildNumber,
				DatabaseAPI.ServerData.IdToken, "");
			ClientFinishLoading();
		}

		[Command]
		private void CmdServerSetupPlayer(string unverifiedClientId, string unverifiedUsername,
			string unverifiedUserid, int unverifiedClientVersion, string unverifiedToken, string clientCurrentScene)
		{
			ClearCache();
			_ = ServerSetUpPlayer(unverifiedClientId, unverifiedUsername, unverifiedUserid, unverifiedClientVersion, unverifiedToken,clientCurrentScene);
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
		private async Task ServerSetUpPlayer(
			string unverifiedClientId,
			string unverifiedUsername,
			string unverifiedUserid,
			int unverifiedClientVersion,
			string unverifiedToken,
			string clientCurrentScene)
		{
			Logger.LogFormat("A joinedviewer called CmdServerSetupPlayer on this server, Unverified ClientId: {0} Unverified Username: {1}",
				Category.Connections,
				unverifiedClientId, unverifiedUsername);

			// Register player to player list (logging code exists in PlayerList so no need for extra logging here)
			var unverifiedConnPlayer = PlayerList.Instance.AddOrUpdate(new PlayerInfo
			{
				Connection = connectionToClient,
				GameObject = gameObject,
				Username = unverifiedUsername,
				Job = JobType.NULL,
				ClientId = unverifiedClientId,
				UserId = unverifiedUserid,
				ConnectionIP = connectionToClient.address
			});

			// this validates Userid and Token
			// and does a lot more stuff
			var isValidPlayer = await PlayerList.Instance.TryLogIn(unverifiedConnPlayer, unverifiedClientVersion, unverifiedToken);
			if (isValidPlayer == false)
			{
				ClearCache();
				PlayerList.Instance.Remove(unverifiedConnPlayer);
				Logger.LogWarning($"Set up new player: invalid player. For {unverifiedUsername}", Category.Connections);
				return;
			}

			//Add player to the list of current round players
			PlayerList.Instance.AddToRoundPlayers(unverifiedConnPlayer);

			//Send to client their job ban entries
			var jobBanEntries = PlayerList.Instance.ClientAskingAboutJobBans(unverifiedConnPlayer);
			PlayerList.ServerSendsJobBanDataMessage.Send(unverifiedConnPlayer.Connection, jobBanEntries);

			//Send to client the current crew job counts
			if (CrewManifestManager.Instance != null)
			{
				SetJobCountsMessage.SendToPlayer(CrewManifestManager.Instance.Jobs, unverifiedConnPlayer);
			}

			UpdateConnectedPlayersMessage.Send();

			IsValidPlayerAndWaitingOnLoad = true;
			STUnverifiedClientId = unverifiedClientId;
			STVerifiedUserid = unverifiedUserid; // Is validated within PlayerList.TryLogIn()
			STVerifiedConnPlayer = unverifiedConnPlayer;
			if (string.IsNullOrEmpty(clientCurrentScene) == false)
			{
				ServerRequestLoadedScenes(clientCurrentScene);
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

			// Check if they have a player to rejoin before creating a new ConnectedPlayer
			var loggedOffPlayer = PlayerList.Instance.RemovePlayerbyClientId(STUnverifiedClientId, STVerifiedUserid, STVerifiedConnPlayer);
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
			var player = PlayerList.Instance.GetOnline(connectionToClient);

			CharacterSettings charSettings = null;
			if (isReady)
			{
				charSettings = JsonConvert.DeserializeObject<CharacterSettings>(jsonCharSettings);
			}
			PlayerList.Instance.SetPlayerReady(player, isReady, charSettings);
		}
	}
}
