using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using UnityEngine;
using Mirror;
using DatabaseAPI;

namespace Core.Networking
{
	public struct AuthData
	{
		/// <summary>The identifier for the game client.</summary>
		public string ClientId;
		/// <summary>The identifier for the player's Unitystation account.</summary>
		public string AccountId;
		/// <summary>Human-friendly public-facing username of the player's account.</summary>
		public string Username;
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
			public string Username;
			public string Token;
		}

		public struct AuthResponseMessage : NetworkMessage
		{
			public ResponseCode Code;
			public string Message;
		}

		public enum ResponseCode
		{
			Success,
			InvalidClientVersion,
			InvalidAccountDetails,
			AccountValidationError,
			AccountValidationFailed,
		}

		#endregion

		#region Server

		private readonly Dictionary<string, (DateTime, DateTime)> connectionCooldowns = new();
		private readonly float minCooldown = 1f;

		public override void OnStartServer()
		{
			NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage, false);
		}

		public override void OnServerAuthenticate(NetworkConnection conn)
		{
			Logger.LogTrace($"A client not yet authenticated is joining. Address: {conn.address}.", Category.Connections);
		}

		public async void OnAuthRequestMessage(NetworkConnectionToClient conn, AuthRequestMessage msg)
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

			if (ValidatePlayerClient(conn, msg.ClientVersion) == false) return;
			if (await ValidatePlayerAccount(conn, msg.AccountId, msg.Token) == false) return;

			// Accept the successful authentication
			conn.authenticationData = new AuthData
			{
				ClientId = msg.ClientId,
				AccountId = msg.AccountId,
				Username = msg.Username,
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

			if (connSecondsElapsed < minCooldown)
			{
				// Cooldown on logging so we don't spam our logs.
				if (logSecondsElapsed > minCooldown)
				{
					Logger.Log($"Connection spam alert. Address {conn.address} is trying to spam connections.",
							Category.Connections);
				}

				return true;
			}

			return false;
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

		private async Task<bool> ValidatePlayerAccount(NetworkConnection conn, string accountId, string accountToken)
		{
			// Allow local offline testing
			if (GameData.Instance.OfflineMode) return true;

			// Must have account ID and token
			if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(accountToken))
			{
				Logger.Log(
						"A user tried to connect with an invalid account ID and/or token."
						+ $" Account ID: '{accountId}'. IP: '{conn.address}'.",
						Category.Connections);
				DisconnectClient(conn, ResponseCode.InvalidAccountDetails,
						"Account has invalid token. Try restarting the game and relogging into your account.");

				return false;
			}

			// Validate the provided token against the account details
			var refresh = new RefreshToken { userID = accountId, refreshToken = accountToken };
			var response = await ServerData.ValidateToken(refresh, true);
			if (response == null)
			{
				Logger.LogError($"Server error when validating user account token."
						+ $" Account ID: '{accountId}'. IP: '{conn.address}'.",
						Category.Connections);
				DisconnectClient(conn, ResponseCode.AccountValidationError,
						"Server Error: unknown problem encountered when attempting to validate your account token.");

				return false;
			}

			if (response.errorCode == 1)
			{
				Logger.Log("A user tried to authenticate with a bad token. Possible spoof attempt."
						+ $" Account ID: '{accountId}'. IP: '{conn.address}'.",
						Category.Connections);
				DisconnectClient(conn, ResponseCode.AccountValidationFailed,
						"Account token validation failed. Try restarting the game and relogging into your account.");

				return false;
			}

			return true;
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
			yield return new WaitForSeconds(1f);

			ServerReject(conn);
		}

		#endregion

		#region Client

		public override void OnStartClient()
		{
			NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
		}

		public override void OnClientAuthenticate()
		{
			AuthRequestMessage msg = new()
			{
				ClientVersion = GameData.BuildNumber,
				ClientId = GetPhysicalAddress(),
				AccountId = ServerData.UserID,
				Username = ServerData.Auth?.CurrentUser?.DisplayName,
				Token = ServerData.IdToken,
			};

			NetworkClient.Send(msg);
		}

		public void OnAuthResponseMessage(AuthResponseMessage msg)
		{
			if (msg.Code == ResponseCode.Success)
			{
				ClientAccept();
				return;
			}

			Logger.Log($"Disconnected from server. Reason: {msg.Code}.");
			UIManager.InfoWindow.Show(msg.Message, bwoink: false, "Disconnected");
			ClientReject(); // Gracefully handle rejection by disconnecting.
			CustomNetworkManager.Instance.StopClient(); // Then shut down the client to return to the main menu.
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

		#endregion
	}
}
