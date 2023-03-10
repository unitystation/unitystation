using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Mirror;
using Core.Accounts;
using Core.Database;
using DatabaseAPI;
using GameConfig;
using Lobby;

namespace Core.Networking
{
	public struct AuthData
	{
		/// <summary>The identifier for the game client.</summary>
		public string ClientId;
		/// <summary>The player's Unitystation account information.</summary>
		public Account Account;
	}

	/// <summary>
	/// Authentication for Mirror clients connecting to servers.
	/// NetworkAuthenticator Documentation: https://mirror-networking.gitbook.io/docs/components/network-authenticators
	/// API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkAuthenticator.html
	/// </summary>
	public class Authenticator : NetworkAuthenticator
	{
		#region Messages

		public struct AuthRequestMessage : NetworkMessage
		{
			public int ClientVersion;
			public string ClientId;
			public string AccountId;
			public string PlayerToken;
			public string LobbyPassword;
		}

		public struct AuthResponseMessage : NetworkMessage
		{
			public ResponseCode Code;
			public string Message;
		}

		public struct ServerClientAuthRequestMessage : NetworkMessage { }
		public struct ServerClientAuthResponseMessage : NetworkMessage { }

		public enum ResponseCode
		{
			Success,
			InvalidClientVersion,
			InvalidAccountDetails,
			AccountValidationError,
			AccountValidationFailed,
			AccountNotVerified,
			RequestPassword,
			IncorrectPassword,
		}

		#endregion

		#region Server

		private readonly Dictionary<string, (DateTime, DateTime)> connectionCooldowns = new();
		private readonly Dictionary<string, DateTime> connectionPasswordRequestTime = new();

		private const float MinCooldown = 1f;

		//30 seconds to give correct password or disconnected
		private const float PasswordRequestTime = 30f;

		private const string PasswordField = "NA";

		public override void OnStartServer()
		{
			NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequest, false);
			NetworkServer.RegisterHandler<ServerClientAuthRequestMessage>(OnServerClientAuthRequest, false);
		}

		public override void OnServerAuthenticate(NetworkConnection conn)
		{
			Logger.LogTrace($"A client not yet authenticated is joining. Address: {conn.address}.", Category.Connections);
		}

		public void OnServerClientAuthRequest(NetworkConnectionToClient conn, ServerClientAuthRequestMessage msg)
		{
			// A special authentication request for the server's local client.

			// Before proceeding, check for connection spam.
			if (IsSpamming(conn))
			{
				ServerReject(conn);
				return;
			}

			if (conn != NetworkServer.localConnection)
			{
				ServerReject(conn); // In the unlikely case of a modified client beating the server's client's request.
				return;
			}

			NetworkServer.UnregisterHandler<ServerClientAuthRequestMessage>(); // Should only be one of these.
			conn.Send(new ServerClientAuthResponseMessage());
			ServerAccept(conn); // A server's local client can automatically authenticate with itself.
		}

		public async void OnAuthRequest(NetworkConnectionToClient conn, AuthRequestMessage msg)
		{
			Logger.LogTrace($"A client is requesting authentication. " +
					$"Address: {conn.address}. Client Version: {msg.ClientVersion}. Account ID: {msg.AccountId}.",
					Category.Connections);

			// Before proceeding, check for connection spam.
			if (IsSpamming(conn))
			{
				ServerReject(conn);
				return;
			}

			Account account;

			// Allow local offline testing
			if (GameData.Instance.OfflineMode)
			{
				account = new Account(); // TODO probably need to populate w/ dummy values
			}
			else
			{
				if (ValidateRequest(conn, msg.AccountId, msg.PlayerToken) == false) return;
				if (ValidatePlayerClient(conn, msg.ClientVersion) == false) return;

				account = await TryGetPlayerAccount(conn, msg.AccountId, msg.PlayerToken);
				if (account == null) return;

				if (ValidatePlayerAccount(conn, account) == false) return;
			}

			if (ValidateLobbyPassword(conn, msg) == false) return;

			// Accept the successful authentication
			conn.authenticationData = new AuthData
			{
				ClientId = msg.ClientId,
				Account = account,
			};

			conn.Send(new AuthResponseMessage
			{
				Code = ResponseCode.Success,
				Message = "Authentication successful.",
			});

			ServerAccept(conn);
		}

		private bool IsSpamming(NetworkConnection conn)
		{
			if (connectionCooldowns.ContainsKey(conn.address) == false)
			{
				connectionCooldowns.Add(conn.address, (DateTime.Now, DateTime.MinValue));
				return false;
			}

			var connSecondsElapsed = (DateTime.Now - connectionCooldowns[conn.address].Item1).TotalSeconds;
			var logSecondsElapsed = (DateTime.Now - connectionCooldowns[conn.address].Item2).TotalSeconds;
			connectionCooldowns[conn.address] = (DateTime.Now, DateTime.MinValue);

			if (connSecondsElapsed < MinCooldown)
			{
				// Cooldown on logging so we don't spam our logs.
				if (logSecondsElapsed > MinCooldown)
				{
					Logger.LogError($"Connection spam alert. Address {conn.address} is trying to spam connections.",
							Category.Connections);
				}

				return true;
			}

			return false;
		}

