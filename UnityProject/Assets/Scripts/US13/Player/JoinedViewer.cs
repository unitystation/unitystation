using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Logs;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using US13.Core.Admin.Logs;
using US13.Core.Lifecycle;
using US13.Core.Networking;
using US13.Core.Sprite_Handler;
using US13.Core.Utils;
using US13.Managers;
using US13.Managers.MatrixManager;
using US13.Managers.NetworkManagement;
using US13.Managers.SubSceneManager;
using US13.Managers.Supporters;
using US13.Messages.Client;
using US13.Messages.Client.Admin;
using US13.Messages.Client.NewPlayer;
using US13.Messages.Client.SpriteMessages;
using US13.Messages.Server;
using US13.Messages.Server.JoinedViewer;
using US13.Messages.Server.Mapping;
using US13.Systems.Lobby;
using US13.Systems.Occupations;
using US13.Systems.Permissions;
using US13.UI.Systems;
using US13.UI.Systems.Jobs;
using US13.UI.Systems.PreRound;
using Util;
using Util.Independent.FluentRichText;

namespace US13.Player
{
	/// <summary>
	/// This is the Viewer object for a joined player.
	/// Once they join they will have local ownership of this object until a job is determined
	/// and then they are spawned as player entity
	/// </summary>
	public class JoinedViewer : NetworkBehaviour
	{
		public static bool ClientValidated = false;

		public static List<Action> DelayTillAuthenticated = new List<Action>();

		private bool IsValidPlayerAndWaitingOnLoad = false; //Note This class is reused for multiple Connections

		private string STUnverifiedClientId;
		private string STVerifiedUserid;
		private PlayerInfo STVerifiedConnPlayer;

		[SyncVar] public bool ServerDoneLoading = false;

		public static void AddOnPlayerValidated(Action ToInvoke)
		{
			if (ClientValidated)
			{
				ToInvoke.Invoke();
			}
			else
			{
				DelayTillAuthenticated.Add(ToInvoke);
			}
		}

		public static void FinishedValidating()
		{
			ClientValidated = true;
			foreach (var Action in DelayTillAuthenticated)
			{
				try
				{
					Action.Invoke();
				}
				catch (Exception e)
				{
					Loggy.Error(e.ToString());
				}
			}
		}


		public override void OnStartLocalPlayer()
		{
			base.OnStartLocalPlayer();

			PlayerManager.SetViewerForControl(this);
			ServerDoneLoading = false;

			if (isServer && isLocalPlayer)
			{
				RequestObserverRefresh.Send(SceneManager.GetActiveScene().name);
				ServerSetUpPlayer(string.Empty);
				_ = ClientFinishLoading();
				FinishedValidating();
			}
			else
			{
				CmdServerSetupPlayer(SceneManager.GetActiveScene().name);
			}

			try
			{
				GUI_PreRoundWindow.Instance?.OnClientLoadUpdateStatus?.Invoke("Player Loading", "Prefetching character sheets..", 0.1f);
				_ = PlayerManager.CharacterManager.LoadCharacters();
			}
			catch (Exception e)
			{
				Loggy.Error(e.ToString());
			}
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

			foreach (var scene in SubSceneManager.Instance.loadedScenesList)
			{
				if (AlreadyLoaded == scene.SceneName) continue;
				SceneS.Add(scene);
			}
			RpcLoadScenes(JsonConvert.SerializeObject(SceneS), AlreadyLoaded);
		}

		[TargetRpc]
		private void RpcLoadScenes(string Data, string OriginalScene)
		{
			if (isServer) return;
			GUI_PreRoundWindow.Instance?.OnClientLoadUpdateStatus?.Invoke("Starting game", "Loading Scenes from server.", 0.2f);
			SubSceneManager.Instance.LoadScenesFromServer(JsonConvert.DeserializeObject<List<SceneInfo>>(Data),
				OriginalScene, ClientFinishedLoading);
		}

