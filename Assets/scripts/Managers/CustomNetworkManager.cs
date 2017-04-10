using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;
using PlayGroup;
using UnityEngine.SceneManagement;

public class CustomNetworkManager: NetworkManager
{
    public static CustomNetworkManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    void Start(){
        if (!IsClientConnected())
        {
            UIManager.Display.logInWindow.SetActive(true);   
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnLevelFinishedLoading;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        //This client connecting to server
        base.OnClientConnect(conn);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        PlayerList.Instance.RemovePlayer(conn.playerControllers[0].gameObject.name);
    }

    void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        if (IsClientConnected())
        {
            //make sure login window does not show on scene changes if connected
            UIManager.Display.logInWindow.SetActive(false);
        }
        else
        {
            UIManager.Display.logInWindow.SetActive(true);
        }
    }
   
}