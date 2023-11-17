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
		public static string Host = "dev-api.unitystation.org"; // TODO: expose to config
		public static UriBuilder UriBuilder = new("https", Host);

		public static Uri GetUri(string endpoint, string queries = null, bool NOaccounts = false)
		{
			if (NOaccounts == false)
			{
				UriBuilder.Path = $"/accounts/{endpoint}";
			}
			else
			{
				UriBuilder.Path = $"/{endpoint}";
			}

			if (string.IsNullOrEmpty(queries) == false)
			{
				UriBuilder.Query = queries;
			}

			return UriBuilder.Uri;
		}

		public static async Task<AccountRegisterResponse> Register(
				string unique_identifier, string emailAddress, string username, string password)
		{
			var requestBody = new AccountRegister
			{
				email = emailAddress,
				unique_identifier = unique_identifier,
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


		public static async Task<AccountGetResponse> GetAccountInfo(string accountIdentifier)
		{
			var response = await ApiServer.Get<AccountGetResponse>(GetUri($"users/{accountIdentifier}"));

			return response;
		}


		public static async Task<SubAccountGetCharacterSheet> PutAccountsCharacterByID(int ID, SubAccountGetCharacterSheet subAccountGetCharacterSheet, string token)
		{
			var response = await ApiServer.Put<SubAccountGetCharacterSheet>(
				GetUri($"persistence/characters/{ID}/update", null, true),
				subAccountGetCharacterSheet, token
				);
			return response;
		}



		public static async Task<string> DeleteAccountsCharacterByID(int ID, string token)
		{
			var response = await ApiServer.Delete(GetUri($"persistence/characters/{ID}/delete", null,true),token );
			return response;
		}

		public static async Task<SubAccountGetCharacterSheet> PostMakeAccountsCharacter(SubAccountGetCharacterSheet subAccountGetCharacterSheet, string token)
		{
			var response = await ApiServer.Post<SubAccountGetCharacterSheet>(GetUri($"persistence/characters/create", null,true), subAccountGetCharacterSheet, token);

			return response;
		}

		public static async Task<SubAccountGetCharacterSheet> GetAccountsCharacter(int ID, string token)
		{
			var response = await ApiServer.Get<SubAccountGetCharacterSheet>(GetUri($"persistence/characters/{ID}", null,true),
				token);

			return response;
		}


		public static async Task<AccountGetCharacterSheets> GetAccountsCharacters(
			string fork_compatibility,
			string characterSheetVersion,
			string token)
		{
			var response = await ApiServer.Get<AccountGetCharacterSheets>(
				GetUri($"persistence/characters/compatible", $"?fork_compatibility={fork_compatibility}&character_sheet_version={characterSheetVersion}",true)
				,token);

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
				unique_identifier = accountId,
				verification_token = token,
			};

			var response = await ApiServer.Post<AccountGetResponse>(GetUri("verify-account"), requestBody);

			return response;
		}
	}
}

// TODO for test case where is success 200 but object returned is unexpected
