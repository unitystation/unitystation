using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecureStuff;
using Newtonsoft.Json;
using UnityEngine;
using Mirror;
using IgnoranceTransport;
using Core.Accounts;
using Core.Database;
using UI.CharacterCreator;
using Managers;
using Shared.Managers;
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
		private static readonly int MaxJoinHistory = 20; // Arbitrary & more than enough

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

		public async Task<bool> TryLogin(string emailAddress, string password)
		{
			lobbyDialogue.ShowLoadingPanel("Signing in...");

			PlayerPrefs.SetString(PlayerPrefKeys.AccountEmail, emailAddress);

			await PlayerManager.Account.Login(emailAddress, password).Then(task =>
			{
				HandleLoginTask(task);
			});

			return PlayerManager.Account.IsAvailable;

			// bool isLoginSuccess = false;
			// Loggy.Log("[LobbyManager/TryLogin()] - Executing FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync()");
			// await FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(email, password)
			// 		.ContinueWithOnMainThread(async task =>
			// {
			// 	if (task.IsCanceled)
			// 	{
			// 		Loggy.LogWarning($"Sign in canceled.");
			// 		lobbyDialogue.ShowLoginPanel();
			// 	}
			// 	else if (task.IsFaulted)
			// 	{
			// 		var knownCodes = new List<int> { 12 };
			//
			// 		var exception = task.Exception.Flatten().InnerExceptions[0];
			// 		Loggy.LogError($"Sign in error: {task.Exception.Message}", Category.DatabaseAPI);
			//
			// 		if (exception is FirebaseException firebaseException && knownCodes.Contains(firebaseException.ErrorCode))
			// 		{
			// 			lobbyDialogue.ShowLoginError($"Account sign in failed. {firebaseException.Message}");
			// 		}
			// 		else
			// 		{
			// 			lobbyDialogue.ShowLoginError($"Unexpected error. Check your console (F5)");
			// 		}
			// 	}
			// 	else if (await ServerData.ValidateUser(task.Result, (errorStr) => {
			// 		Loggy.LogError($"Account validation error: {errorStr}");
			// 		lobbyDialogue.ShowLoginError($"Account validation error. {errorStr}");
			// 	}))
			// 	{
			// 		isLoginSuccess = true;
			// 		PlayerPrefs.SetString(PlayerPrefKeys.AccountEmail, task.Result.Email);
			// 		lobbyDialogue.ShowMainPanel();
			// 	}
			// });
			//
			// Loggy.Log("[LobbyManager/TryLogin()] - Finished FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync()");
			//
			// return isLoginSuccess;
		}

		public async Task<bool> TryTokenLogin(string token)
		{

			var username = PlayerPrefs.GetString(PlayerPrefKeys.AccountName);

			lobbyDialogue.ShowLoadingPanel("Welcome back! Signing you in...");
			LoginTimer();

			var randomGreeting = string.Format(greetings.PickRandom(), username);
			lobbyDialogue.ShowLoadingPanel($"{randomGreeting}\n\nSigning you in...");


			// It's weird that we use the PlayerManager.Account to log in, but then we go and update that same object w/ the result...
			var loginTask = PlayerManager.Account.Login(token);
			try
			{
				await loginTask;
			}
			finally
			{
				// Let HandleLoginTask handle any exceptions.
				HandleLoginTask(loginTask);
			}

			return PlayerManager.Account.IsAvailable;
			//
			// Loggy.Log("[LobbyManager/TryTokenLogin()] - Executing ServerData.ValidateToken()");
			// var response = await ServerData.ValidateToken(refreshToken);
			// Loggy.Log("[LobbyManager/TryTokenLogin()] - Finished ServerData.ValidateToken() after awaiting.");
			//
			// if (response == null)
			// {
			// 	lobbyDialogue.ShowLoginError($"Unknown server error. Check your console (F5)");
			// 	Loggy.Log("[LobbyManager/TryTokenLogin()] - Response is null.");
			// 	cancelTimer = true;
			// 	return false;
			// }
			//
			// try
			// {
			// 	Loggy.Log($"[LobbyManager/TryTokenLogin()] - ResponseData:\n {response.message}\n {response.errorMsg}\n {response.errorCode}.");
			// }
			// catch (Exception e)
			// {
			// 	Loggy.Log($"[LobbyManager/TryTokenLogin()] - Failed Attempting to get some data from response:\n {e.ToString()}.");
			// }
			//
			// if (string.IsNullOrEmpty(response.errorMsg) == false)
			// {
			// 	Loggy.LogError($"Something went wrong with hub token validation: {response.errorMsg}");
			// 	lobbyDialogue.ShowLoginError($"Could not verify your details. {response.errorMsg}");
			// 	cancelTimer = true;
			// 	return false;
			// }
			//
			// bool isLoginSuccess = false;
			// Loggy.Log("[LobbyManager/TryTokenLogin()] - Executing FirebaseAuth.DefaultInstance.SignInWithCustomTokenAsync()");
			// await FirebaseAuth.DefaultInstance.SignInWithCustomTokenAsync(response.message)
			// 		.ContinueWithOnMainThread(async task =>
			// {
			// 	if (task.IsCanceled)
			// 	{
			// 		Loggy.LogError("Custom token sign in was canceled.");
			// 		lobbyDialogue.ShowLoginError($"Sign in was cancelled.");
			// 	}
			// 	else if (task.IsFaulted)
			// 	{
			// 		Loggy.LogError($"Token login task faulted: {task.Exception}");
			// 		lobbyDialogue.ShowLoginError($"Unexpected error encountered. Check your console (F5)");
			// 	}
			// 	else if (await ServerData.ValidateUser(task.Result, lobbyDialogue.ShowLoginError))
			// 	{
			// 		Loggy.Log("Sign in with token successful.");
			// 		isLoginSuccess = true;
			// 	}
			// });
			// Loggy.Log("[LobbyManager/TryTokenLogin()] - Finished FirebaseAuth.DefaultInstance.SignInWithCustomTokenAsync() after awaiting it.");
			// cancelTimer = true;
			// return isLoginSuccess;

		}

		public async Task<bool> TryAutoLogin(bool autoJoin)
		{
			Loggy.Log("Attempting automatic login by token...");

			if (PlayerPrefs.GetInt(PlayerPrefKeys.AccountAutoLogin) == 1 && PlayerPrefs.HasKey(PlayerPrefKeys.AccountToken))
			{
				return await TryTokenLogin(PlayerPrefs.GetString(PlayerPrefKeys.AccountToken));
			}

			Loggy.Log("Couldn't log in via PlayerPrefs token: automatic login not enabled or no token.");

			return false;
		}


		private void HandleLoginTask(Task<Account> task)
		{
			if (task.IsCanceled)
			{
				Loggy.LogWarning("Login cancelled.");
				lobbyDialogue.ShowLoginPanel();
			}
			else if (task.IsFaulted)
			{
				if (task.Exception?.GetBaseException() is ApiRequestException apiException)
				{
					lobbyDialogue.ShowLoginError(apiException.Message);
				}
				else
				{
					Loggy.LogError("Fault while logging in.");
					task.LogFaultedTask();
					lobbyDialogue.ShowLoginError("Cannot communicate with the account server. Check your console (F5).");
				}
			}
			else
			{
				var account = task.Result;
				PlayerManager.Instance.SetAccount(account);

				lobbyDialogue.ShowMainPanel();
			}
		}

		public void SetAutoLogin(bool shouldAutoLogin)
		{
			PlayerPrefs.SetInt(PlayerPrefKeys.AccountAutoLogin, shouldAutoLogin ? 1 : 0);
		}

		#endregion

		public async Task<bool> CreateAccount(string username, string email, string accountIdentifier, string password) // TODO accountIdentifier, order
		{

			var isSuccess = false;
			await PlayerManager.Account.Register(accountIdentifier , email, username, password).Then(task =>
			{
				if (task.IsCanceled)
				{
					Loggy.LogWarning("Account creation cancelled.");
					lobbyDialogue.ShowAccountCreatePanel();
				}
				else if (task.IsFaulted)
				{
					var message = "Couldn't create your account. Check the console (F5).";

					if (task.Exception.InnerException is ApiRequestException apiException)
					{
						message = $"Couldn't create your account. {apiException.Message}";
					}
					else
					{
						task.LogFaultedTask();
					}

					lobbyDialogue.ShowInfoPanel(new InfoPanelArgs
					{
						Heading = "Account Creation Failed",
						Text = message,
						IsError = true,
						LeftButtonLabel = "Back",
						LeftButtonCallback = lobbyDialogue.ShowAccountCreatePanel, // TODo move here?>
						RightButtonLabel = "Retry",
						RightButtonCallback = () => _ = CreateAccount(username, email, accountIdentifier, password),
					});
				}
				else
				{
					isSuccess = true;
					lobbyDialogue.ShowInfoPanel(new InfoPanelArgs
					{
						Heading = "Account Created",
						Text = $"Success! An email will be sent to\n<b>{email}</b>\n\n" +
								$"Please click the link in the email to verify your account before signing in.",
						LeftButtonLabel = "Back",
						LeftButtonCallback = lobbyDialogue.ShowLoginPanel,
						RightButtonLabel = "Resend Email",
						RightButtonCallback = ResendVerifyEmail,
					});
				}
			});

			if (isSuccess)
			{
				PlayerPrefs.SetString(PlayerPrefKeys.AccountEmail, email);
				PlayerPrefs.Save();
			}

			return isSuccess;
		}

		public void ResendVerifyEmail()
		{
			var email = PlayerPrefs.GetString(PlayerPrefKeys.AccountEmail);
			PlayerManager.Account.RequestNewVerifyEmail().Then(task =>
			{
				if (task.IsCanceled)
				{
					Loggy.Log("Request for a new verification email cancelled.");
					lobbyDialogue.ShowLoginPanel();
				}
				else if (task.IsFaulted)
				{
					Loggy.LogError($"Couldn't request a new verification email. {task.Exception.GetBaseException()}");
					lobbyDialogue.ShowInfoPanel(new InfoPanelArgs
					{
						Heading = "Account Creation Failed",
						Text = $"Failed to request a new verification email for \n<b>{email}<b>.\nCheck your console (F5)",
						IsError = true,
						LeftButtonLabel = "Back",
						LeftButtonCallback = lobbyDialogue.ShowLoginPanel,
						RightButtonLabel = "Retry",
						RightButtonCallback = ResendVerifyEmail,
					});
				}
				else
				{
					lobbyDialogue.ShowInfoPanel(new InfoPanelArgs
					{
						Heading = "Resend Verification Email",
						Text = $"A new verification email will be sent to \n<b>{email}</b>",
						IsError = true,
						LeftButtonLabel = "Back",
						LeftButtonCallback = lobbyDialogue.ShowLoginPanel,
					});
				}
			});

			PlayerManager.Account.Logout(); // TODO why here
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
			// Only clear the cached email address if deliberately logged out,
			// so login email form can be prepopulated if user restarts game.
			PlayerPrefs.DeleteKey(PlayerPrefKeys.AccountEmail);
			PlayerPrefs.DeleteKey(PlayerPrefKeys.AccountAutoLogin);

			PlayerManager.Account.Logout();

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
			LoadManager.DoInMainThread(() =>
			{
				lobbyDialogue.ShowLoadingPanel("This is taking longer than it should..\n\n If it continues, try disabling your VPNs and installing the game in full English path.");
			} );

			await Task.Delay(30500);
			if (cancelTimer) return;
			LoadManager.DoInMainThread(() =>
			{
				lobbyDialogue.ShowLoginError($"Unexpected error. Check your console (F5)");
			} );

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
