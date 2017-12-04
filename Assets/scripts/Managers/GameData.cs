using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UI;

public class GameData : MonoBehaviour
{
    /// <summary>
    /// Check to see if you are in the game or in the lobby
    /// </summary>
    public static bool IsInGame { get; private set; }

    public static bool IsHeadlessServer { get; private set; }

    public bool testServer = false;
    private static GameData gameData;

    public static GameData Instance
    {
        get
        {
            if (!gameData)
            {
                gameData = FindObjectOfType<GameData>();
                gameData.Init();
            }

            return gameData;
        }
    }

    public bool IsTestMode
    {
        get
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.StartsWith("InitTestScene");
        }
    }

    void Init()
    {
        if (IsTestMode)
            return;

        Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");
        LoadData();
    }

    void ApplicationWillResignActive()
    {
        if (IsTestMode)
            return;

        SaveData();
    }

    void OnEnable()
    {
        if (IsTestMode)
            return;

        SceneManager.sceneLoaded += OnLevelFinishedLoading;
    }

    void OnDisable()
    {
        if (IsTestMode)
            return;

        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
        SaveData();
    }

    void OnApplicationQuit()
    {
        if (IsTestMode)
            return;

        SaveData();
    }

    void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Lobby")
        {
            IsInGame = false;
            Managers.instance.SetScreenForLobby();
        }
        else
        {
            IsInGame = true;
            Managers.instance.SetScreenForGame();
            SetPlayerPreferences();
        }

        if (CustomNetworkManager.Instance.isNetworkActive)
        {
            //Reset stuff
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null || Instance.testServer)
            {
                IsHeadlessServer = true;
            }
            if (IsInGame && GameManager.Instance != null)
            {
                GameManager.Instance.ResetRoundTime();
            }
            return;
        }
        //Check if running in batchmode (headless server)
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null || Instance.testServer)
        {
            Debug.Log("START SERVER HEADLESS MODE");
            IsHeadlessServer = true;
            StartCoroutine(WaitToStartServer());
            return;
        }
    }

    IEnumerator WaitToStartServer()
    {
        yield return new WaitForSeconds(0.1f);
        CustomNetworkManager.Instance.StartHost();
    }

    void LoadData()
    {
        Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");
        if (File.Exists(Application.persistentDataPath + "/genData01.dat"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/genData01.dat", FileMode.Open);
            UserData data = (UserData)bf.Deserialize(file);
            //DO SOMETHNG WITH THE VALUES HERE, I.E STORE THEM IN A CACHE IN THIS CLASS
            //TODO: LOAD SOME STUFF

            file.Close();
        }
    }

    void SaveData()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/genData01.dat");
        UserData data = new UserData();
        /// PUT YOUR MEMBER VALUES HERE, ADD THE PROPERTY TO USERDATA CLASS AND THIS WILL SAVE IT

        //TODO: SAVE SOME STUFF
        bf.Serialize(file, data);
        file.Close();
    }

    void SetPlayerPreferences()
    {
        //Ambient Volume
        if (PlayerPrefs.HasKey("AmbientVol"))
        {
            SoundManager.Instance.ambientTracks[SoundManager.Instance.ambientPlaying].volume = PlayerPrefs.GetFloat("AmbientVol");
        }

    }
}

[Serializable]
class UserData
{
    //TODO: add your members here

}
