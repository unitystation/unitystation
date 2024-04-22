using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Systems.Character;

namespace Core.Database
{
	#region Requests

	[Serializable]
	public class AccountRegister : JsonObject
	{
		[JsonProperty("unique_identifier")]
		public string UniqueIdentifier { get; set; }

		[JsonProperty("email")]
		public string Email {get; set;}

		[JsonProperty("username")]
		public string Username {get; set;}

		[JsonProperty("password")]
		public string Password {get; set;}
	}

	[Serializable]
	public class AccountLoginCredentials : JsonObject
	{
		[JsonProperty("email")]
		public string Email {get; set;}

		[JsonProperty("password")]
		public string Password {get; set;}
	}

	[Serializable]
	public class GetAccountsCharacters : JsonObject
	{
		[JsonProperty("fork_compatibility")]
		public string ForkCompatibility {get; set;}

		[JsonProperty("character_sheet_version")]
		public string CharacterSheetVersion {get; set;}
	}

	[Serializable]
	public class AccountLoginToken : JsonObject, ITokenAuthable
	{
		public string Token { get; set; }
	}

	[Serializable]
	public class AccountLogout : JsonObject, ITokenAuthable
	{
		public string Token { get; set; }
	}

	[Serializable]
	public class AccountUpdate : JsonObject, ITokenAuthable
	{
		public string Token { get; set; }
		[JsonProperty("email")]
		public string Email {get; set;}

		[JsonProperty("username")]
		public string Username {get; set;}

		[JsonProperty("password")]
		public string Password {get; set;}
	}

	[Serializable]
	public class AccountUpdateCharacters : JsonObject, ITokenAuthable
	{
		public string Token { get; set; }

		[JsonProperty("characters")]
		public Dictionary<string, Dictionary<string, CharacterSheet>> Characters { get; set; }
	}

	[Serializable]
	public class AccountValidate : JsonObject
	{
		[JsonProperty("unique_identifier")]
		public string UniqueIdentifier {get; set;}

		[JsonProperty("verification_token")]
		public string VerificationToken {get; set;}
	}

	#endregion

	#region Responses


	[Serializable]
	public class AccountGetCharacterSheets : JsonObject
	{
		[JsonProperty("count")]
		public int Count {get; set;}

		[JsonProperty("results")]
		public List<SubAccountGetCharacterSheet> Results {get; set;}
	}

	[Serializable]
	public class SubAccountGetCharacterSheet : JsonObject
	{
		[JsonProperty("id")]
		public int Id {get; set;}

		[JsonProperty("account")]
		public string Account {get; set;}

		[JsonProperty("fork_compatibility")]
		public string ForkCompatibility {get; set;}

		[JsonProperty("character_sheet_version")]
		public string CharacterSheetVersion {get; set;}

		[JsonProperty("data")]
		public CharacterSheet Data {get; set;}

		[JsonProperty("last_updated")]
		public DateTime LastUpdated {get; set;}
	}

	[Serializable]
	public class AccountGetResponse : JsonObject
	{
		[JsonProperty("unique_identifier")]
		public string UniqueIdentifier {get; set;}

		[JsonProperty("username")]
		public string Username {get; set;}

		[JsonProperty("is_verified")]
		public bool IsVerified {get; set;}
	}

	[Serializable]
	public class AccountRegisterResponse : JsonObject
	{
		[JsonProperty("account")]
		public AccountRegisterDetails Account { get; set; }
	}

	[Serializable]
	public class AccountRegisterDetails : JsonObject
	{
		[JsonProperty("unique_identifier")]
		public string UniqueIdentifier {get; set;}

		[JsonProperty("email")]
		public string Email {get; set;}

		[JsonProperty("username")]
		public string Username {get; set;}
	}

	[Serializable]
	public class AccountLoginResponse : JsonObject
	{
		[JsonProperty("token")]
		public string Token {get; set;}

		[JsonProperty("account")]
		public AccountGetResponse Account {get; set;}
	}

	[Serializable]
	public class AccountUpdateResponse : JsonObject
	{
		[JsonProperty("email")]
		public string Email {get; set;}

		[JsonProperty("username")]
		public string Username {get; set;}
	}

	[Serializable]
	public class AccountUpdateCharactersResponse : JsonObject
	{

	}

	[Serializable]
	public class AccountVerificationTokenResponse : JsonObject
	{
		[JsonProperty("verification_token")]
		public string VerificationToken { get; set; }
	}

	[Serializable]
	public class AccountResendEmailConfirmationRequest : JsonObject
	{
		[JsonProperty("email")]
		public string Email { get; set; }
	}

	#endregion
}
