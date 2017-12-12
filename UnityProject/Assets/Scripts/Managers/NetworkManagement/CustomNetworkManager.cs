using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UI;
using UnityEngine.SceneManagement;
using Tilemaps.Scripts.Behaviours.Layers;

public class CustomNetworkManager : NetworkManager
{
    public static CustomNetworkManager Instance;
    [HideInInspector] public bool _isServer = false;
    [HideInInspector] public bool spawnableListReady = false;

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

    void Start()
    {
        customConfig = true;

        SetSpawnableList();
        if (!IsClientConnected() && !GameData.IsHeadlessServer)
        {
            UIManager.Display.logInWindow.SetActive(true);
        }

        channels.Add(QosType.ReliableSequenced);

        connectionConfig.AcksType = ConnectionAcksType.Acks64;
        connectionConfig.FragmentSize = 512;
    }

    void SetSpawnableList()
    {
        spawnPrefabs.Clear();

        var networkObjects = Resources.LoadAll<NetworkIdentity>("");
        foreach (var netObj in networkObjects)
        {
            if (!netObj.gameObject.name.Contains("Player"))
            {
                spawnPrefabs.Add(netObj.gameObject);
            }
        }

        var dirs = Directory.GetDirectories(Application.dataPath, "Resources", SearchOption.AllDirectories);


        foreach (string dir in dirs)
        {
            loadFolder(dir);
            foreach (var subdir in Directory.GetDirectories(dir, "*", SearchOption.AllDirectories))
            {
                loadFolder(subdir);
            }
        }

        spawnableListReady = true;
    }

    private void loadFolder(string folderpath)
    {
        folderpath = folderpath.Substring(folderpath.IndexOf("Resources", StringComparison.Ordinal) + "Resources".Length);
        foreach (var netObj in Resources.LoadAll<NetworkIdentity>(folderpath))
        {
            if (!netObj.name.Contains("Player") && !spawnPrefabs.Contains(netObj.gameObject))
            {
                spawnPrefabs.Add(netObj.gameObject);
            }
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

    public override void OnStartServer()
    {
        _isServer = true;
        base.OnStartServer();
        this.RegisterServerHandlers();
    }

    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        //This spawns the player prefab
        if (GameData.IsHeadlessServer || GameData.Instance.testServer)
        {
            //this is a headless server || testing headless (it removes the server player for localClient)
            if (conn.address != "localClient")
            {
                StartCoroutine(WaitToSpawnPlayer(conn, playerControllerId));
            }
        }
        else
        {
            //This is a host server (keep the server player as it is for the host player)
            StartCoroutine(WaitToSpawnPlayer(conn, playerControllerId));
        }
    }

    IEnumerator WaitToSpawnPlayer(NetworkConnection conn, short playerControllerId)
    {
        yield return new WaitForSeconds(1f);
        OnServerAddPlayerInternal(conn, playerControllerId);
    }

    private void OnServerAddPlayerInternal(NetworkConnection conn, short playerControllerId)
    {
        if ((UnityEngine.Object)playerPrefab == (UnityEngine.Object)null)
        {
            if (!LogFilter.logError)
                return;
            Debug.LogError((object)"The PlayerPrefab is empty on the NetworkManager. Please setup a PlayerPrefab object.");
        }
        else if ((UnityEngine.Object)playerPrefab.GetComponent<NetworkIdentity>() == (UnityEngine.Object)null)
        {
            if (!LogFilter.logError)
                return;
            Debug.LogError((object)"The PlayerPrefab does not have a NetworkIdentity. Please add a NetworkIdentity to the player prefab.");
        }
        else if ((int)playerControllerId < conn.playerControllers.Count && conn.playerControllers[(int)playerControllerId].IsValid && (UnityEngine.Object)conn.playerControllers[(int)playerControllerId].gameObject != (UnityEngine.Object)null)
        {
            if (!LogFilter.logError)
                return;
            Debug.LogError((object)"There is already a player at that playerControllerId for this connections.");
        }
        else
        {
            SpawnHandler.SpawnPlayer(conn, playerControllerId);
        }
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        if (_isServer)
        {
            //do special server wizardry here
        }
        else
        {
            this.RegisterServerHandlers(); //ghetto fix for IDs to match
        }

        if (GameData.IsInGame)
        {
            ObjectManager.StartPoolManager();
        }

        //This client connecting to server, wait for the spawnable prefabs to register
        StartCoroutine(WaitForSpawnListSetUp(conn));
        this.RegisterClientHandlers(conn);
    }

    IEnumerator WaitForSpawnListSetUp(NetworkConnection conn)
    {
        while (!spawnableListReady)
        {
            yield return new WaitForSeconds(1);
        }

        base.OnClientConnect(conn);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        if (conn != null && conn.playerControllers.Count > 0)
        {
            PlayerList.Instance.RemovePlayer(conn.playerControllers[0].gameObject.name);
            //TODO DROP ALL HIS OBJECTS
            Debug.Log("PlayerDisconnected: " + conn.playerControllers[0].gameObject.name);
            NetworkServer.Destroy(conn.playerControllers[0].gameObject);
        }
    }

    void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        if (GameData.IsInGame)
        {
            ObjectManager.StartPoolManager();
        }

        if (IsClientConnected())
        {
            //make sure login window does not show on scene changes if connected
            UIManager.Display.logInWindow.SetActive(false);
            StartCoroutine(DoHeadlessCheck());
        }
        else
        {
            StartCoroutine(DoHeadlessCheck());
        }
    }

    IEnumerator DoHeadlessCheck()
    {
        yield return new WaitForSeconds(0.1f);
        if (!GameData.IsHeadlessServer && !GameData.Instance.testServer)
        {
            if (!IsClientConnected())
                UIManager.Display.logInWindow.SetActive(true);
        }
        else
        {
            //Set up for headless mode stuff here
            //Useful for turning on and off components
            _isServer = true;
        }
    }
}
