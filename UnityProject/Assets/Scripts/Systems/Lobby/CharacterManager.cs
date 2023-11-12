using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SecureStuff;
using Newtonsoft.Json;
using UnityEngine;
using DatabaseAPI;
using Logs;

namespace Systems.Character
{
	/// <summary>Manage a player's characters. Intended for the local player client.</summary>
	// TODO this class has stubs
	public class CharacterManager
	{
		/// <summary>
		/// A list of the player's loaded characters.
		/// Please consider using <see cref="CharacterManager"/>'s methods to manipulate the list instead of directly.
		/// </summary>
		public List<CharacterSheet> Characters { get; } = new();

		/// <summary>Get the key associated with the active character (the character the rest of the game should use).</summary>
		public int ActiveCharacterKey { get; private set; } = 0;

		/// <summary>Get the active character (the character the rest of the game should use).</summary>
		public CharacterSheet ActiveCharacter => Get(ActiveCharacterKey);

		private string OfflineStoragePath => $"characters.json";

		public void Init()
		{
			LoadOfflineCharacters();
			DetermineActiveCharacter();
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
				Loggy.LogError("An attempt was made to set the active character with a key that doesn't exist. Ignoring.");
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
				Loggy.LogError("An attempt was made to set the active character with a key that doesn't exist. Ignoring.");
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
				return default;
			}

			return Characters[key];
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

			Characters[key] = character;
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

			Characters.Add(character);
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
				Loggy.LogWarning($"An attempt was made to remove the last character with key \"{key}\". Ignoring as there should be at least one character.");
				return;
			}

			if (ActiveCharacterKey == key)
			{
				SetActiveCharacter(key - 1);
				SetLastCharacterKey(key - 1);
			}

			Characters.RemoveAt(key);
		}

		/// <summary>Load characters that have been saved to the cloud.</summary>
		public void LoadOnlineCharacters()
		{
			throw new NotImplementedException();
		}

		private string OLDOfflineStoragePath => $"{Application.persistentDataPath}characters.json";


		/// <summary>Load characters that are saved to Unity's persistent data folder.</summary>
		public void LoadOfflineCharacters()
		{

			Characters.Clear();
			if (AccessFile.Exists(OfflineStoragePath, userPersistent: true) == false)
			{
				return;
			}

			string json = AccessFile.Load(OfflineStoragePath, userPersistent: true);

			var characters = JsonConvert.DeserializeObject<List<CharacterSheet>>(json);

			if (characters == null) return;

			foreach (var character in characters)
			{
				Add(character);
			}
		}

		/// <summary>Save characters to both the cloud and offline storage.</summary>
		public void SaveCharacters()
		{
			SaveCharactersOffline();
			SaveCharactersOnline();
		}

		/// <summary>Save characters to the cloud.</summary>
		public void SaveCharactersOnline()
		{
			// TODO support multiple characters
			_ = ServerData.UpdateCharacterProfile(Get(GetLastCharacterKey()));
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
