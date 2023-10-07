using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecureStuff;
using Newtonsoft.Json;
using UnityEngine;
using Mirror;
using IgnoranceTransport;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Shared.Managers;
using Managers;
using DatabaseAPI;
using Initialisation;
using Logs;

namespace Lobby
{
	/// <summary>
	/// Backend scripting for lobby things.
	/// </summary>
	public class LobbyManager : SingletonManager<LobbyManager>
	{
		[SerializeField]
		private GameObject characterSettings;

		[SerializeField]
		private GUI_LobbyDialogue lobbyDialogue;

		[field: SerializeField]
		public GUI_ServerPassword LobbyPasswordGUI { get; private set; } = null;

		public static GUI_LobbyDialogue UI => Instance.OrNull()?.lobbyDialogue;

		// Set true by custom network manager if disconnected from server
		public bool WasDisconnected { get; set; } = false;

		public List<ConnectionHistory> ServerJoinHistory { get; private set; }
		private static readonly int MaxJoinHistory = 20; // Aribitrary & more than enough

		private bool cancelTimer = false;

		#region Lifecycle

		public override void Start()
		{
			base.Start();

			LoadHistoricalServers();

			DetermineUIScale();
			UIManager.Display.SetScreenForLobby();
		}

		#endregion

		#region Login

		private static readonly List<string> greetings = new()
		{
			"Hello {0}!",
			"Hi {0}!",
			"Hey {0}!",
			"Hiya {0}!",
			"Heya {0}!",
			"Hello there {0}!",
			"Well hello there {0}!",
			"Hi there {0}!",
			"Hey there {0}!",
			"Greetings {0}!",
			"Welcome {0}!",
			"Welcome back {0}!",
			"Good to see you, {0}!",
			"G'day {0}!",
			"Howdy {0}!",
		};

		public async Task<bool> TryLogin(string email, string password)
		{
			lobbyDialogue.ShowLoadingPanel("Signing in...");

			bool isLoginSuccess = false;
			Loggy.Log("[LobbyManager/TryLogin()] - Executing FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync()");
			await FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(email, password)
					.ContinueWithOnMainThread(async task =>
			{
				if (task.IsCanceled)
				{
					Loggy.LogWarning($"Sign in canceled.");
					lobbyDialogue.ShowLoginPanel();
				}
				else if (task.IsFaulted)
				{
					var knownCodes = new List<int> { 12 };

					var exception = task.Exception.Flatten().InnerExceptions[0];
					Loggy.LogError($"Sign in error: {task.Exception.Message}", Category.DatabaseAPI);

					if (exception is FirebaseException firebaseException && knownCodes.Contains(firebaseException.ErrorCode))
					{
						lobbyDialogue.ShowLoginError($"Account sign in failed. {firebaseException.Message}");
					}
					else
					{
						lobbyDialogue.ShowLoginError($"Unexpected error. Check your console (F5)");
					}
				}
				else if (await ServerData.ValidateUser(task.Result, (errorStr) => {
					Loggy.LogError($"Account validation error: {errorStr}");
					lobbyDialogue.ShowLoginError($"Account validation error. {errorStr}");
				}))
				{
					isLoginSuccess = true;
					PlayerPrefs.SetString(PlayerPrefKeys.AccountEmail, task.Result.Email);
					lobbyDialogue.ShowMainPanel();
				}
			});

			Loggy.Log("[LobbyManager/TryLogin()] - Finished FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync()");

			return isLoginSuccess;
		}

