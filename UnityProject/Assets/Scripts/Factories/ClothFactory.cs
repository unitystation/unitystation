using UnityEngine;
using UnityEngine.Networking;

public class ClothFactory : NetworkBehaviour
{
	public static ClothFactory Instance;

	public static string ClothingHierIdentifier = "cloth";
	public static string HeadsetHierIdentifier = "headset";

	private GameObject uniCloth { get; set; }
	private GameObject uniHeadSet { get; set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(this);
		}
	}

	private void Start()
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
	public GameObject CreateCloth(string hierString, Vector3 spawnPos, Transform parent)
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			return null;
		}

		//PoolManager handles networkspawn
		GameObject uniItem = pickClothObject(hierString);
		GameObject clothObj = ItemFactory.SpawnItem(uniItem, spawnPos, parent);
		ItemAttributes i = clothObj.GetComponent<ItemAttributes>();
		i.hierarchy = hierString;
		if (uniItem == uniHeadSet)
		{
			Headset headset = clothObj.GetComponent<Headset>();
			headset.init();
		}
		return clothObj;
	}

	private GameObject pickClothObject(string hierarchy)
	{
		if (hierarchy.Contains(ClothingHierIdentifier))
		{
			return uniCloth;
		}
		if (hierarchy.Contains(HeadsetHierIdentifier))
		{
			return uniHeadSet;
		}
		Debug.LogError("Clot factory could not pick uni item. Falling back to uniCloth");
		return uniCloth;
	}
}