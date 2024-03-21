using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Core.Database;
using Systems.Character;

namespace Core.Accounts
{
	/// <summary>The object representation of a player's Unitystation account.</summary>
	public class Account
	{
		/// <summary>The identifier for the player's Unitystation account.</summary>
		public string Id { get;  set; }

		/// <summary>Human-friendly public-facing username of the player's account.</summary>
		public string Username { get;  set; }

		/// <summary>The token associated with the account for logging in, changing password, etc.</summary>
		/// <remarks>Not to be confused with the game token or verification token
		/// as used by <see cref="Networking.Authenticator"/>, which allows a
		/// game server to authenticate a Unitystation account.</remarks>
		public string Token { get; private set; }

		/// <summary>Determine if this account has been verified (i.e. is someone of importance in the scene, like twitter verified ticks).</summary>
		public bool IsVerified { get; private set; }

		/// <summary>A dictionary of the account's supported characters. The characters are keyed by a uint for identification.</summary>
		public Dictionary<string, CharacterSheet> Characters { get; private set; } = new();

		// TODO if the object instance exists, isn't that enough to consider the account available?
		public bool IsAvailable { get; private set; } = false;

		public bool IsLoggedIn { get; private set; }

		public static Account FromAccountGetResponse(AccountGetResponse accountResponse)
		{
			Account account = new Account().PopulateAccount(accountResponse);
			account.IsLoggedIn = true;
			return account;
		}

		public async Task<Account> Login(string emailAddress, string password)
		{
			ApiResult<AccountLoginResponse> loginResponse = await AccountServer.Login(emailAddress, password);

			if (!loginResponse.IsSuccess)
			{
				IsAvailable = false;
				IsLoggedIn = false;
				return this;
			}

			AccountLoginResponse account = loginResponse.Data;

			PostLogin(account!.Token, account.Account);

			return this;
		}

		public async Task<Account> Login(string token)
		{
			ApiResult<AccountLoginResponse> loginResponse = await AccountServer.Login(token);

			if (!loginResponse.IsSuccess)
			{
				IsAvailable = false;
				IsLoggedIn = false;
				return this;
			}

			AccountLoginResponse account = loginResponse.Data;

			PostLogin(account!.Token, account.Account);

			return this;
		}

		public async Task<Account> Register(string userId, string emailAddress, string username, string password)
		{
			ApiResult<AccountRegisterResponse> registerResponse = await AccountServer.Register(userId, emailAddress, username, password);

			if (!registerResponse.IsSuccess)
			{
				IsAvailable = false;
				IsLoggedIn = false;

				//TODO: communicate the error to the user?!
				return this;
			}

			AccountRegisterResponse account = registerResponse.Data;

			Id = account!.Account.UniqueIdentifier;
			Username = account.Account.Username;

			return this; // set IsAvailable true?
		}

		public async Task<Account> ResendMailConfirmation(string email)
		{
			ApiResult<JsonObject> response = await AccountServer.ResendEmailConfirmation(email);

			if (!response.IsSuccess)
			{
				IsAvailable = false;
				IsLoggedIn = false;
				throw response.Exception!;
			}

			return this;
		}

		// get latest info for this account
		public async Task<Account> FetchAccount()
		{
			ApiResult<AccountGetResponse> accountResponse = await AccountServer.GetAccountInfo(Id);

			if (!accountResponse.IsSuccess)
			{
				IsAvailable = false;
				IsLoggedIn = false;
				return this;
			}

			AccountGetResponse account = accountResponse.Data;

			PopulateAccount(account);

			return this;
		}

		public async Task<string> GetPlayerToken()
		{
			ApiResult<AccountVerificationTokenResponse> response = await AccountServer.GetVerificationToken(Token);

			if (!response.IsSuccess)
			{
				IsAvailable = false;
				IsLoggedIn = false;
				return "";
			}

			return response.Data!.VerificationToken;
		}

		public async Task<CharacterSheet> GetCharacter(string key)
		{
			if (Characters.TryGetValue(key, out CharacterSheet character))
			{
				return character;
			}

			// else fetch character
			await FetchAccount();
			return Characters.TryGetValue(key, out character) ? character : null;
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
			IsLoggedIn = true;
		}

		private Account PopulateAccount(AccountGetResponse accountResponse)
		{
			Id = accountResponse.UniqueIdentifier;
			Username = accountResponse.Username;
			IsVerified = accountResponse.IsVerified;

			PlayerManager.CharacterManager.Init();
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