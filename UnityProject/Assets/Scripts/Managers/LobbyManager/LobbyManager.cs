using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Mirror;
using IgnoranceTransport;
using Firebase.Auth;
using Firebase.Extensions;
using Shared.Managers;
using Managers;
using DatabaseAPI;
using UI.CharacterCreator;
using System.Linq;
using Firebase;

namespace Lobby
{
	/// <summary>
	/// Backend scripting for lobby things.
	/// </summary>
	public class LobbyManager : SingletonManager<LobbyManager>
	{
		[SerializeField]
		private CharacterCustomization characterCustomization;

		[SerializeField]
		private GUI_LobbyDialogue lobbyDialogue;

		[field: SerializeField]
		public GUI_ServerPassword LobbyPasswordGUI { get; private set; } = null;

		public static GUI_LobbyDialogue UI => Instance.OrNull()?.lobbyDialogue;

		// Set true by custom network manager if disconnected from server
		public bool WasDisconnected { get; set; } = false;

		public List<ConnectionHistory> ServerJoinHistory { get; private set; }
		private static readonly int MaxJoinHistory = 20; // Aribitrary & more than enough

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
			await FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(email, password)
					.ContinueWithOnMainThread(async task =>
			{
				if (task.IsCanceled)
				{
					Logger.LogWarning($"Sign in canceled.", Category.DatabaseAPI);
					lobbyDialogue.ShowLoginPanel();
				}
				else if (task.IsFaulted)
				{
					var knownCodes = new List<int> { 12 };

					var exception = task.Exception.Flatten().InnerExceptions[0];
					Logger.LogError($"Sign in error: {task.Exception.Message}", Category.DatabaseAPI);

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
					Logger.LogError($"Account validation error: {errorStr}", Category.DatabaseAPI);
					lobbyDialogue.ShowLoginError($"Account validation error. {errorStr}");
				}))
				{
					isLoginSuccess = true;
					PlayerPrefs.SetString(PlayerPrefKeys.AccountEmail, task.Result.Email);
					lobbyDialogue.ShowMainPanel();
				}
			});

			return isLoginSuccess;
		}

		public async Task<bool> TryTokenLogin(string uid, string token)
		{
			lobbyDialogue.ShowLoadingPanel("Welcome back! Signing you in...");

			var refreshToken = new RefreshToken();
			refreshToken.refreshToken = token;
			refreshToken.userID = uid;

			var response = await ServerData.ValidateToken(refreshToken);

			if (response == null)
			{
				lobbyDialogue.ShowLoginError($"Unknown server error. Check your console (F5)");
				return false;
			}

			if (string.IsNullOrEmpty(response.errorMsg) == false)
			{
				Logger.LogError($"Something went wrong with hub token validation: {response.errorMsg}", Category.DatabaseAPI);
				lobbyDialogue.ShowLoginError($"Could not verify your details. {response.errorMsg}");
				return false;
			}

			bool isLoginSuccess = false;
			await FirebaseAuth.DefaultInstance.SignInWithCustomTokenAsync(response.message)
					.ContinueWithOnMainThread(async task =>
			{
				if (task.IsCanceled)
				{
					Logger.LogError("Custom token sign in was canceled.", Category.DatabaseAPI);
					lobbyDialogue.ShowLoginError($"Sign in was cancelled.");
				}
				else if (task.IsFaulted)
				{
					Logger.LogError($"Token login task faulted: {task.Exception}", Category.DatabaseAPI);
					lobbyDialogue.ShowLoginError($"Unexpected error encountered. Check your console (F5)");
				}
				else if (await ServerData.ValidateUser(task.Result, lobbyDialogue.ShowLoginError))
				{
					Logger.Log("Sign in with token successful.", Category.DatabaseAPI);
					isLoginSuccess = true;
				}
			});

			return isLoginSuccess;
		}

		public async Task<bool> TryAutoLogin()
		{
			if (FirebaseAuth.DefaultInstance.CurrentUser == null)
			{
				// We haven't seen this user before.
				lobbyDialogue.ShowAlphaPanel();
				return false;
			}

			var randomGreeting = string.Format(greetings.PickRandom(), FirebaseAuth.DefaultInstance.CurrentUser.DisplayName);
			lobbyDialogue.ShowLoadingPanel($"{randomGreeting}\n\nSigning you in...");

			bool isLoginSuccess = false;
			await FirebaseAuth.DefaultInstance.CurrentUser.TokenAsync(true).ContinueWithOnMainThread(task => {
				if (task.IsCanceled)
				{
					Logger.LogWarning($"Auto sign in cancelled.");
					lobbyDialogue.ShowLoginPanel();
				}
				else if (task.IsFaulted)
				{
					Logger.LogError($"Auto sign in failed: {task.Exception?.Message}");
					lobbyDialogue.ShowLoginError("Unexpected error encountered. Check your console (F5)");
				}
				isLoginSuccess = true;
			});

			if (isLoginSuccess == false) return false;

			if (await ServerData.ValidateUser(FirebaseAuth.DefaultInstance.CurrentUser, lobbyDialogue.ShowLoginError))
			{
				lobbyDialogue.ShowMainPanel();
				return true;
			}

			return false;
		}

		#endregion

		public void ResendEmail()
		{
			if (FirebaseAuth.DefaultInstance.CurrentUser == null)
			{
				Logger.LogError("Cannot resend email for unknown account.", Category.DatabaseAPI);
				return;
			}

			FirebaseAuth.DefaultInstance.CurrentUser.SendEmailVerificationAsync();
			UI.ShowEmailResendPanel(FirebaseAuth.DefaultInstance.CurrentUser.Email);

			FirebaseAuth.DefaultInstance.SignOut();
		}

		public void ShowCharacterEditor()
		{
			characterCustomization.SetActive(true);
		}

		public void JoinServer(string address, ushort port)
		{
			lobbyDialogue.ShowLoadingPanel("Joining game...");

			GameScreenManager.Instance.serverIP = address;

			LoadingScreenManager.LoadFromLobby(() =>
			{
				// Init network client
				Logger.LogFormat("Client trying to connect to {0}:{1}", Category.Connections, address, port);
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

				CustomNetworkManager.Instance.OnClientDisconnected.AddListener(() =>
				{
					lobbyDialogue.ShowInfoPanel(new InfoPanelArgs
					{
						IsError = true,
						Heading = "Join Server Failed",
						Text = "Couldn't connect to the server.",
						LeftButtonLabel = "Back",
						LeftButtonCallback = lobbyDialogue.ShowJoinPanel,
						RightButtonLabel = "Retry",
						RightButtonCallback = () => JoinServer(address, port),
					});
				});

				CustomNetworkManager.Instance.StartClient();
			});
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

			characterCustomization.gameObject.SetActive(false);
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
					characterCustomization.transform.localScale *= 1.25f;
					lobbyDialogue.transform.localScale *= 2.0f;
				}
			}
			else
			{
				if (Screen.height > 720f)
				{
					characterCustomization.transform.localScale *= 0.8f;
					lobbyDialogue.transform.localScale *= 0.8f;
				}
				else
				{
					characterCustomization.transform.localScale *= 0.9f;
					lobbyDialogue.transform.localScale *= 0.9f;
				}
			}
		}

		#region Server History

		private static string HistoryFile => $"{Application.persistentDataPath}{Path.DirectorySeparatorChar}ConnectionHistory.json";

		private void LoadHistoricalServers()
		{
			if (File.Exists(HistoryFile))
			{
				string json = File.ReadAllText(HistoryFile);

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
			if (File.Exists(HistoryFile))
			{
				File.Delete(HistoryFile);
			}
			while (File.Exists(HistoryFile) == false) // TODO isn't this quite dangerous??
			{
				var fs = new FileStream(HistoryFile, FileMode.Create); //To avoid share rule violations
				fs.Dispose();
				File.WriteAllText(HistoryFile, json);
			}
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
