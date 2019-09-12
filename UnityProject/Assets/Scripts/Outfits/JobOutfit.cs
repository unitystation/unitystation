using System.Collections.Generic;
using UnityEngine;

public class JobOutfit : MonoBehaviour
{
	public string accessory;
	public List<Access> allowedAccess;

	public BackpackOrPrefab backpack;

	public List<string> backpack_contents = new List<string>();
	public string belt;
	public string box;
	public string duffelbag;
	public HeadsetOrPrefab ears;

	public ClothOrPrefab glasses;
	public ClothOrPrefab gloves;
	public ClothOrPrefab head;
	public JobType jobType;

	public string l_hand;

	public string l_pocket;
	public ClothOrPrefab mask;
	public string r_pocket;
	public string satchel;
	public ClothOrPrefab shoes;
	public ClothOrPrefab suit;

	public ClothOrPrefab suit_store;
	public ClothOrPrefab uniform;
}

[System.Serializable]
public class ClothOrPrefab {	public ClothingData Clothing;
	public GameObject Prefab;

}

[System.Serializable]
public class BackpackOrPrefab
{
	public ContainerData Backpack;
	public GameObject Prefab;

}

[System.Serializable]
public class HeadsetOrPrefab
{
	public HeadsetData Headset;
	public GameObject Prefab;

}