		public async Task<bool> TryTokenLogin(string uid, string token)
		{
			lobbyDialogue.ShowLoadingPanel("Welcome back! Signing you in...");
			LoginTimer();

			var refreshToken = new RefreshToken();
			refreshToken.refreshToken = token;
			refreshToken.userID = uid;

			Loggy.Log("[LobbyManager/TryTokenLogin()] - Executing ServerData.ValidateToken()");
			var response = await ServerData.ValidateToken(refreshToken);
			Loggy.Log("[LobbyManager/TryTokenLogin()] - Finished ServerData.ValidateToken() after awaiting.");

			if (response == null)
			{
				lobbyDialogue.ShowLoginError($"Unknown server error. Check your console (F5)");
				Loggy.Log("[LobbyManager/TryTokenLogin()] - Response is null.");
				cancelTimer = true;
				return false;
			}

			try
			{
				Loggy.Log($"[LobbyManager/TryTokenLogin()] - ResponseData:\n {response.message}\n {response.errorMsg}\n {response.errorCode}.");
			}
			catch (Exception e)
			{
				Loggy.Log($"[LobbyManager/TryTokenLogin()] - Failed Attempting to get some data from response:\n {e.ToString()}.");
			}

			if (string.IsNullOrEmpty(response.errorMsg) == false)
			{
				Loggy.LogError($"Something went wrong with hub token validation: {response.errorMsg}");
				lobbyDialogue.ShowLoginError($"Could not verify your details. {response.errorMsg}");
				cancelTimer = true;
				return false;
			}

			bool isLoginSuccess = false;
			Loggy.Log("[LobbyManager/TryTokenLogin()] - Executing FirebaseAuth.DefaultInstance.SignInWithCustomTokenAsync()");
			await FirebaseAuth.DefaultInstance.SignInWithCustomTokenAsync(response.message)
					.ContinueWithOnMainThread(async task =>
			{
				if (task.IsCanceled)
				{
					Loggy.LogError("Custom token sign in was canceled.");
					lobbyDialogue.ShowLoginError($"Sign in was cancelled.");
				}
				else if (task.IsFaulted)
				{
					Loggy.LogError($"Token login task faulted: {task.Exception}");
					lobbyDialogue.ShowLoginError($"Unexpected error encountered. Check your console (F5)");
				}
				else if (await ServerData.ValidateUser(task.Result, lobbyDialogue.ShowLoginError))
				{
					Loggy.Log("Sign in with token successful.");
					isLoginSuccess = true;
				}
			});
			Loggy.Log("[LobbyManager/TryTokenLogin()] - Finished FirebaseAuth.DefaultInstance.SignInWithCustomTokenAsync() after awaiting it.");
			cancelTimer = true;
			return isLoginSuccess;
		}

