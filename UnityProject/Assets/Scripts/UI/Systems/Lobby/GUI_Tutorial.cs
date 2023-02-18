using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;

public class GUI_Tutorial : MonoBehaviour
{
	// Add new file name to list when new languages are added
	private readonly string[] languageFileNames = new string[]
	{
		"Lang_Bot_English.xml",
		"Lang_Bot_French.xml"
	};
	private string[] languageFilePaths;
	private string languageFolderPath = Application.persistentDataPath + "/languages";

	public GameObject languageChoice;

	private void Start()
	{
		if (!Directory.Exists(languageFolderPath))
		{
			
			BuildLanguagesDirectory();
		}
	}

	private void BuildLanguagesDirectory()
	{
		Directory.CreateDirectory(languageFolderPath);

		CreateFilePaths();
	}

	private void CreateFilePaths()
	{
		languageFilePaths = new string[languageFileNames.Length];

		for (int i = 0; i < languageFileNames.Length; i++)
		{
			languageFilePaths[i] = Path.Combine(Application.streamingAssetsPath, languageFileNames[i]);
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
			Debug.Log($"Error while copying text from: {source} to: {destination}. {exception}");
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
