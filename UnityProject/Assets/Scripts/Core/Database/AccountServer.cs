using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Systems.Character;

namespace Core.Database
{
	internal static class Endpoints
	{
		//public static Uri asdf = new Uri("") { };
	}


	public static class AccountServer
	{
		public static string Host = "usapi.deb.local"; // TODO: expose to config
		public static UriBuilder UriBuilder = new("http", Host);

		public static Uri GetUri(string endpoint)
		{
			UriBuilder.Path = $"/api/accounts/{endpoint}";
			return UriBuilder.Uri;
		}

		public static async Task<AccountRegisterResponse> Register(
				string account_identifier, string emailAddress, string username, string password)
		{
			var requestBody = new AccountRegister
			{
				email = emailAddress,
				account_identifier = account_identifier,
				username = username,
				password = password,
			};

			return await ApiServer.Post<AccountRegisterResponse>(GetUri("register"), requestBody);
		}

		public static async Task<AccountLoginResponse> Login(string emailAddress, string password)
		{
			var requestBody = new AccountLoginCredentials
			{
				email = emailAddress,
				password = password,
			};

			var response = await ApiServer.Post<AccountLoginResponse>(GetUri("login-credentials"), requestBody);

			return response;
		}

		public static async Task<AccountTokenLoginResponse> Login(string token)
		{
			var requestBody = new AccountLoginToken
			{
				Token = token,
			};

			var response = await ApiServer.Post<AccountTokenLoginResponse>(GetUri("login-token"), requestBody);

			return response;
		}

		public static async Task<JsonObject> Logout(string token, bool destroyAllSessions = false) // TODO: but no response?
		{
			var requestBody = new AccountLogout
			{
				Token = token,
			};

			var response = await ApiServer.Post<JsonObject>(GetUri(destroyAllSessions ? "logoutall" : "logout"), requestBody);

			return response;
		}

		public static async Task<AccountUpdateResponse> UpdateAccount(string token, string emailAddress, string username, string password)
		{
			var requestBody = new AccountUpdate
			{
				Token = token,
				email = emailAddress,
				username = username,
				password = password,
			};

			var response = await ApiServer.Post<AccountUpdateResponse>(GetUri("update-account"), requestBody);

			return response;
		}

		public static async Task<JsonObject> UpdateCharacters(string token, Dictionary<string, Dictionary<string, CharacterSheet>> characters)
		{
			var requestBody = new AccountUpdateCharacters
			{
				Token = token,
				characters = characters,
			};

			var response = await ApiServer.Post<JsonObject>(GetUri("update-characters"), requestBody);

			return response;
		}

		public static async Task<AccountGetResponse> GetAccountInfo(string accountIdentifier)
		{
			var response = await ApiServer.Get<AccountGetResponse>(GetUri($"users/{accountIdentifier}"));

			return response;
		}

		// Verification token is what is used by the game server to validate account on the account server.
		// This is a separate token to account token, which is to authorize account-related operations
		public static async Task<AccountVerificationTokenResponse> GetVerificationToken(string accountToken)
		{
			var response = await ApiServer.Get<AccountVerificationTokenResponse>(GetUri("request-verification-token"), accountToken);

			return response;
		}

		public static async Task<AccountGetResponse> VerifyAccount(string accountId, string token)
		{
			var requestBody = new AccountValidate
			{
				account_identifier = accountId,
				verification_token = token,
			};

			var response = await ApiServer.Post<AccountGetResponse>(GetUri("verify-account"), requestBody);

			return response;
		}
	}
}

// TODO for test case where is success 200 but object returned is unexpected
