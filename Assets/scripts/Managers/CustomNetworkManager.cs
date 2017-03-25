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

    public override void OnClientConnect(NetworkConnection conn)
    {
        //This client connecting to server
        base.OnClientConnect(conn);
    }

   
}