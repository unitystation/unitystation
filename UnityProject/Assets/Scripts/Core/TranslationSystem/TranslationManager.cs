using System.Collections.Generic;
using Initialisation;
using Newtonsoft.Json;
using SecureStuff;
using UnityEngine;

public class TranslationManager : MonoBehaviour, IInitialise
{
	public InitialisationSystems Subsystem => InitialisationSystems.TranslationSystem;

	public void Initialise()
	{

		if (Application.isBatchMode) return;

		string CurrentLanguage = Application.systemLanguage.ToString();

		if (PlayerPrefs.HasKey(PlayerPrefKeys.LanguagePreference))
		{
			CurrentLanguage = PlayerPrefs.GetString(CurrentLanguage, "English");
		}

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
