using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Core.Database;
using SecureStuff;
using Newtonsoft.Json;
using UnityEngine;
using DatabaseAPI;
using Logs;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Systems.Character
{
	/// <summary>Manage a player's characters. Intended for the local player client.</summary>
	// TODO this class has stubs
	public class CharacterManager
	{
		/// <summary>Character sheets not under this version will be ignored.</summary>
		public static readonly string CharacterSheetVersion = "1.0.0";

		public static readonly string CharacterSheetForkCompatibility = "Unitystation";


		/// <summary>
		/// A list of the player's loaded characters.
		/// Please consider using <see cref="CharacterManager"/>'s methods to manipulate the list instead of directly.
		/// </summary>
		public List<SubAccountGetCharacterSheet> Characters { get; } = new();

		/// <summary>Get the key associated with the active character (the character the rest of the game should use).</summary>
		public int ActiveCharacterKey { get; private set; } = 0;

		/// <summary>Get the active character (the character the rest of the game should use).</summary>
		public CharacterSheet ActiveCharacter => Get(ActiveCharacterKey);

		private string OfflineStoragePath => $"characters.json";


		public void Init()
		{
			LoadCharacters();
		}

		private void DetermineActiveCharacter()
		{
			if (Characters.Count <= 0)
			{
				// No characters? All good, just create a random one and remember it.
				var defaultCharacter = CharacterSheet.GenerateRandomCharacter();
				Add(defaultCharacter);
				SetLastCharacterKey(0);
				SaveCharactersOffline();
				return;
			}

			int lastKeyUsed = GetLastCharacterKey();
			SetActiveCharacter(IsCharacterKeyValid(lastKeyUsed) ? lastKeyUsed : Characters.Count - 1);
		}

		public void SetActiveCharacter(int key)
		{
			if (IsCharacterKeyValid(key) == false)
			{
				Loggy.LogError(
					"An attempt was made to set the active character with a key that doesn't exist. Ignoring.");
				return;
			}

			ActiveCharacterKey = key;
		}

		/// <summary>Check if the provided <see cref="CharacterSheet"/> key is valid.</summary>
		/// <param name="key">The <see cref="CharacterSheet"/> key to check.</param>
		/// <returns>True if the key is valid.</returns>
		public bool IsCharacterKeyValid(int key)
		{
			return key >= 0 && key < Characters.Count;
		}

		/// <summary>Get the key of the <see cref="CharacterSheet"/> that was last set as active.</summary>
		/// <returns>The last active <see cref="CharacterSheet"/>.</returns>
		public int GetLastCharacterKey()
		{
			int lastCharacterIndex = PlayerPrefs.GetInt(PlayerPrefKeys.LastCharacterIndex);

			return Math.Clamp(lastCharacterIndex, 0, Characters.Count);
		}

		/// <summary>Set and remember the <see cref="CharacterSheet"/> that should be automatically selected as active.</summary>
		/// <param name="key">Key of the <see cref="CharacterSheet"/>.</param>
		public void SetLastCharacterKey(int key)
		{
			if (IsCharacterKeyValid(key) == false)
			{
				Loggy.LogError(
					"An attempt was made to set the active character with a key that doesn't exist. Ignoring.");
				return;
			}

			PlayerPrefs.SetInt(PlayerPrefKeys.LastCharacterIndex, key);
			PlayerPrefs.Save();
		}

		/// <summary>Get the <see cref="CharacterSheet"/> associated with the given key or default.</summary>
		/// <param name="key">Key associated with the requested <see cref="CharacterSheet"/>.</param>
		/// <returns><see cref="CharacterSheet"/> or default.</returns>
		public CharacterSheet Get(int key)
		{
			if (IsCharacterKeyValid(key) == false)
			{
				Loggy.LogWarning($"An attempt was made to fetch a character with an invalid key \"{key}\". Ignoring.");
				if (Characters.Count > 0)
				{
					return default;
				}
				else
				{
					return new CharacterSheet();
				}
			}

			var Character = Characters[key];
			return Character.data;
		}

		/// <summary>Set the <see cref="CharacterSheet"/> associated with the given key.</summary>
		/// <param name="key">Key associated with the updated <see cref="CharacterSheet"/>.</param>
		/// <param name="character"><see cref="CharacterSheet"/> to set.</param>
		public void Set(int key, CharacterSheet character)
		{
			if (IsCharacterKeyValid(key) == false)
			{
				Loggy.LogWarning($"An attempt was made to set a character with an invalid key \"{key}\". Ignoring.");
				return;
			}

			Characters[key].data = character;

			_ = PersistenceServer.PutAccountsCharacterByID(Characters[key].id, Characters[key], PlayerManager.Account.Token);
			Task.Run(() => UpdateCharacterOnline(Characters[key]));
			SaveCharacters(false);
		}

		public async Task UpdateCharacterOnline(SubAccountGetCharacterSheet character)
		{
			ApiResult<SubAccountGetCharacterSheet> response = await PersistenceServer.PutAccountsCharacterByID(character.id, character, PlayerManager.Account.Token);

			if (!response.IsSuccess)
			{
				Loggy.LogError($"Failed to update character online. because: {response.Exception!.Message}");
				//TODO: feedback to user
				return;
			}

			SubAccountGetCharacterSheet characters = response.Data;

			character.last_updated = characters!.last_updated;
			SaveCharacters(false);
		}


		/// <summary>Add a new <see cref="CharacterSheet"/>.</summary>
		/// <param name="character"><see cref="CharacterSheet"/> to add.</param>
		public void Add(CharacterSheet character)
		{
			if (ValidateCharacterSheet(character) == false)
			{
				Loggy.LogError("An attempt was made to add a character but character validation failed. Ignoring.");
				return;
			}

			var SubAccountGetcharacter = new SubAccountGetCharacterSheet()
			{
				account = PlayerManager.Account.Id,
				fork_compatibility = CharacterSheetForkCompatibility,
				character_sheet_version = CharacterSheetVersion,
				data = character
			};
			Characters.Add(SubAccountGetcharacter);
			Task.Run(() => SaveNewCharacterTask(SubAccountGetcharacter));
			SaveCharacters(false);
		}


		public void Add(SubAccountGetCharacterSheet character, bool AddOnline = true)
		{
			if (ValidateCharacterSheet(character.data) == false)
			{
				Loggy.LogError("An attempt was made to add a character but character validation failed. Ignoring.");
				return;
			}


			Characters.Add(character);
			if (AddOnline)
			{
				Task.Run(() => SaveNewCharacterTask(character));
			}

			SaveCharacters(false);
		}


		public async Task SaveNewCharacterTask(SubAccountGetCharacterSheet character)
		{
			ApiResult<SubAccountGetCharacterSheet> response = await PersistenceServer.PostMakeAccountsCharacter(character, PlayerManager.Account.Token);
			if (response.IsSuccess == false)
			{
				Loggy.LogError($"Failed to save new character online. because: {response.Exception!.Message}");
				return;
			}

			SubAccountGetCharacterSheet characterSheet = response.Data;

			character.id = characterSheet!.id;
			SaveCharacters(false);
		}

		/// <summary>Remove a <see cref="CharacterSheet"/> associated with the given key.</summary>
		/// <param name="key">Key associated with the <see cref="CharacterSheet"/> to be removed.</param>
		public void Remove(int key)
		{
			if (IsCharacterKeyValid(key) == false)
			{
				Loggy.LogWarning($"An attempt was made to remove a character with an invalid key \"{key}\". Ignoring.");
				return;
			}

			if (key < Characters.Count - 1)
			{
				Loggy.LogWarning(
					$"An attempt was made to remove the last character with key \"{key}\". Ignoring as there should be at least one character.");
				return;
			}

			if (ActiveCharacterKey == key)
			{
				SetActiveCharacter(key - 1);
				SetLastCharacterKey(key - 1);
			}

			var CharacterRemove = Characters[key];
			Characters.RemoveAt(key);

			_ = PersistenceServer.DeleteAccountsCharacterByID(CharacterRemove.id, PlayerManager.Account.Token);
			SaveCharacters(false);
		}

		public async Task LoadOnlineCharacters()
		{
			try
			{
				ApiResult<AccountGetCharacterSheets> accountResponse =
					await PersistenceServer.GetAccountsCharacters(CharacterSheetForkCompatibility, CharacterSheetVersion, PlayerManager.Account.Token);

				if (!accountResponse.IsSuccess)
				{
					Loggy.LogError($"Failed to load characters online. because: {accountResponse.Exception!.Message}");
					throw accountResponse.Exception;
				}

				AccountGetCharacterSheets characters = accountResponse.Data;

				Characters.AddRange(characters!.results);
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
			}

			SaveCharacters(false);
		}


		/// <summary>Load characters that are saved to Unity's persistent data folder.</summary>
		public async Task LoadCharacters()
		{
			Characters.Clear();
			if (AccessFile.Exists(OfflineStoragePath, userPersistent: true) == false)
			{
				await LoadOnlineCharacters();
			}
			else
			{
				string json = AccessFile.Load(OfflineStoragePath, userPersistent: true);
				var old = false;
				List<SubAccountGetCharacterSheet> characters = new List<SubAccountGetCharacterSheet>();
				try
				{
					characters = JsonConvert.DeserializeObject<List<SubAccountGetCharacterSheet>>(json);
					if (characters.Count == 0 || characters[0].data == null)
					{
						old = true;
						characters.Clear();
					}
				}
				catch (Exception e)
				{
					Loggy.LogError("OLD Characters detected porting");
					old = true;
				}


				if (old)
				{
					var OLDCharacters = JsonConvert.DeserializeObject<List<CharacterSheet>>(json);

					foreach (var OLDCharacter in OLDCharacters)
					{
						characters.Add(new SubAccountGetCharacterSheet()
						{
							account = PlayerManager.Account.Id,
							fork_compatibility = CharacterSheetForkCompatibility,
							character_sheet_version = CharacterSheetVersion,
							data = OLDCharacter
						});
					}

				}
				else
				{
					characters = JsonConvert.DeserializeObject<List<SubAccountGetCharacterSheet>>(json);
				}


				if (characters != null)
				{
					foreach (var character in characters)
					{
						Add(character, false);
					}

					ApiResult<AccountGetCharacterSheets> accountResponse = null;


					try
					{
						accountResponse = await PersistenceServer.GetAccountsCharacters(CharacterSheetForkCompatibility, CharacterSheetVersion, PlayerManager.Account.Token);
					}
					catch (Exception e)
					{
						Loggy.LogError(e.ToString());
					}



					if (accountResponse != null)
					{
						List<SubAccountGetCharacterSheet> MissingOnline = new List<SubAccountGetCharacterSheet>();
						List<SubAccountGetCharacterSheet> MissingLocal = new List<SubAccountGetCharacterSheet>();

						List<SubAccountGetCharacterSheet> UpdateOnline = new List<SubAccountGetCharacterSheet>();
						List<ToUpdateLocal> UpdateLocal = new List<ToUpdateLocal>();

						foreach (var LocalCharacter in Characters)
						{
							bool OnlineHasNotLocal = true;
							foreach (var OnlineCharacter in accountResponse.Data.results)
							{
								if (OnlineCharacter.id == LocalCharacter.id)
								{
									OnlineHasNotLocal = false;

									if (OnlineCharacter.last_updated > LocalCharacter.last_updated)
									{
										UpdateLocal.Add(new ToUpdateLocal()
										{
											local =  LocalCharacter,
											online = OnlineCharacter
										});
									}
									if (OnlineCharacter.last_updated < LocalCharacter.last_updated)
									{
										UpdateOnline.Add(LocalCharacter);
									}
								}
							}

							if (OnlineHasNotLocal)
							{
								MissingOnline.Add(LocalCharacter);
							}
						}

						foreach (var OnlineCharacter in accountResponse.Data.results)
						{
							bool LocalHasNotOnline = true;
							foreach (var LocalCharacter in Characters)
							{
								if (OnlineCharacter.id == LocalCharacter.id)
								{
									LocalHasNotOnline = false;
									//TODO is Missing date modified field
								}
							}

							if (LocalHasNotOnline)
							{
								MissingLocal.Add(OnlineCharacter);
							}
						}



						foreach (var character in MissingLocal)
						{
							Add(character, false);
						}

						foreach (var character in MissingOnline)
						{
							await SaveNewCharacterTask(character);
						}

						foreach (var character in UpdateOnline)
						{
							await PersistenceServer.PutAccountsCharacterByID(character.id, character, PlayerManager.Account.Token);
						}

						foreach (var character in UpdateLocal)
						{
							character.local.data = character.online.data;
							character.local.fork_compatibility = character.online.fork_compatibility;
							character.local.account = character.online.account;
							character.local.id = character.online.id;
							character.local.character_sheet_version = character.online.character_sheet_version;
							character.local.last_updated = character.online.last_updated;
						}
						SaveCharacters(false);
					}
				}
			}

			DetermineActiveCharacter();
		}

		public struct ToUpdateLocal
		{
			public SubAccountGetCharacterSheet online;
			public SubAccountGetCharacterSheet local;

		}

		/// <summary>Save characters to both the cloud and offline storage.</summary>
		public void SaveCharacters(bool Online = true)
		{
			SaveCharactersOffline();
			if (Online)
			{
				SaveCharactersOnline();
			}
		}

		/// <summary>Save characters to the cloud.</summary>
		public void SaveCharactersOnline()
		{
			//TODO
			// foreach (var Character in Characters)
			// {
			//
			// 	_ = PlayerManager.Account.SaveCharacters(Characters);
			// }
		}

		/// <summary>Save characters to Unity's persistent data folder.</summary>
		public void SaveCharactersOffline()
		{
			var settings = new JsonSerializerSettings
			{
				PreserveReferencesHandling = PreserveReferencesHandling.All,
				NullValueHandling = NullValueHandling.Ignore,
				ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
				Formatting = Formatting.Indented
			};

			string json = Characters.Count == 0
				? ""
				: JsonConvert.SerializeObject(Characters, settings);

			if (AccessFile.Exists(OfflineStoragePath, userPersistent: true))
			{
				AccessFile.Delete(OfflineStoragePath, userPersistent: true);
			}

			AccessFile.Save(OfflineStoragePath, json, userPersistent: true);
		}

		public bool ValidateCharacterSheet(CharacterSheet character)
		{
			if (character == null) return false;

			try
			{
				character.ValidateSettings();
			}
			catch (InvalidOperationException)
			{
				return false;
			}

			return true;
		}
	}
}