		public async Task<bool> TryAutoLogin()
		{
			try
			{
				if (FirebaseAuth.DefaultInstance.CurrentUser == null)
				{
					Loggy.Log("[LobbyManager/TryAutoLogin()] - FirebaseAuth.DefaultInstance.CurrentUser is null. Attempting to send user to first time panel.");
					// We haven't seen this user before.
					lobbyDialogue.ShowAlphaPanel();
					return false;
				}

				var randomGreeting = string.Format(greetings.PickRandom(), FirebaseAuth.DefaultInstance.CurrentUser.DisplayName);
				lobbyDialogue.ShowLoadingPanel($"{randomGreeting}\n\nSigning you in...");
				LoginTimer();
				bool isLoginSuccess = false;
				Loggy.Log("[LobbyManager/TryAutoLogin()] - Executing  FirebaseAuth.DefaultInstance.CurrentUser.TokenAsync(true).ContinueWithOnMainThread().");
				await FirebaseAuth.DefaultInstance.CurrentUser.TokenAsync(true).ContinueWithOnMainThread(task => {
					if (task.IsCanceled)
					{
						Loggy.LogWarning($"Auto sign in cancelled.");
						LoadManager.DoInMainThread(() => { lobbyDialogue.ShowLoginPanel(); });

					}
					else if (task.IsFaulted)
					{
						Loggy.LogError($"Auto sign in failed: {task.Exception?.Message}");
						lobbyDialogue.ShowLoginError("Unexpected error encountered. Check your console (F5)");
						LoadManager.DoInMainThread(() => { lobbyDialogue.ShowLoginPanel(); });
					}
					isLoginSuccess = true;
				});
				Loggy.Log("[LobbyManager/TryAutoLogin()] - Finished awaited FirebaseAuth.DefaultInstance.CurrentUser.TokenAsync(true).ContinueWithOnMainThread().");

				if (isLoginSuccess == false)
				{
					Loggy.Log("[LobbyManager/TryAutoLogin()] - isLoginSuccess is false.");
					LoadManager.DoInMainThread(() => { lobbyDialogue.ShowLoginPanel(); });
					return false;
				}
				cancelTimer = true;

				Loggy.Log("[LobbyManager/TryAutoLogin()] - Executing awaited ServerData.ValidateUser(FirebaseAuth.DefaultInstance.CurrentUser, lobbyDialogue.ShowLoginError)");

				var longRunningTask = ServerData.ValidateUser(FirebaseAuth.DefaultInstance.CurrentUser, lobbyDialogue.ShowLoginError);
				var timeout = TimeSpan.FromSeconds(5);

				// Use Task.WhenAny to wait for either the long-running task to complete or the timeout to occur
				var completedTask = await Task.WhenAny(longRunningTask, Task.Delay(timeout));

				// Check which task completed
				if (completedTask == longRunningTask)
				{
					LoadManager.DoInMainThread(() => { lobbyDialogue.ShowMainPanel(); });
					Loggy.Log("[LobbyManager/TryAutoLogin()] - Finished awaited ServerData.ValidateUser(~~~) and showing main panel.");
					return false;
				}
				else
				{
					Loggy.Log("[LobbyManager/TryAutoLogin()] - Finished awaited ServerData.ValidateUser(~~~) with false result.");
					LoadManager.DoInMainThread(() => { lobbyDialogue.ShowLoginPanel(); });
					return true;
				}
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
				LoadManager.DoInMainThread(() => { lobbyDialogue.ShowLoginPanel(); });

				return false;
			}

		}

		#endregion

		public void ResendEmail()
		{
			if (FirebaseAuth.DefaultInstance.CurrentUser == null)
			{
				Loggy.LogError("Cannot resend email for unknown account.");
				return;
			}

			FirebaseAuth.DefaultInstance.CurrentUser.SendEmailVerificationAsync();
			UI.ShowEmailResendPanel(FirebaseAuth.DefaultInstance.CurrentUser.Email);

			FirebaseAuth.DefaultInstance.SignOut();
		}

		public void ShowCharacterEditor()
		{
			characterSettings.SetActive(true);
		}

		public void JoinServer(string address, ushort port)
		{
			lobbyDialogue.ShowLoadingPanel("Joining game...");
			GameScreenManager.Instance.serverIP = address;

			LoadingScreenManager.LoadFromLobby(() =>
			{
				// Init network client
				Loggy.LogFormat("Client trying to connect to {0}:{1}", Category.Connections, address, port);
				LogServerConnHistory(address, port);

				CustomNetworkManager.Instance.networkAddress = address;

				if (CustomNetworkManager.Instance.TryGetComponent<TelepathyTransport>(out var telepathy))
				{
					telepathy.port = port;
				}

				if (CustomNetworkManager.Instance.TryGetComponent<Ignorance>(out var ignorance))
				{
					ignorance.port = port;
				}

				CustomNetworkManager.Instance.OnClientDisconnected.AddListener(GetOnClientDisconnected(address, port));

				CustomNetworkManager.Instance.StartClient();
			});
		}

		public static UnityEngine.Events.UnityAction GetOnClientDisconnected(string address, ushort port)
		{
			return () =>
			{
				if (LobbyManager.Instance != null)
				{
					LobbyManager.Instance.lobbyDialogue.ShowInfoPanel(new InfoPanelArgs
					{
						IsError = true,
						Heading = "Join Server Failed",
						Text = "Couldn't connect to the server.",
						LeftButtonLabel = "Back",
						LeftButtonCallback = LobbyManager.Instance.lobbyDialogue.ShowJoinPanel,
						RightButtonLabel = "Retry",
						RightButtonCallback = () => LobbyManager.Instance.JoinServer(address, port),
					});
				}
			};
		}

