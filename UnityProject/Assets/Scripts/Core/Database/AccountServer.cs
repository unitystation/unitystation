using System;
using System.Threading.Tasks;

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

		public static Uri GetUri(string endpoint, string queries = null, bool uglyPatch = false)
		{

			UriBuilder.Path = $"/accounts/{endpoint}";

			if (string.IsNullOrEmpty(queries) == false)
			{
				UriBuilder.Query = queries;
			}

			return UriBuilder.Uri;
		}

		public static async Task<ApiResult<AccountRegisterResponse>> Register(
			string uniqueIdentifier, string emailAddress, string username, string password)
		{
			var requestBody = new AccountRegister
			{
				email = emailAddress,
				unique_identifier = uniqueIdentifier,
				username = username,
				password = password,
			};

			var response = await ApiServer.Post<AccountRegisterResponse>(GetUri("register"), requestBody);

			if (!response.IsSuccess)
			{
				throw response.Exception!;
			}

			return response;
		}

		public static async Task<ApiResult<AccountLoginResponse>> Login(string emailAddress, string password)
		{
			AccountLoginCredentials requestBody = new()
			{
				email = emailAddress,
				password = password,
			};

			ApiResult<AccountLoginResponse> response = await ApiServer.Post<AccountLoginResponse>(GetUri("login-credentials"), requestBody);

			if (!response.IsSuccess)
			{
				throw response.Exception!;
			}

			return response;
		}

		public static async Task<ApiResult<AccountTokenLoginResponse>> Login(string token)
		{
			AccountLoginToken requestBody = new()
			{
				Token = token,
			};

			ApiResult<AccountTokenLoginResponse> response = await ApiServer.Post<AccountTokenLoginResponse>(GetUri("login-token"), requestBody);

			if (!response.IsSuccess)
			{
				throw response.Exception!;
			}

			return response;
		}

		public static async Task<JsonObject> Logout(string token, bool destroyAllSessions = false) // TODO: but no response?
		{
			AccountLogout requestBody = new()
			{
				Token = token,
			};

			var response = await ApiServer.Post<JsonObject>(GetUri(destroyAllSessions ? "logoutall" : "logout"), requestBody);

			return response;
		}

		public static async Task<ApiResult<AccountUpdateResponse>> UpdateAccount(string token, string emailAddress, string username, string password)
		{
			AccountUpdate requestBody = new()
			{
				Token = token,
				email = emailAddress,
				username = username,
				password = password,
			};

			var response = await ApiServer.Post<AccountUpdateResponse>(GetUri("update-account"), requestBody);

			return response;
		}


		public static async Task<ApiResult<AccountGetResponse>> GetAccountInfo(string accountIdentifier)
		{
			ApiResult<AccountGetResponse> response = await ApiServer.Get<AccountGetResponse>(GetUri($"users/{accountIdentifier}"));

			return response;
		}

		// Verification token is what is used by the game server to validate account on the account server.
		// This is a separate token to account token, which is to authorize account-related operations
		public static async Task<ApiResult<AccountVerificationTokenResponse>> GetVerificationToken(string accountToken)
		{
			ApiResult<AccountVerificationTokenResponse> response = await ApiServer.Get<AccountVerificationTokenResponse>(GetUri("request-verification-token"), accountToken);

			return response;
		}

		public static async Task<ApiResult<AccountGetResponse>> VerifyAccount(string accountId, string token)
		{
			AccountValidate requestBody = new()
			{
				unique_identifier = accountId,
				verification_token = token,
			};

			var response = await ApiServer.Post<AccountGetResponse>(GetUri("verify-account"), requestBody);

			return response;
		}

		public static async Task<ApiResult<JsonObject>> ResendEmailConfirmation(string email)
		{
			AccountResendEmailConfirmationRequest requestBody = new()
			{
				Email = email,
			};

			var response = await ApiServer.Post<JsonObject>(GetUri("resend-account-confirmation"), requestBody);
			return response;
		}
	}
}

// TODO for test case where is success 200 but object returned is unexpected
