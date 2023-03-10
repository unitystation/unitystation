using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Core.Database;
using Systems.Character;

namespace Core.Accounts
{
	// TODO: what add check to see if account is disabled (e.g. cuz user not verified)
	/// <summary>The object representation of a player's Unitystation account.</summary>
	public class Account
	{
		/// <summary>The identifier for the player's Unitystation account.</summary>
		public string Id { get; private set; }

		/// <summary>Human-friendly public-facing username of the player's account.</summary>
		public string Username { get; private set; }

		/// <summary>The token associated with the account for logging in, changing password, etc.</summary>
		/// <remarks>Not to be confused with the game token or verification token
		/// as used by <see cref="Networking.Authenticator"/>, which allows a
		/// game server to authenticate a Unitystation account.</remarks>
		public string Token { get; private set; }

		/// <summary>Determine if this account has been verified (i.e. registration email confirmed).
		/// Some servers might allow unverified or offline accounts.</summary>
		public bool IsVerified { get; private set; }

		/// <summary>The conclusive list of of the account's characters, including characters considered not compatible with this version.</summary>
		/// <remarks>This extra string layer doesn't do much aside from allow room for a different set of characters,
		/// e.g. for a different fork with different content.</remarks>
		public Dictionary<string, Dictionary<string, CharacterSheet>> AllCharacters { get; private set; } = new();

		/// <summary>A dictionary of the account's supported characters. The characters are keyed by a uint for identification.</summary>
		public Dictionary<string, CharacterSheet> Characters { get; private set; } = new();

		// TODO if the object instance exists, isn't that enough to consider the account available?
		public bool IsAvailable { get; private set; } = false;

		public static Account FromAccountGetResponse(AccountGetResponse accountResponse)
		{
			return new Account().PopulateAccount(accountResponse);
		}

		public async Task<Account> Login(string emailAddress, string password)
		{
			var loginResponse = await AccountServer.Login(emailAddress, password);
			PostLogin(loginResponse.token, loginResponse.account);

			return this;
		}

		public async Task<Account> Login(string token)
		{
			var loginResponse = await AccountServer.Login(token);
			PostLogin(loginResponse.token, loginResponse.user);

			return this;
		}

		public async Task<Account> Register(string userId, string emailAddress, string username, string password)
		{
			var registerResponse = await AccountServer.Register(userId, emailAddress, username, password);

			Token = registerResponse.token;
			Id = registerResponse.account.account_identifier;
			Username = registerResponse.account.username;

			return this; // set IsAvailable true?
		}

		public async Task<Account> RequestNewVerifyEmail()
		{
			// TODO
			//AccountServer.RequestVerifyEmail(Token);

			throw new System.NotImplementedException();
		}

		// get latest info for this account
		public async Task<Account> FetchAccount()
		{
			var accountResponse = await AccountServer.GetAccountInfo(Id);
			PopulateAccount(accountResponse);

			return this;
		}

		public async Task<string> GetPlayerToken()
		{
			var response = await AccountServer.GetVerificationToken(Token);

			return response.verification_token;
		}

		public async Task<CharacterSheet> GetCharacter(string key)
		{
			if (Characters.TryGetValue(key, out CharacterSheet character))
			{
				return character;
			}

			// else fetch character
			await FetchAccount();
			if (Characters.TryGetValue(key, out character))
			{
				return character;
			}

			return character;
		}

		// characterId optional, assign to update existing character
		public async Task<string> SetCharacter(CharacterSheet character, string key = "")
		{
			// Fetch latest account data (we may have saved a character on another session during this session).
			await FetchAccount();

			// Generate a random character ID
			if (string.IsNullOrEmpty(key))
			{
				// Collision risk should be low enough for the consequence
				key = RandomUtils.GetRandomString(8);
			}

			Characters.Add(key, character);
			AllCharacters[CharacterManager.CharacterSheetVersion] = Characters;

			await AccountServer.UpdateCharacters(Token, AllCharacters);

			return key;
		}

		public async void Logout(bool destroyAllSessions = false)
		{
			await AccountServer.Logout(Token, destroyAllSessions);

			DeleteAccountCache();
		}

		private void PostLogin(string token, AccountGetResponse account)
		{
			Token = token;
			PopulateAccount(account);
			UpdateAccountCache();

			IsAvailable = true;
		}

		private Account PopulateAccount(AccountGetResponse accountResponse)
		{
			Id = accountResponse.account_identifier;
			Username = accountResponse.username;
			IsVerified = accountResponse.is_verified;
			AllCharacters = accountResponse.characters_data;

			// May be null if AccountGetResponse schema is out of date with the API (TODO: affects all responses, so handle them)
			AllCharacters ??= new();
			Characters = AllCharacters[CharacterManager.CharacterSheetVersion] ?? new();

			return this;
		}

		private void UpdateAccountCache()
		{
			// Only save details if auto-login (consider poorly set-up gaming cafe)
			if (PlayerPrefs.GetInt(PlayerPrefKeys.AccountAutoLogin) == 1)
			{
				PlayerPrefs.SetString(PlayerPrefKeys.AccountName, Username);
				PlayerPrefs.SetString(PlayerPrefKeys.AccountToken, Token);
			}

			PlayerPrefs.Save();
		}

		private void DeleteAccountCache()
		{
			PlayerPrefs.DeleteKey(PlayerPrefKeys.AccountName);
			PlayerPrefs.DeleteKey(PlayerPrefKeys.AccountToken);

			PlayerPrefs.Save();
		}
	}
}