			public void HostServer()
		{
			lobbyDialogue.ShowLoadingPanel("Hosting a game...");
			LoadingScreenManager.LoadFromLobby(CustomNetworkManager.Instance.StartHost);
		}

		public void Logout()
		{
			ServerData.Auth.SignOut();
			PlayerPrefs.DeleteKey(PlayerPrefKeys.AccountUsername);
			PlayerPrefs.DeleteKey(PlayerPrefKeys.AccountToken);
			PlayerPrefs.Save();

			characterSettings.SetActive(false);
			lobbyDialogue.gameObject.SetActive(true);
			lobbyDialogue.ShowLoginPanel();
		}

		public void Quit()
		{
			// TODO: doesn't work in editor. Not a big deal.
			Application.Quit();
		}

		private void DetermineUIScale()
		{
			if (Application.isMobilePlatform)
			{
				if (!UIManager.IsTablet)
				{
					characterSettings.transform.localScale *= 1.25f;
					lobbyDialogue.transform.localScale *= 2.0f;
				}
			}
			else
			{
				if (Screen.height > 720f)
				{
					characterSettings.transform.localScale *= 0.8f;
					lobbyDialogue.transform.localScale *= 0.8f;
				}
				else
				{
					characterSettings.transform.localScale *= 0.9f;
					lobbyDialogue.transform.localScale *= 0.9f;
				}
			}
		}

		private async void LoginTimer()
		{
			await Task.Delay(14000);
			if (cancelTimer) return;
			lobbyDialogue.ShowLoadingPanel("This is taking longer than it should..\n\n If it continues, try disabling your VPNs and installing the game in full English path.");
			await Task.Delay(30500);
			if (cancelTimer) return;
			lobbyDialogue.ShowLoginError($"Unexpected error. Check your console (F5)");
		}

		#region Server History

		private static string HistoryFile => "ConnectionHistory.json";

		private void LoadHistoricalServers()
		{
			if (AccessFile.Exists(HistoryFile, userPersistent: true))
			{
				string json = AccessFile.Load(HistoryFile, userPersistent: true);

				ServerJoinHistory = JsonConvert.DeserializeObject<List<ConnectionHistory>>(json)?.Distinct()?.ToList();
			}

			ServerJoinHistory ??= new List<ConnectionHistory>();
		}

		public void LogServerConnHistory(string address, ushort port)
		{
			var entry = new ConnectionHistory
			{
				Address = address,
				Port = port,
			};

			var i = ServerJoinHistory.IndexOf(entry);
			if (i != -1)
			{
				// Move to start of list
				ServerJoinHistory.RemoveAt(i);
			}

			ServerJoinHistory.Insert(0, entry);

			if (ServerJoinHistory.Count >= MaxJoinHistory)
			{
				// Remove older entries
				ServerJoinHistory.RemoveRange(MaxJoinHistory, ServerJoinHistory.Count - MaxJoinHistory);
			}

			SaveServerHistoryFile();
		}

		public void ConnectToLastServer()
		{
			var entry = ServerJoinHistory.FirstOrDefault();

			JoinServer(entry.Address, entry.Port);
		}

		private void SaveServerHistoryFile()
		{
			string json = JsonConvert.SerializeObject(ServerJoinHistory);

			AccessFile.Save(HistoryFile, json, userPersistent: true);
		}

		#endregion
	}

	public struct ConnectionHistory
	{
		[JsonProperty("IP")]
		public string Address { get; set; }

		[JsonProperty("Port")]
		public ushort Port { get; set; }

		public override string ToString()
		{
			return $"{Address}:{Port}";
		}

		public override bool Equals(object obj)
		{
			if (obj is ConnectionHistory other) {
				return other.Address == Address && other.Port == Port;
			}

			return false;
		}

		public override int GetHashCode()
		{
			return ToString().GetHashCode(); // lazy
		}
	}
}
