using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SecureStuff;
using Shared.Managers;
using UnityEngine;
using US13.Core.Initialisation;
using US13.PlayerPrefs;

namespace US13.Core.TranslationSystem
{
	public class TranslationManager : SingletonManager<TranslationManager>, IInitialise
	{
		public InitialisationSystems Subsystem => InitialisationSystems.TranslationSystem;

		public void Initialise()
		{

			if (Application.isBatchMode) return;

			string CurrentLanguage = Application.systemLanguage.ToString();

			if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.LanguagePreference))
			{
				var PreferenceLanguage = UnityEngine.PlayerPrefs.GetString(PlayerPrefKeys.LanguagePreference, "English");
				if (PreferenceLanguage != "System")
				{
					CurrentLanguage = PreferenceLanguage;
				}
			}

			TranslationSystem.AvailableLanguages = SecureStuff.AccessFile.DirectoriesOrFilesIn("", FolderType.Translation).Select(x => x.Replace(".json", "")).ToList();
			TranslationSystem.AvailableLanguages.Add("English");
			TranslationSystem.AvailableLanguages.Add("System");
			//CurrentLanguage = "Welsh";


			if (CurrentLanguage == "English")
			{
				TranslationSystem.English = true;
				return;
			}
			else
			{
				TranslationSystem.English = false;
			}

			var LanguageData = SecureStuff.AccessFile.Load($"{CurrentLanguage}.json", FolderType.Translation);

			if (string.IsNullOrEmpty(LanguageData))
			{
				return;
			}

			TranslationSystem.LoadedLanguage = JsonConvert.DeserializeObject<Dictionary<string, string>>(LanguageData);

		}

	}
}
