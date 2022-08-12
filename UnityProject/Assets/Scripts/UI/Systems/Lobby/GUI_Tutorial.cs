using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class GUI_Tutorial : MonoBehaviour
{
    public GameObject languageChoice;
    public void OnTutorialButton(string choice)
    {
        ///Start tutorial if directory languages exist
        if(Directory.Exists(Application.persistentDataPath + "/languages"))
        {
            GameManager.Instance.onTuto = true;
            GameManager.Instance.language = choice;
            LoadingScreenManager.LoadFromLobby(CustomNetworkManager.Instance.StartHost);
        }
        else
        {
            Debug.LogError("CAN'T FIND LANGUAGES");
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
