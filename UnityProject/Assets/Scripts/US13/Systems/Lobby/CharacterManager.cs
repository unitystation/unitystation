using System;
using System.Collections.Generic;
using System.Text;
using Logs;
using Newtonsoft.Json;
using SecureStuff;
using US13.Core.Database;
using US13.Core.Initialisation;
using US13.Managers.NetworkManagement;
using US13.Player;
using US13.PlayerPrefs;
using US13.UI.Systems;
using US13.UI.Systems.PreRound;
using Task = System.Threading.Tasks.Task;

namespace US13.Systems.Lobby
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

		/// <summary>Get the id of the active character (the character the rest of the game should use).</summary>
		public int ActiveCharacterId { get; private set; }

		/// <summary>Get the active character (the character the rest of the game should use).</summary>
		public CharacterSheet ActiveCharacter => Get(ActiveCharacterId);

		/// <summary>Raised on the main thread when the active character is swapped or edited.</summary>
		public event Action OnActiveCharacterChanged;

		private string OfflineStoragePath => $"characters.json";


		public void Init()
		{
			_ = LoadCharacters();
		}

		private void DetermineActiveCharacter()
		{
			if (Characters.Count <= 0)
			{
				// No characters? All good, just create a random one and remember it.
				var defaultCharacter = CharacterSheet.GenerateRandomCharacter();
				Add(defaultCharacter);
				SetActiveCharacter(Characters[0].Id);
				SetLastCharacter(Characters[0].Id);
				SaveCharacters();
				return;
			}

			int lastId = GetLastCharacterId();
			SetActiveCharacter(HasCharacter(lastId) ? lastId : Characters[Characters.Count - 1].Id);
		}

		/// <summary>Set the character that the rest of the game should use.</summary>
		/// <param name="id">Id of the <see cref="CharacterSheet"/>.</param>
		public void SetActiveCharacter(int id)
		{
			if (HasCharacter(id) == false)
			{
				Loggy.Error(
					$"An attempt was made to set the active character to id \"{id}\" which doesn't exist. Ignoring.");
				return;
			}

			ActiveCharacterId = id;
			NotifyActiveCharacterChanged();
		}

		private void NotifyActiveCharacterChanged()
		{
			LoadManager.DoInMainThread(() => OnActiveCharacterChanged?.Invoke());
		}

		/// <summary>Check whether a <see cref="CharacterSheet"/> with the given id is loaded.</summary>
		/// <param name="id">The <see cref="CharacterSheet"/> id to check.</param>
		public bool HasCharacter(int id)
		{
			return IndexOfId(id) != -1;
		}

		/// <summary>Get the list position of the <see cref="CharacterSheet"/> with the given id, or -1.</summary>
		/// <param name="id">The <see cref="CharacterSheet"/> id to look for.</param>
		public int IndexOfId(int id)
		{
			if (id == 0) return -1;

			for (int i = 0; i < Characters.Count; i++)
			{
				if (Characters[i].Id != id) continue;

				return i;
			}

			return -1;
		}

		/// <summary>Get the id of the <see cref="CharacterSheet"/> that was last set as active.</summary>
		public int GetLastCharacterId()
		{
			return UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.LastCharacterId);
		}

		/// <summary>Remember the <see cref="CharacterSheet"/> that should be automatically selected as active.</summary>
		/// <param name="id">Id of the <see cref="CharacterSheet"/>.</param>
		public void SetLastCharacter(int id)
		{
			if (HasCharacter(id) == false)
			{
				Loggy.Error(
					$"An attempt was made to remember character id \"{id}\" which doesn't exist. Ignoring.");
				return;
			}

			UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.LastCharacterId, id);
			UnityEngine.PlayerPrefs.Save();
		}

		private bool IsRegisteredOnline(SubAccountGetCharacterSheet character)
		{
			return character.Id > 0;
		}

		private void EnsureId(SubAccountGetCharacterSheet character)
		{
			if (character.Id != 0) return;

			int lowest = 0;
			foreach (var existing in Characters)
			{
				if (existing.Id < lowest)
				{
					lowest = existing.Id;
				}
			}

			character.Id = lowest - 1;
		}

		private void ReplaceCharacterId(int oldId, int newId)
		{
			if (ActiveCharacterId != oldId) return;

			ActiveCharacterId = newId;
			SetLastCharacter(newId);
		}

		/// <summary>Get the <see cref="CharacterSheet"/> with the given id, or default.</summary>
		/// <param name="id">Id of the requested <see cref="CharacterSheet"/>.</param>
		/// <returns><see cref="CharacterSheet"/> or default.</returns>
		public CharacterSheet Get(int id)
		{
			CharacterSheet GenerateRandomSheet()
			{
				var sheet = CharacterSheet.GenerateRandomCharacter();
				var wrapped = new SubAccountGetCharacterSheet()
				{
					Account = PlayerManager.Account.Id,
					ForkCompatibility = CharacterSheetForkCompatibility,
					CharacterSheetVersion = CharacterSheetVersion,
					Data = sheet,
					LastUpdated = DateTime.Now
				};
				EnsureId(wrapped);
				Characters.Add(wrapped);
				return sheet;
			}

			if (Characters.Count == 0)
			{
				Loggy.Info("No character sheets found. Generating a new one..");
				return GenerateRandomSheet();
			}

			int index = IndexOfId(id);
			if (index != -1)
			{
				return Characters[index].Data;
			}

			if (id != 0)
			{
				Loggy.Error($"An attempt was made to fetch character id \"{id}\" which doesn't exist. Using the first character instead.");
			}

			return Characters[0].Data;
		}

		/// <summary>Set the <see cref="CharacterSheet"/> with the given id.</summary>
		/// <param name="id">Id of the updated <see cref="CharacterSheet"/>.</param>
		/// <param name="character"><see cref="CharacterSheet"/> to set.</param>
		public void Set(int id, CharacterSheet character)
		{
			int index = IndexOfId(id);
			if (index == -1)
			{
				Loggy.Warning($"An attempt was made to set character id \"{id}\" which doesn't exist. Ignoring.");
				return;
			}

			Characters[index].Data = character;

			Task.Run(() => UpdateCharacterOnline(Characters[index]));
			SaveCharacters();

			if (id == ActiveCharacterId)
			{
				NotifyActiveCharacterChanged();
			}
		}

		public async Task UpdateCharacterOnline(SubAccountGetCharacterSheet character)
		{
			if (IsRegisteredOnline(character) == false) return;

			LoadManager.DoInMainThread( () =>
			{
				Loggy.Info($"Updating character {character.Id} online.");
			});
			ApiResult<SubAccountGetCharacterSheet> response = await PersistenceServer.PutAccountsCharacterByID(character.Id, character, PlayerManager.Account.Token);

			if (!response.IsSuccess)
			{
				LoadManager.DoInMainThread( ()=>
				{
					Loggy.Error($"Failed to update character online. because: {response.Exception!.Message}");
				});
				//TODO: feedback to user
				return;
			}

			SubAccountGetCharacterSheet characters = response.Data;

			character.LastUpdated = characters!.LastUpdated;
			SaveCharacters();
		}


		/// <summary>Add a new <see cref="CharacterSheet"/>.</summary>
		/// <param name="character"><see cref="CharacterSheet"/> to add.</param>
		public void Add(CharacterSheet character)
		{
			if (ValidateCharacterSheet(character) == false)
			{
				LoadManager.DoInMainThread( ()=>
				{
					Loggy.Error(
						"An attempt was made to add a character but character validation failed. Ignoring.");
				});
				return;
			}

			var SubAccountGetcharacter = new SubAccountGetCharacterSheet()
			{
				Account = PlayerManager.Account.Id,
				ForkCompatibility = CharacterSheetForkCompatibility,
				CharacterSheetVersion = CharacterSheetVersion,
				Data = character
			};
			EnsureId(SubAccountGetcharacter);
			Characters.Add(SubAccountGetcharacter);
			Task.Run(() => SaveNewCharacterTask(SubAccountGetcharacter));
			SaveCharacters();
		}


		public void Add(SubAccountGetCharacterSheet character, bool AddOnline = true)
		{
			if (ValidateCharacterSheet(character.Data) == false)
			{
				LoadManager.DoInMainThread( ()=>
				{
					Loggy.Error(
						"An attempt was made to add a character but character validation failed. Ignoring.");
				});
				return;
			}

			EnsureId(character);
			Characters.Add(character);
			if (AddOnline)
			{
				Task.Run(() => SaveNewCharacterTask(character));
			}

			SaveCharacters();
		}


		public async Task SaveNewCharacterTask(SubAccountGetCharacterSheet character)
		{
			ApiResult<SubAccountGetCharacterSheet> response = await PersistenceServer.PostMakeAccountsCharacter(character, PlayerManager.Account.Token);
			if (response.IsSuccess == false)
			{
				LoadManager.DoInMainThread(() =>
				{
					Loggy.Error($"Failed to save new character online. because: {response.Exception!.Message}");
				});
				return;
			}

			SubAccountGetCharacterSheet characterSheet = response.Data;

			int localId = character.Id;
			character.Id = characterSheet!.Id;
			SaveCharacters();
			LoadManager.DoInMainThread(() => ReplaceCharacterId(localId, character.Id));
		}

		/// <summary>Remove the <see cref="CharacterSheet"/> with the given id.</summary>
		/// <param name="id">Id of the <see cref="CharacterSheet"/> to be removed.</param>
		public void Remove(int id)
		{
			int index = IndexOfId(id);
			if (index == -1)
			{
				Loggy.Error($"An attempt was made to remove character id \"{id}\" which doesn't exist. Ignoring.");
				return;
			}

			var characterRemove = Characters[index];
			Characters.RemoveAt(index);
			if (IsRegisteredOnline(characterRemove))
			{
				_ = PersistenceServer.DeleteAccountsCharacterByID(characterRemove.Id, PlayerManager.Account.Token);
			}
			SaveCharacters();

			if (ActiveCharacterId == id && Characters.Count > 0)
			{
				int fallback = Math.Clamp(index - 1, 0, Characters.Count - 1);
				SetActiveCharacter(Characters[fallback].Id);
				SetLastCharacter(Characters[fallback].Id);
			}
		}

		public async Task LoadOnlineCharacters()
		{
			if (CustomNetworkManager.IsHeadless) return;
			try
			{
				ApiResult<AccountGetCharacterSheets> accountResponse =
					await PersistenceServer.GetAccountsCharacters(CharacterSheetForkCompatibility, CharacterSheetVersion, PlayerManager.Account.Token);

				if (!accountResponse.IsSuccess)
				{
					LoadManager.DoInMainThread(()=>
					{
						Loggy.Error(
							$"Failed to load characters online. because: {accountResponse.Exception!.Message}");
						UIManager.InfoWindow.Show("Failed to load characters online" +
						                          $"{accountResponse.Exception!.Message}",
							false, "Error");
					});
					if (accountResponse.Exception != null)
					{
						throw accountResponse.Exception;
					}
					else
					{
						throw new Exception("Failed to load characters online");
					}
				}
				else
				{
					LoadManager.DoInMainThread(()=>
					{
						var loadedCharacters = new StringBuilder();
						if (accountResponse.Data != null)
						{
							foreach (var sheet in accountResponse.Data.Results)
							{
								loadedCharacters.AppendLine($"[CharacterManager/LoadOnlineCharacters] {sheet.Id} - {sheet.Data.Name}");
							}
							Loggy.Info($"{loadedCharacters}");
						}
					});
				}

				AccountGetCharacterSheets characters = accountResponse.Data;
				Characters.AddRange(characters!.Results);
			}
			catch (Exception e)
			{
				LoadManager.DoInMainThread( ()=>
				{
					Loggy.Error(e.ToString());
					UIManager.InfoWindow.Show("Something went wrong while attempting to fetch your characters." +
					                          " Make sure you're online and you have a valid account token.",
						false, "Error");
				});
			}
		}


		/// <summary>Load characters that are saved to Unity's persistent data folder.</summary>
		public async Task LoadCharacters()
		{
			Characters.Clear();
			await LoadOnlineCharacters();
			if (Characters.Count == 0 && AccessFile.Exists(OfflineStoragePath, userPersistent: true))
			{
				List<SubAccountGetCharacterSheet> characters = new List<SubAccountGetCharacterSheet>();
				LoadOfflineCharacterSheets(ref characters);
				if (characters != null)
				{
					foreach (var character in characters)
					{
						Add(character, false);
					}
				}
			}
			DetermineActiveCharacter();
			GUI_PreRoundWindow.Instance?.OnClientLoadUpdateStatus?.Invoke("", "", 2f);
		}

		private void LoadOfflineCharacterSheets(ref List<SubAccountGetCharacterSheet> characters)
		{
			string json = AccessFile.Load(OfflineStoragePath, userPersistent: true);
			var old = false;
			try
			{
				characters = JsonConvert.DeserializeObject<List<SubAccountGetCharacterSheet>>(json);
				if (characters.Count == 0 || characters[0].Data == null)
				{
					old = true;
					characters.Clear();
				}
			}
			catch (Exception e)
			{
				Loggy.Error("OLD Characters detected porting");
				old = true;
			}

			if (old)
			{
				PortOldCharacterSheetsToNewVersion(ref characters, json);
			}
			else
			{
				characters = JsonConvert.DeserializeObject<List<SubAccountGetCharacterSheet>>(json);
			}
		}

		private void PortOldCharacterSheetsToNewVersion(ref List<SubAccountGetCharacterSheet> characters, string json)
		{
			var oldCharacters = JsonConvert.DeserializeObject<List<CharacterSheet>>(json);
			foreach (var oldCharacter in oldCharacters)
			{
				characters.Add(new SubAccountGetCharacterSheet()
				{
					Account = PlayerManager.Account.Id,
					ForkCompatibility = CharacterSheetForkCompatibility,
					CharacterSheetVersion = CharacterSheetVersion,
					Data = oldCharacter
				});
			}
		}

		/// <summary>Save characters to both the cloud and offline storage.</summary>
		public void SaveCharacters()
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