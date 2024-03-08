using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Systems.Character;
using UnityEngine.Serialization;

namespace Core.Database
{
	#region Requests

	[Serializable]
	public class AccountRegister : JsonObject
	{
		public string unique_identifier; // unique
		public string email;
		public string username; // non-unique, public-facing
		public string password;
	}

	[Serializable]
	public class AccountLoginCredentials : JsonObject
	{
		public string email;
		public string password;
	}

	[Serializable]
	public class GetAccountsCharacters : JsonObject
	{
		public string fork_compatibility;
		public string character_sheet_version;
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
		public string email;
		public string username;
		public string password;
	}

	[Serializable]
	public class AccountUpdateCharacters : JsonObject, ITokenAuthable
	{
		public string Token { get; set; }
		public Dictionary<string, Dictionary<string, CharacterSheet>> characters;
	}

	[Serializable]
	public class AccountValidate : JsonObject
	{
		public string unique_identifier;
		public string verification_token;
	}

	#endregion

	#region Responses


	[Serializable]
	public class AccountGetCharacterSheets : JsonObject
	{
		public int count;
		public List<SubAccountGetCharacterSheet> results;
	}

	[Serializable]
	public class SubAccountGetCharacterSheet : JsonObject
	{
		public int id;
		public string account;
		public string fork_compatibility;
		public string character_sheet_version;
		public CharacterSheet data;
		public DateTime last_updated;
	}



	[Serializable]
	public class AccountGetResponse : JsonObject
	{
		public string unique_identifier;
		public string username;
		public bool is_verified;
	}

	[Serializable]
	public class AccountRegisterResponse : JsonObject
	{
		public AccountRegisterDetails account;
	}

	[Serializable]
	public class AccountRegisterDetails : JsonObject
	{
		public string unique_identifier;
		public string email;
		public string username;
	}

	[Serializable]
	public class AccountLoginResponse : JsonObject
	{
		public string token;
		public AccountGetResponse account;
	}

	[Serializable]
	public class AccountTokenLoginResponse : JsonObject
	{
		public string token;
		public AccountGetResponse user;
	}

	[Serializable]
	public class AccountUpdateResponse : JsonObject
	{
		public string email;
		public string username;
	}

	[Serializable]
	public class AccountUpdateCharactersResponse : JsonObject
	{

	}

	[Serializable]
	public class AccountVerificationTokenResponse : JsonObject
	{
		public string verification_token;
	}

	[Serializable]
	public class AccountResendEmailConfirmationRequest : JsonObject
	{
		[JsonProperty("email")]
		public string Email { get; init; }
	}

	#endregion
}
