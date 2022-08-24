using UnityEngine;
using UI.CharacterCreator;
using Shared.Managers;
using System.Threading.Tasks;
using Managers;
using IgnoranceTransport;
using Mirror;
using DatabaseAPI;
using Firebase.Auth;
using Firebase.Extensions;
using System;

namespace Lobby
{
	/// <summary>
	/// Backend scripting for lobby things.
	/// </summary>
	public class LobbyManager : SingletonManager<LobbyManager>
	{
		public CharacterCustomization characterCustomization;

		public GUI_LobbyDialogue lobbyDialogue;

		#region Lifecycle

		public override void Start()
		{
			base.Start();

			DetermineUIScale();
			UIManager.Display.SetScreenForLobby();
		}

		#endregion

		#region Login

		public async Task<bool> TryLogin(string email, string password)
		{
			lobbyDialogue.ShowLoadingPanel("Logging in...");

			bool isLoginSuccess = false;
			await FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(email, password)
				.ContinueWithOnMainThread(async task =>
				{
					if (task.IsCanceled)
					{
						Logger.LogError($"Sign in error: {task.Exception.Message}", Category.DatabaseAPI);
						lobbyDialogue.LoginError("Sign in canceled.");
						return;
					}

					if (task.IsFaulted)
					{
						Logger.LogError($"Sign in error: {task.Exception.Message}", Category.DatabaseAPI);
						lobbyDialogue.LoginError($"Sign in error. Check the console (F5)");
						return;
					}

					await ServerData.ValidateUser(task.Result, (_) =>
					{
						isLoginSuccess = true;
						SaveAccountPrefs(task.Result.Email, String.Empty); // TODO ???? token sort it
						lobbyDialogue.LoginSuccess();
					}, (errorStr) =>
					{
						Logger.LogError($"Account validation error: {errorStr}", Category.DatabaseAPI);
						lobbyDialogue.LoginError($"Account validation error. {errorStr}");
					});
				});

			return isLoginSuccess;
		}

		public async Task<bool> TryTokenLogin(string uid, string token)
		{
			lobbyDialogue.ShowLoadingPanel("Logging you in...");

			var refreshToken = new RefreshToken();
			refreshToken.refreshToken = token;
			refreshToken.userID = uid;

			var response = await ServerData.ValidateToken(refreshToken);

			if (response == null)
			{
				lobbyDialogue.LoginError(
					$"Unknown server error. Please check your logs for more information by press F5");
				return false;
			}

			if (string.IsNullOrEmpty(response.errorMsg) == false)
			{
				Logger.LogError($"Something went wrong with hub token validation: {response.errorMsg}", Category.DatabaseAPI);
				lobbyDialogue.LoginError($"Could not verify your details {response.errorMsg}");
				return false;
			}

			bool isLoginSuccess = false;
			await FirebaseAuth.DefaultInstance.SignInWithCustomTokenAsync(response.message).ContinueWithOnMainThread(
				async task =>
				{
					if (task.IsCanceled)
					{
						Logger.LogError("Custom token sign in was canceled.", Category.DatabaseAPI);
						lobbyDialogue.LoginError($"Sign in was cancelled");
						return;
					}

					if (task.IsFaulted)
					{
						Logger.LogError("Task Faulted: " + task.Exception, Category.DatabaseAPI);
						lobbyDialogue.LoginError($"Task Faulted: " + task.Exception);
						return;
					}

					var success = await ServerData.ValidateUser(task.Result, null, null);

					if (success)
					{
						Logger.Log("Signed in successfully with valid token", Category.DatabaseAPI);
						isLoginSuccess = true;
					}
				});

			return isLoginSuccess;
		}

		public async Task<bool> TryAutoLogin()
		{
			await Task.Delay(TimeSpan.FromSeconds(0.1));

			bool isLoginSuccess = false;
			if (FirebaseAuth.DefaultInstance.CurrentUser != null)
			{
				lobbyDialogue.ShowLoadingPanel($"Loading user profile for {FirebaseAuth.DefaultInstance.CurrentUser.DisplayName}");

				await FirebaseAuth.DefaultInstance.CurrentUser.TokenAsync(true).ContinueWithOnMainThread(task => {
					if (task.IsCanceled || task.IsFaulted)
					{
						lobbyDialogue.LoginError(task.Exception?.Message);
					}
				});

				await ServerData.ValidateUser(FirebaseAuth.DefaultInstance.CurrentUser, (_) => {
					isLoginSuccess = true;
				}, lobbyDialogue.LoginError);
			}

			return isLoginSuccess;
		}

		private void SaveAccountPrefs(string email, string token)
		{
			PlayerPrefs.SetString(PlayerPrefKeys.AccountEmail, email);

			if (lobbyDialogue.LoginUIScript.IsAutoLoginEnabled)
			{
				PlayerPrefs.SetString(PlayerPrefKeys.AccountToken, token);
			}
			else
			{
				PlayerPrefs.DeleteKey(PlayerPrefKeys.AccountToken);
			}

			PlayerPrefs.Save();
		}

		#endregion

		public void JoinServer(string address, ushort port)
		{
			lobbyDialogue.ShowLoadingPanel("Joining game...");

			GameScreenManager.Instance.serverIP = address;

			LoadingScreenManager.LoadFromLobby(() =>
			{
				// Init network client
				Logger.LogFormat("Client trying to connect to {0}:{1}", Category.Connections, address, port);
				lobbyDialogue.LogConnectionToHistory(address, port);

				CustomNetworkManager.Instance.networkAddress = address;

				if (CustomNetworkManager.Instance.TryGetComponent<TelepathyTransport>(out var telepathy))
				{
					telepathy.port = port;
				}

				if (CustomNetworkManager.Instance.TryGetComponent<Ignorance>(out var ignorance))
				{
					ignorance.port = port;
				}

				CustomNetworkManager.Instance.StartClient();
			});
		}

		public void HostServer()
		{
			lobbyDialogue.ShowLoadingPanel("Hosting a game....");
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
			// TODO: doesn't work in editor.
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
	}
}
