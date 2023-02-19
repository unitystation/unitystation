using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;

/// <summary>
/// This script loads language files into LocalLow\Unitystation\unitystation\languages so the XML files are available
/// in the final build and generates a button for each language found.
/// 
/// To add a new language, add the xml filename to <param>languageFileNames</param> array and save the xml file to
/// Unitystation-Tutorial\UnityProject\Assets\StreamingAssets\languages
/// </summary>

public class GUI_Tutorial : MonoBehaviour
{
	// Add new file name to list when new languages are added
	private readonly string[] languageFileNames = new string[]
	{
		"Lang_Bot_English.xml",
		"Lang_Bot_French.xml"
	};
	private string[] languageFilePaths;
	private string languageFolderPath;

	public GameObject languageChoice;

	private void Start()
	{
		languageFolderPath = Application.persistentDataPath + "/languages";

		if (!Directory.Exists(languageFolderPath))
		{
			BuildLanguagesDirectory();
			CreateFilePaths();
		}
	}

	private void BuildLanguagesDirectory()
	{
		Directory.CreateDirectory(languageFolderPath);
	}

	private void CreateFilePaths()
	{
		languageFilePaths = new string[languageFileNames.Length];

		for (int i = 0; i < languageFileNames.Length; i++)
		{
			languageFilePaths[i] = Path.Combine(Application.streamingAssetsPath + "/languages", languageFileNames[i]);
		}

		for (int i = 0; i < languageFileNames.Length; i++)
		{
			WriteFile(languageFilePaths[i], languageFileNames[i]);
		}
	}

	/// <summary>
	/// Copies contents of file sored at <paramref name="source"/> and pastes to <paramref name="destination"/>.
	/// </summary>
	/// <param name="source">The file to read from</param>
	/// <param name="destination">The file to write to</param>
	private void WriteFile(string source, string destination)
	{
		try
		{
			File.WriteAllText(Path.Combine(languageFolderPath, $"{destination}.xml"), File.ReadAllText(source));
		}
		catch (Exception exception)
		{
			Debug.Log($"[LANGUAGES] Error while copying text from: {source} to: {destination}. {exception}");
		}
	}

	public void OnTutorialButton(string choice)
    {
		///Start tutorial if directory languages exist
		if (Directory.Exists(languageFolderPath))
		{
            GameManager.Instance.onTuto = true;
            GameManager.Instance.language = choice;
            LoadingScreenManager.LoadFromLobby(CustomNetworkManager.Instance.StartHost);
        }
		else
		{
			Debug.LogError($"CAN'T FIND LANGUAGES: {languageFolderPath}");
		}
	}

    public void ChoiceLanguage()
    {
        languageChoice.SetActive(true);
    }
    public void ExitChoice()
    {
        languageChoice.SetActive(false);
    }
}
