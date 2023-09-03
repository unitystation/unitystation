using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using Mirror;
using Core.Networking;
using Logs;
using Systems;
using Systems.Character;
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

			if (isServer && isLocalPlayer)
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
			ClearCache(true);
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

			SubSceneManager.Instance.LoadScenesFromServer(JsonConvert.DeserializeObject<List<SceneInfo>>(Data),
				OriginalScene, CMDFinishLoading);
		}

		[Server]
		private void ServerSetUpPlayer(string currentScene)
		{
			var authData = (AuthData) connectionToClient.authenticationData;

			// Sanity check in case Mirror does a surprising thing and allows commands from unauthenticated clients.
			if (connectionToClient.isAuthenticated == false)
			{
				Loggy.LogError(
					$"A client attempted to set up their server player object but they haven't authenticated yet! Address: {connectionToClient.address}.");
				ClearCache();
				return;
			}

			Loggy.LogTrace(
				$"{authData.Username}'s {nameof(JoinedViewer)} called CmdServerSetupPlayer. ClientId: {authData.ClientId}.",
				Category.Connections);


			var Existingplayer = PlayerList.Instance.GetLoggedOffClient(authData.ClientId, authData.AccountId);
			if (Existingplayer == null || Existingplayer == PlayerInfo.Invalid  )
			{
				if (GameData.Instance.DevBuild == false)
				{
					Existingplayer = PlayerList.Instance.GetLoggedOnClient(authData.ClientId, authData.AccountId);

					if (Existingplayer != null && Existingplayer.Connection != connectionToClient)
					{
						Loggy.LogError($"Disconnecting player {Existingplayer?.Name} via Disconnect previous Using account/mac Address ");
						Existingplayer.Connection?.Disconnect();
					}
				}
			}

			if (Existingplayer == null ||  Existingplayer == PlayerInfo.Invalid)
			{
				Existingplayer = new PlayerInfo
				{
					Connection = connectionToClient,
					GameObject = gameObject,
					Username = authData.Username,
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
				PlayerList.Instance.Remove(player);
				Loggy.LogError($"Set up new player: invalid player. For {authData.Username}", Category.Connections);
				ClearCache();
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
				Loggy.LogError($"Disconnecting {this.STVerifiedUserid} by Trying to call CMDFinishLoading When server wasn't expecting player to be loading  ", Category.Connections);
				connectionToClient.Disconnect();
				ClearCache();
				return;
			}

			if (STVerifiedConnPlayer.Connection != connectionToClient)
			{
				Loggy.LogError($"Disconnecting {this.STVerifiedConnPlayer.Name} by Authenticated user connection matching The game objects connection ", Category.Connections);
				connectionToClient.Disconnect();
				ClearCache();
				return;
			}

			ClientFinishLoading();
		}

		public void ClearCache(bool bNew = false)
		{
			IsValidPlayerAndWaitingOnLoad = false;
			STUnverifiedClientId = null;
			STVerifiedUserid = null;
			STVerifiedConnPlayer = null;
			if (bNew == false)
			{
				_ = Despawn.ServerSingle(this.gameObject);
			}
		}

		public void ClientFinishLoading()
		{
			IsValidPlayerAndWaitingOnLoad = false;
			// Only sync the pre-round countdown if it's already started.
			if (GameManager.Instance.CurrentRoundState == RoundState.PreRound)
			{
				if (GameManager.Instance.waitForStart)
				{
					TargetSyncCountdown(connectionToClient, GameManager.Instance.waitForStart,
						GameManager.Instance.CountdownEndTime);
				}
				else
				{
					GameManager.Instance.CheckPlayerCount();
				}
			}



			PlayerList.Instance.CheckAdminState(STVerifiedConnPlayer);
			PlayerList.Instance.CheckMentorState(STVerifiedConnPlayer, STVerifiedUserid);

			// If there's a logged off player, we will force them to rejoin their body
			if (STVerifiedConnPlayer.Mind == null) //TODO Handle when someone gets kicked out of their mind
			{
				TargetLocalPlayerSetupNewPlayer(connectionToClient, GameManager.Instance.CurrentRoundState);
				GameManager.Instance.OrNull()?.PlayerLoadedIn(connectionToClient);
				ClearCache(true);
			}
			else
			{
				StartCoroutine(WaitForLoggedOffObserver(STVerifiedConnPlayer.Mind));
			}
		}

		/// <summary>
		/// Waits for the client to be an observer of the player before continuing
		/// </summary>
		private IEnumerator WaitForLoggedOffObserver(Mind loggedOffPlayer)
		{
			TargetLocalPlayerRejoinUI(connectionToClient);
			// TODO: When we have scene network culling we will need to allow observers
			// for the whole specific scene and the body before doing the logic below:
			var netIdentity = loggedOffPlayer.GetComponent<NetworkIdentity>();
			if (netIdentity == null)
			{
				Loggy.LogError($"No {nameof(NetworkIdentity)} component on {loggedOffPlayer}! " +
				                "Cannot rejoin that player. Was original player object improperly created? " +
				                "Did we get runtime error while creating it?", Category.Connections);
				// TODO: if this issue persists, should probably send the poor player a message about failing to rejoin.
				ClearCache();
				yield break;
			}

			while (!netIdentity.observers.ContainsKey(connectionToClient.connectionId))
			{
				yield return WaitFor.EndOfFrame;
				if (connectionToClient == null)
				{
					//disconnected while we were waiting
					ClearCache();
					yield break;
				}
			}



			TargetLocalPlayerRejoinUI(connectionToClient);
			GameManager.Instance.OrNull()?.PlayerLoadedIn(connectionToClient);
			STVerifiedConnPlayer.Mind.OrNull()?.ReLog();


			ClearCache();
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
			var jsonCharSettings = JsonConvert.SerializeObject(PlayerManager.ActiveCharacter);

			if (PlayerList.Instance.ClientJobBanCheck(job) == false)
			{
				Loggy.LogWarning($"Client failed local job-ban check for {job}.", Category.Jobs);
				UIManager.Display.jobSelectWindow.GetComponent<GUI_PlayerJobs>()
					.ShowFailMessage(JobRequestError.JobBanned);
				return;
			}

			ClientRequestJobMessage.Send(job, jsonCharSettings, DatabaseAPI.ServerData.UserID);
		}

		public void RequestJob(int attribute)
		{
			var jsonCharSettings = JsonConvert.SerializeObject(PlayerManager.ActiveCharacter);
			ClientRequestSpawnWithAttribute.Send(attribute, jsonCharSettings, DatabaseAPI.ServerData.UserID);
		}

		public void Spectate()
		{
			var jsonCharSettings = JsonConvert.SerializeObject(PlayerManager.ActiveCharacter);
			ClientRequestJobMessage.Send(JobType.NULL, jsonCharSettings, DatabaseAPI.ServerData.UserID);
		}

		/// <summary>
		/// Tells the client to start the countdown if it's already started
		/// </summary>
		[TargetRpc]
		private void TargetSyncCountdown(NetworkConnection target, bool started, double endTime)
		{
			Loggy.Log("Syncing countdown!", Category.Round);
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
				jsonCharSettings = JsonConvert.SerializeObject(PlayerManager.ActiveCharacter);
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