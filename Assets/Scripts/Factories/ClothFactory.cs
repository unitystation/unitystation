using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;

public class ClothFactory : NetworkBehaviour
{

    private static ClothFactory clothFactory;
    public static ClothFactory Instance
    {
        get
        {
            if (clothFactory == null)
            {
                clothFactory = FindObjectOfType<ClothFactory>();
                Instance.Init();
            }
            return clothFactory;
        }
    }

    private GameObject uniCloth { get; set; }

    void Init()
    {
        //Do init stuff
        Instance.uniCloth = Resources.Load("UniCloth") as GameObject;
    }

    public static void PreLoadCloth(int preLoads)
    {
        for (int i = 0; i < preLoads; i++)
        {
            PoolManager.PoolNetworkPreLoad(Instance.uniCloth);
        }
    }

    //TODO is it going to be spawned on a player in equipment etc?
    public static GameObject CreateCloth(string hierString, Vector3 spawnPos)
    {
        if (!CustomNetworkManager.Instance._isServer)
            return null;

        //PoolManager handles networkspawn
        GameObject clothObj = PoolManager.PoolNetworkInstantiate(Instance.uniCloth, spawnPos, Quaternion.identity);
        ItemAttributes i = clothObj.GetComponent<ItemAttributes>();
        i.hierarchy = hierString;
        return clothObj;
    }
}