		private void Update()
		{
			PasswordTimeCheck();
		}

		private void PasswordTimeCheck()
		{
			if(connectionPasswordRequestTime.Count == 0) return;

			//Have to do ToList thx dictionaries
			foreach (var connectionTimer in connectionPasswordRequestTime.ToList())
			{
				var connSecondsElapsed = (DateTime.Now - connectionTimer.Value).TotalSeconds;

				if (connSecondsElapsed < PasswordRequestTime) continue;

				Logger.LogError(
					$"A user ran out of time while sending a password. IP: '{connectionTimer.Key}'.",
					Category.Connections);

				//Try to find the connection if they are still connected
				var connectionFound =
					NetworkServer.connections.FirstOrDefault(
						x => x.Value.address == connectionTimer.Key);

				if (connectionFound.Equals(default(KeyValuePair<int,NetworkConnectionToClient>)) == false)
				{
					//If still connected then disconnect
					DisconnectClient(connectionFound.Value, ResponseCode.IncorrectPassword, "Invalid Password!");
				}

				connectionPasswordRequestTime.Remove(connectionTimer.Key);
			}
		}

		private bool ValidatePlayerClient(NetworkConnection conn, int clientVersion)
		{
			// Check the client version is the same as the server
			if (clientVersion != GameData.BuildNumber)
			{
				Logger.LogTrace($"A client tried to connect with a different client version. Version: {clientVersion}.",
						Category.Connections);
				DisconnectClient(conn, ResponseCode.InvalidClientVersion,
						$"Invalid Client Version! You need version {GameData.BuildNumber}. This can be acquired through the station hub.");
				return false;
			}

			return true;
		}

		private bool ValidateRequest(NetworkConnection conn, string accountId, string playerToken)
		{
			// Must have account ID and player token
			if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(playerToken))
			{
				Logger.LogError(
						"A user tried to connect with an invalid account ID and/or token."
						+ $" Account ID: '{accountId}'. IP: '{conn.address}'.",
						Category.Connections);
				DisconnectClient(conn, ResponseCode.InvalidAccountDetails,
						"Account has invalid token. Try restarting the game and relogging into your account.");

				return false;
			}