		[Server]
		private void ServerSetUpPlayer(string currentScene)
		{
			ServerDoneLoading = false;
			var authData = (AuthData) connectionToClient.authenticationData;

			// Sanity check in case Mirror does a surprising thing and allows commands from unauthenticated clients.
			if (connectionToClient.isAuthenticated == false)
			{
				Loggy.Error(
					$"A client attempted to set up their server player object but they haven't authenticated yet! Address: {connectionToClient.address}.");
				ClearCache();
				return;
			}

			Loggy.Trace(
				$"{authData.Account.Username}'s {nameof(JoinedViewer)} called CmdServerSetupPlayer. ClientId: {authData.ClientId}.",
				Category.Connections);


			bool GennewID = false;
			var Existingplayer = PlayerList.Instance.GetLoggedOffClient(authData.ClientId, authData.Account.Id);
			if (Existingplayer == null || Existingplayer == PlayerInfo.Invalid)
			{
				if (GameData.Instance.DevBuild == false)
				{
					Existingplayer = PlayerList.Instance.GetLoggedOnClient(authData.ClientId, authData.Account.Id);

					if (Existingplayer != null && Existingplayer.Connection != connectionToClient)
					{
						Loggy.Error($"Disconnecting player {Existingplayer?.Name} via Disconnect previous Using account/mac Address ");
						Existingplayer.Connection?.Disconnect();
					}
				}
				else
				{
					GennewID = true;

				}
			}

			if (Existingplayer == null ||  Existingplayer == PlayerInfo.Invalid)
			{
				if (GennewID && GameData.Instance.DevBuild)
				{
					authData.Account.Id = authData.Account.Id + RNG.GetRandomNumber(0, 10000);
				}

				Existingplayer = new PlayerInfo
				{
					Connection = connectionToClient,
					ConnectionIP = connectionToClient.address,
					ClientId = authData.ClientId,
					Account = authData.Account,
					GameObject = gameObject,
				};
			}

			Existingplayer.Connection = connectionToClient;
			Existingplayer.ClientId = authData.ClientId;
			Existingplayer.Account = authData.Account;
			Existingplayer.ConnectionIP = connectionToClient.address;
			// Register player to player list (logging code exists in PlayerList so no need for extra logging here)
			var player = PlayerList.Instance.AddOrUpdate(Existingplayer);

			// Check if they're admin / banned etc
			var isValidPlayer = PlayerList.Instance.TryLogIn(player);
			if (isValidPlayer == false)
			{
				// Any actions, including logging, done in CanPlayerJoin.
				PlayerList.Instance.Remove(player);
				Loggy.Warning($"Set up new player: invalid player. For {authData.Account.Username}", Category.Connections);
				ClearCache();

				return;
			}

			if (AdminSetWatchlist.Watchlist.ContainsKey(authData.Account.Id))
			{
				if (AdminSetWatchlist.Watchlist[authData.Account.Id])
				{
					AdminLogsManager.AddNewLog($"Player has joined who is on watchlist ID {authData.Account.Id}", LogCategory.Connections, BubbleUpToChatAdmin:true);
				}
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
			STVerifiedUserid = authData.Account.Id;
			STVerifiedConnPlayer = player;
			SendDataToClient();

			if (string.IsNullOrEmpty(currentScene) == false)
			{
				ServerRequestLoadedScenes(currentScene);
			}

			var supporterCheck = Supporters.IsSupporter(player);
			if (supporterCheck.Item1)
			{
				PermissionsManager.Instance.AddTempRoleTo(player.AccountId, "supporter");
			}
		}

		[Server]
		public void SendDataToClient()
		{
			foreach (var MapData in CustomNetworkManager.Instance.LoadedMapDatas)
			{
				ServerReturnMapData.Send(this.gameObject, MapData.Item1, ServerReturnMapData.MessageType.MapDataForClient, MapData.Item2);
			}

			foreach (var Matrix in MatrixManager.Instance.ActiveMatrices)
			{
				Matrix.Value.Matrix.MetaDataLayer.UpdateNewPlayer(connectionToClient);
				Matrix.Value.Matrix.TileChangeManager.UpdateNewPlayer(connectionToClient);
			}
		}

		[Client]
		public void ClientFinishedLoading()
		{
			FinishedValidating();
			CmdFinishLoading();
			SpriteRequestCurrentStateMessage.Send(SpriteHandlerManager.Instance.GetComponent<NetworkIdentity>().netId);
		}

		[Command]
		public void CmdFinishLoading()
		{
			if (IsValidPlayerAndWaitingOnLoad == false)
			{
				Loggy.Error($"Disconnecting {this.STVerifiedUserid} by Trying to call CMDFinishLoading When server wasn't expecting player to be loading  ", Category.Connections);
				AdminLogsManager.AddNewLog(null, $"Disconnecting {this.STVerifiedUserid} by Trying to call CMDFinishLoading When server wasn't expecting player to be loading", LogCategory.Connections, Severity.IMMEDIATE_ATTENTION);
				connectionToClient.Disconnect();
				ClearCache();
				return;
			}

			if (STVerifiedConnPlayer.Connection != connectionToClient)
			{
				Loggy.Error($"Disconnecting {this.STVerifiedConnPlayer.Name} by Authenticated user connection matching The game objects connection ", Category.Connections);
				AdminLogsManager.AddNewLog(null, $"Disconnecting {this.STVerifiedConnPlayer.Name} by Authenticated user connection matching The game objects connection", LogCategory.Connections, Severity.IMMEDIATE_ATTENTION);
				connectionToClient.Disconnect();
				ClearCache();
				return;
			}

			_ = ClientFinishLoading();
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
			GUI_PreRoundWindow.Instance?.OnClientLoadUpdateStatus?.Invoke("", "", 2f);
		}

		private async UniTask ClientFinishLoading()
		{
			// Only sync the pre-round countdown if it's already started.
			if (GameManager.Instance.CurrentRoundState == RoundState.PreRound)
			{
				if (GameManager.Instance.waitForStart)
				{
					GUI_PreRoundWindow.Instance?.OnClientLoadUpdateStatus?.Invoke("Starting game", "Syncing countdown end time.", 0.7f);
					TargetSyncCountdown(connectionToClient, GameManager.Instance.waitForStart,
						GameManager.Instance.CountdownEndTime);
				}
				else
				{
					GameManager.Instance.CheckPlayerCount();
				}
			}

			// If there's a logged off player, we will force them to rejoin their body
			if (STVerifiedConnPlayer.Mind == null) //TODO Handle when someone gets kicked out of their mind
			{
				GUI_PreRoundWindow.Instance?.OnClientLoadUpdateStatus?.Invoke("", "", 2f);
				TargetLocalPlayerSetupNewPlayer(connectionToClient, GameManager.Instance.CurrentRoundState);
				GameManager.Instance.OrNull()?.PlayerLoadedIn(connectionToClient);
				ClearCache(true);
			}
			else
			{
				GUI_PreRoundWindow.Instance?.OnClientLoadUpdateStatus?.Invoke("Starting game", "Found previous mind. Rejoining.", 0.9f);
				await WaitForLoggedOffObserver(STVerifiedConnPlayer.Mind);
			}
			IsValidPlayerAndWaitingOnLoad = false;
			ServerDoneLoading = true;
		}

		/// <summary>
		/// Waits for the client to be an observer of the player before continuing
		/// </summary>
		private async UniTask WaitForLoggedOffObserver(Mind loggedOffPlayer)
		{
			TargetLocalPlayerRejoinUI(connectionToClient, 0.1f, "Rejoining", "Waiting for logged off observer..");

			// TODO: When we have scene network culling we will need to allow observers
			// for the whole specific scene and the body before doing the logic below:
			var identity = loggedOffPlayer.GetComponent<NetworkIdentity>();
			if (identity == null)
			{
				GUI_PreRoundWindow.Instance?.OnClientLoadUpdateStatus?.Invoke("Encountered an issue while loading", "An error occurred. Press F5 to check for what error had occured.".Color(Color.red), 0f);
				Loggy.Error($"No {nameof(NetworkIdentity)} component on {loggedOffPlayer}! " +
				                "Cannot rejoin that player. Was original player object improperly created? " +
				                "Did we get runtime error while creating it?");
				// TODO: if this issue persists, should probably send the poor player a message about failing to rejoin.
				ClearCache();
				return;
			}

			var antiFreezeCheckCount = 0;
			while (connectionToClient != null && identity.observers.ContainsKey(connectionToClient.connectionId) == false)
			{
				antiFreezeCheckCount++;
				await UniTask.WaitForSeconds(1f);
				if (connectionToClient == null)
				{
					Loggy.Info("A client seemed to have discconected while we're waiting for their observer.");
					ClearCache();
					break;
				}
				if (antiFreezeCheckCount > 20)
				{
					GUI_PreRoundWindow.Instance?.OnClientLoadUpdateStatus?.Invoke("Encountered an issue while loading", "A problem occurred while attempting to check for a valid connection ID." +
						"No valid connection found after 20 seconds. Press F5 to check for if an error had occured.".Color(Color.red), 0f);
					Loggy.Error($"ID {connectionToClient.connectionId} not found in observers dictionary!" +
					            "Cannot rejoin that player. Was original player object improperly created? " +
					            "Did we get runtime error while creating it?");
					//FIXME: This is a temporary banadge for a game breaking issue.
					//(Max): I can't figure out why the observers dictionary isn't getting updated accordingly, or what is responsible for it.
					//This way of checking possesion IDs directly should at least stop players from getting stuck on round-rejoins,
					//but it isn't encourged to be the main way of handling this.
					AttemptFallback(loggedOffPlayer, connectionToClient);
					ClearCache();
					return;
				}
			}

			SuccesfullyRejoin();
		}

		private void AttemptFallback(Mind loggedOffPlayer, NetworkConnectionToClient conn = null)
		{
			if (conn == null) return; //weaver requirement
			foreach (var player in PlayerList.Instance.AllPlayers)
			{
				if (player.Mind == null || player.Mind.ControlledBy == null) continue;
				if (player.Mind.ControlledBy.Mind != loggedOffPlayer) continue;
				if (player.ViewerScript.IsValidPlayerAndWaitingOnLoad == false)
				{
					Loggy.Error($"{player.Username} detected while attempting to fallback to mind checks, but IsValidPlayerAndWaitingOnLoad is set to false?!");
				}
				SuccesfullyRejoin();
				break;
			}
		}

		private void SuccesfullyRejoin()
		{
			TargetLocalPlayerRejoinUI(connectionToClient, 0.9f, "Rejoining", "Successfully rejoined. Alerting Mind..");
			GameManager.Instance.OrNull()?.PlayerLoadedIn(connectionToClient);
			STVerifiedConnPlayer.Mind.OrNull()?.ReLog();
			ClearCache(true);
		}

		[TargetRpc]
		private void TargetLocalPlayerRejoinUI(NetworkConnection target, float amount, string loadingTitle, string loadingSubject)
		{
			UIManager.Display.preRoundWindow.LoadingArea.UpdateLoadingBar(loadingTitle, loadingSubject, amount);
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
			CharacterSheet characterSheet = PlayerManager.ActiveCharacter;

			if (characterSheet == null)
			{
				characterSheet = new CharacterSheet();
			}

			var jsonCharSettings = JsonConvert.SerializeObject(characterSheet);

			if (PlayerList.Instance.ClientJobBanCheck(job) == false)
			{
				Loggy.Warning($"Client failed local job-ban check for {job}.", Category.Jobs);
				UIManager.Display.jobSelectWindow.GetComponent<GUI_PlayerJobs>()
					.ShowFailMessage(JobRequestError.JobBanned);
				return;
			}

			ClientRequestJobMessage.Send(job, jsonCharSettings);
		}

		public void RequestJob(int attribute)
		{
			var jsonCharSettings = JsonConvert.SerializeObject(PlayerManager.ActiveCharacter);
			ClientRequestSpawnWithAttribute.Send(attribute, jsonCharSettings, PlayerManager.Account.Id);
		}

		public void Spectate()
		{
			var jsonCharSettings = JsonConvert.SerializeObject(PlayerManager.ActiveCharacter);
			ClientRequestJobMessage.Send(JobType.NULL, jsonCharSettings);
		}

		/// <summary>
		/// Tells the client to start the countdown if it's already started
		/// </summary>
		[TargetRpc]
		private void TargetSyncCountdown(NetworkConnection target, bool started, double endTime)
		{
			Loggy.Info("Syncing countdown!", Category.Round);
			UIManager.Display.preRoundWindow.GetComponent<GUI_PreRoundWindow>().CountdownArea.SyncCountdown(started, endTime);
		}

		/// <summary>
		/// Mark this joined viewer as ready for job allocation
		/// </summary>
		public void SetReady(bool isReady)
		{
			var jsonCharSettings = "";
			if (isReady)
			{
				CharacterSheet characterSheet = PlayerManager.ActiveCharacter;

				if (characterSheet == null)
				{
					characterSheet = new CharacterSheet();
				}

				jsonCharSettings = JsonConvert.SerializeObject(characterSheet);
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
