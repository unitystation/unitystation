﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using DatabaseAPI;

namespace Systems.Character
{
	/// <summary>Manage a player's characters. Intended for the local player client.</summary>
	// TODO this class has stubs
	public class CharacterManager
	{
		public List<CharacterSheet> Characters { get; } = new();

		private string OfflineStoragePath => $"{Application.persistentDataPath}characters.json";

		public CharacterManager Init()
		{
			LoadOfflineCharacters();

			return this;
		}

		public int GetCurrentCharacterIndex()
		{
			int lastCharacterIndex = PlayerPrefs.GetInt(PlayerPrefKeys.LastCharacterIndex);

			return Math.Clamp(lastCharacterIndex, 0, Characters.Count);
		}

		/// <summary>Remember which character the player last selected.</summary>
		public void SetCurrentCharacterIndex(int index)
		{
			index = Math.Clamp(index, 0, Characters.Count);

			PlayerPrefs.SetInt(PlayerPrefKeys.LastCharacterIndex, index);
			PlayerPrefs.Save();
		}

		/// <summary>Load characters that have been saved to the cloud.</summary>
		public void LoadOnlineCharacters()
		{
			throw new NotImplementedException();
		}

		/// <summary>Load characters that are saved to Unity's persistent data folder.</summary>
		public void LoadOfflineCharacters()
		{
			Characters.Clear();

			if (File.Exists(OfflineStoragePath) == false) return;

			string json = File.ReadAllText(OfflineStoragePath);

			var characters = JsonConvert.DeserializeObject<List<CharacterSheet>>(json);

			if (characters == null) return;

			foreach (var character in characters)
			{
				if (ValidateCharacterSheet(character))
				{
					Characters.Add(character);
				}
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
			_ = ServerData.UpdateCharacterProfile(Characters[GetCurrentCharacterIndex()]);
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

			if (File.Exists(OfflineStoragePath))
			{
				File.Delete(OfflineStoragePath);
			}

			File.WriteAllText(OfflineStoragePath, json);
		}

		public bool ValidateCharacterSheet(CharacterSheet character)
		{
			// TODO: not implemented
			return true;
		}
	}
}
