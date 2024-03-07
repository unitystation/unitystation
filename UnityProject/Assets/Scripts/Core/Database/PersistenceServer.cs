using System;
using System.Threading.Tasks;

namespace Core.Database
{
	public static class PersistenceServer
	{
		public static string Host = GameManager.Instance.AccountAPIHost;
		public static UriBuilder UriBuilder = new("https", Host);

		public static Uri GetUri(string endpoint, string queries = null, bool uglyPatch = false)
		{
			UriBuilder.Path = $"/persistence/{endpoint}";

			if (string.IsNullOrEmpty(queries) == false)
			{
				UriBuilder.Query = queries;
			}

			return UriBuilder.Uri;
		}

		public static async Task<ApiResult<SubAccountGetCharacterSheet>> PutAccountsCharacterByID(int id, SubAccountGetCharacterSheet subAccountGetCharacterSheet, string token)
		{
			var response = await ApiServer.Put<SubAccountGetCharacterSheet>(
				GetUri($"characters/{id}/update", null, true),
				subAccountGetCharacterSheet, token
			);
			return response;
		}

		public static async Task<ApiResult<SubAccountGetCharacterSheet>> PostMakeAccountsCharacter(SubAccountGetCharacterSheet subAccountGetCharacterSheet, string token)
		{
			var response = await ApiServer.Post<SubAccountGetCharacterSheet>(GetUri($"characters/create", null,true), subAccountGetCharacterSheet, token);

			return response;
		}

		public static async Task<ApiResult<SubAccountGetCharacterSheet>> GetAccountsCharacter(int ID, string token)
		{
			var response = await ApiServer.Get<SubAccountGetCharacterSheet>(GetUri($"characters/{ID}", null,true),
				token);

			return response;
		}

		public static async Task<ApiResult<AccountGetCharacterSheets>> GetAccountsCharacters(
			string forkCompatibility,
			string characterSheetVersion,
			string token)
		{
			var response = await ApiServer.Get<AccountGetCharacterSheets>(
				GetUri($"characters/compatible", $"?fork_compatibility={forkCompatibility}&character_sheet_version={characterSheetVersion}",true)
				,token);

			return response;
		}

		public static async Task<ApiResult<JsonObject>> DeleteAccountsCharacterByID(int id, string token)
		{
			var response = await ApiServer.Delete<JsonObject>(GetUri($"characters/{id}/delete", null,true),token );
			return response;
		}
	}
}
