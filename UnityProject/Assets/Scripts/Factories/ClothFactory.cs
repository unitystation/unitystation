using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;

public class ClothFactory : NetworkBehaviour
{
    public static ClothFactory Instance;

	public static string ClothingHierIdentifier = "cloth";
	public static string HeadsetHierIdentifier = "headset";

    private GameObject uniCloth { get; set; }
    private GameObject uniHeadSet { get; set; }

    void Awake(){
        if(Instance == null){
            Instance = this;
        } else {
            Destroy(this);
        }
    }

    void Start()
    {
        //Do init stuff
        uniCloth = Resources.Load("UniCloth") as GameObject;
		uniHeadSet = Resources.Load("UniHeadSet") as GameObject;
	}

    public void PreLoadCloth(int preLoads)
    {
        for (int i = 0; i < preLoads; i++)
        {
            PoolManager.Instance.PoolNetworkPreLoad(Instance.uniCloth);
			PoolManager.Instance.PoolNetworkPreLoad(Instance.uniHeadSet);
		}
    }

    //TODO is it going to be spawned on a player in equipment etc?
    public GameObject CreateCloth(string hierString, Vector3 spawnPos)
    {
        if (!CustomNetworkManager.Instance._isServer)
		{
			return null;
		}

		//PoolManager handles networkspawn
		GameObject uniItem = pickClothObject(hierString);
		GameObject clothObj = PoolManager.Instance.PoolNetworkInstantiate(uniItem, spawnPos, Quaternion.identity);
        ItemAttributes i = clothObj.GetComponent<ItemAttributes>();
        i.hierarchy = hierString;
		if(uniItem == uniHeadSet)
		{
			Headset headset = clothObj.GetComponent<Headset>();
			headset.init();
		}
        return clothObj;
    }

	private GameObject pickClothObject(string hierarchy)
	{
		if(hierarchy.Contains(ClothingHierIdentifier))
		{
			return uniCloth;
		} else if (hierarchy.Contains(HeadsetHierIdentifier))
		{
			return uniHeadSet;
		} else
		{
			Debug.LogError("Clot factory could not pick uni item. Falling back to uniCloth");
			return uniCloth;
		}
	}
}