			return true;
		}

		private async Task<Account> TryGetPlayerAccount(NetworkConnection conn, string accountId, string playerToken)
		{
			// Validate the provided token against the account details and get the account
			AccountGetResponse accountResponse;
			try
			{
				accountResponse = await AccountServer.VerifyAccount(accountId, playerToken);
			}
			catch (ApiRequestException e)
			{
				Logger.Log(
						$"The API server rejected the verification request for account with "
						+ $"ID '{accountId}' at address '{conn.address}'. Error: {e.Message}",
						Category.Connections);

				// TODO check which particular error message corresponds with the log below, if we have one.
				//Logger.Log("A user tried to authenticate with a bad token. Possible spoof attempt."
				//		+ $" Account ID: '{accountId}'. IP: '{conn.address}'.",
				//		Category.Connections);
				DisconnectClient(conn, ResponseCode.AccountValidationFailed,
						"Account token validation failed. Try restarting the game and relogging into your account.");

				return default;
			}
			catch (ApiHttpException e)
			{
				Logger.LogError($"Http error when validating user account token. Error: {e.Message} - "
						+ $"Account ID: '{accountId}'. IP: '{conn.address}'.",
						Category.Connections);
				DisconnectClient(conn, ResponseCode.AccountValidationError,
						"Server Error: unknown problem encountered when attempting to validate your account token.");

				return default;
			}

			return Account.FromAccountGetResponse(accountResponse);
		}

		private bool ValidatePlayerAccount(NetworkConnection conn, Account account)
		{
			var isVerifiedOnlyServer = true; // TODO add option to server config
			if (isVerifiedOnlyServer && account.IsVerified == false)
			{
				Logger.LogError($"A user tried to authenticate with an unverified account, " +
						$"but this server only accepts verified accounts. Username: '{account.Username}'. IP: '{conn.address}'.",
						Category.Connections);
				DisconnectClient(conn, ResponseCode.AccountNotVerified,
						"Account is not verified. Please verify your account before attempting to log in.");

				return false;
			}

			return true;
		}

		private bool ValidateLobbyPassword(NetworkConnection conn, AuthRequestMessage msg)
		{
			var accountId = msg.AccountId;

			//Check for password if needed to join
			if (string.IsNullOrEmpty(ServerData.ServerConfig.ConnectionPassword)) return true;

			//Client has sent password deny if wrong
			if (msg.LobbyPassword != PasswordField)
			{
				//If password correct let through
				if (msg.LobbyPassword == ServerData.ServerConfig.ConnectionPassword)
				{
					//Remove from password timer
					connectionPasswordRequestTime.Remove(conn.address);
					return true;
				}

				Logger.LogError(
					$"A user tried to connect with an invalid lobby password: {msg.LobbyPassword}."
					+ $" Account ID: '{accountId}'. IP: '{conn.address}'.",
					Category.Connections);

				DisconnectClient(conn, ResponseCode.IncorrectPassword, "Invalid lobby password!");

				return false;
			}

			//Request client to send password
			Logger.Log($"Requesting password from user. Account ID: '{accountId}'. IP: '{conn.address}.",
				Category.Connections);

			conn.Send(new AuthResponseMessage
			{
				Code = ResponseCode.RequestPassword,
				Message = "Password needed to connect!",
			});

			connectionPasswordRequestTime.Add(conn.address, DateTime.Now);

			return false;
		}

		private void DisconnectClient(NetworkConnection connection, ResponseCode reason, string message = "")
				=> StartCoroutine(_DisconnectClient(connection, reason, message));

		private IEnumerator _DisconnectClient(NetworkConnection conn, ResponseCode reason, string message = "")
		{
			var msg = new AuthResponseMessage
			{
				Code = reason,
				Message = message,
			};
			conn.Send(msg);

			// Force disconnect after giving time for message receipt,
			// if the client hasn't already disconnected gracefully
			yield return WaitFor.Seconds(1f);

			ServerReject(conn);
		}

		#endregion

		#region Client

		public override void OnStartClient()
		{
			Logger.LogTrace("Authenticator: client starting, preparing before sending authentication request.", Category.Connections);
			NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponse, false);
			NetworkClient.RegisterHandler<ServerClientAuthResponseMessage>(OnServerClientAuthResponse, false);
		}

		public override async void OnClientAuthenticate()
		{
			if (CustomNetworkManager.IsHeadless || GameData.Instance.testServer)
			{
				NetworkClient.Send(new ServerClientAuthRequestMessage());
				return;
			}

			AuthRequestMessage msg = new()
			{
				ClientVersion = GameData.BuildNumber,
				ClientId = GetPhysicalAddress(),
				AccountId = PlayerManager.Account.Id,
				PlayerToken = await PlayerManager.Account.GetPlayerToken(), // TODO error handling
				LobbyPassword = PasswordField,
			};

			NetworkClient.Send(msg);
		}

		public void OnServerClientAuthResponse(ServerClientAuthResponseMessage msg)
		{
			ClientAccept();
		}

		public void OnAuthResponse(AuthResponseMessage msg)
		{
			if (msg.Code == ResponseCode.RequestPassword)
			{
				LobbyManager.Instance.LobbyPasswordGUI.SetActive(true);
				return;
			}

			LobbyManager.Instance.OrNull()?.LobbyPasswordGUI.SetActive(false);

			if (msg.Code == ResponseCode.Success)
			{
				ClientAccept();
				return;
			}

			Logger.Log($"Disconnecting from server. Reason: {msg.Code}.");
			ClientReject(); // Gracefully handle rejection by disconnecting.

			// Then shut down the client to return to the main menu.
			// If this client is also the host but not headless (i.e. not handled by OnServerClientAuthRequest()),
			// we should handle a disconnect request slightly differently.
			if (CustomNetworkManager.IsServer)
			{
				CustomNetworkManager.Instance.StopHost();
			}
			else
			{
				CustomNetworkManager.Instance.StopClient();
			}

			// Try to use the fancy info panel (Lobby scene only)
			// LobbyManager.UI null check, perhaps it will be possible to join a server while not in the lobby scene.
			// It is also null if the client is also the (non-headless) host, as the server has already switched to a different scene.
			// For a headless host, the server's client does not use this authentication path.
			if (msg.Code == ResponseCode.InvalidClientVersion && LobbyManager.UI != null)
			{
				LobbyManager.UI.ShowInfoPanel(new InfoPanelArgs
				{
					IsError = true,
					Heading = "Wrong Version",
					Text = msg.Message,
					LeftButtonLabel = "Back",
					LeftButtonCallback = LobbyManager.UI.ShowJoinPanel,
				});
			}
			else if (msg.Code == ResponseCode.IncorrectPassword && LobbyManager.UI != null)
			{
				LobbyManager.UI.ShowInfoPanel(new InfoPanelArgs
				{
					IsError = true,
					Heading = "Incorrect Password",
					Text = msg.Message,
					LeftButtonLabel = "Back",
					LeftButtonCallback = LobbyManager.UI.ShowJoinPanel,
				});
			}
			else
			{
				// Otherwise use the basic info panel
				UIManager.InfoWindow.Show(msg.Message, bwoink: false, "Disconnected");
			}
		}

		private string GetPhysicalAddress()
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

		public async void ClientSendLobbyPassword(string password)
		{
			AuthRequestMessage msg = new()
			{
				ClientVersion = GameData.BuildNumber,
				ClientId = GetPhysicalAddress(),
				AccountId = PlayerManager.Account.Id,
				PlayerToken = await PlayerManager.Account.GetPlayerToken(), // TODO error handling
				LobbyPassword = password,
			};

			NetworkClient.Send(msg);
		}

		#endregion
	}
}
