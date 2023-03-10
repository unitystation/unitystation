using System;
using System.Collections.Generic;
using Systems.Character;

namespace Core.Database
{
	#region Requests

	[Serializable]
	public class AccountRegister : JsonObject
	{
		public string account_identifier; // unique
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
		public string account_identifier;
		public string verification_token;
	}

	#endregion

	#region Responses

	[Serializable]
	public class AccountGetResponse : JsonObject
	{
		public string account_identifier;
		public string username;
		public bool is_verified;
		public Dictionary<string, Dictionary<string, CharacterSheet>> characters_data;
	}

	[Serializable]
	public class AccountRegisterResponse : JsonObject
	{
		public AccountRegisterDetails account;
		public string token;
	}

	[Serializable]
	public class AccountRegisterDetails : JsonObject
	{
		public string account_identifier;
		public string email;
		public string username;
	}

	[Serializable]
	public class AccountLoginResponse : JsonObject
	{
		public AccountGetResponse account;
		public string token;
	}

	[Serializable]
	public class AccountTokenLoginResponse : JsonObject
	{
		public AccountGetResponse user;
		public string token;
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

	#endregion
}
