using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Handles logic related to unicloths, which require special logic for instantiation because
/// they all share the same base prefabs and only get their unique appearance by passing a hier string
/// to the ItemAttributes behavior.
/// </summary>
public class ClothFactory : NetworkBehaviour
{
	//private static ClothFactory Instance;


	public static GameObject uniCloth;
	//private GameObject uniHeadSet { get; set; }
	//private GameObject uniBackPack { get; set; }

	//private void Awake()
	//{
	//	if (Instance == null)
	//	{
	//		Instance = this;
	//	}
	//	else
	//	{
	//		Destroy(this);
	//	}
	//}

	//private void Start()
	//{
	//	//Do init stuff
	//	uniCloth = Resources.Load("UniCloth") as GameObject;
	//	uniHeadSet = Resources.Load("UniHeadSet") as GameObject;
	//	uniBackPack = Resources.Load("UniBackPack") as GameObject;
	//}

	//private static bool IsInstanceInit()
	//{
	//	if (Instance == null)
	//	{
	//		Logger.LogError("ClothFactory was attempted to be used before it has initialized. Please delay using" +
	//		                " ClothFactory (such as by using a coroutine to wait) until it is initialized. Nothing will" +
	//		                " be done and null will be returned.", Category.PlayerSprites);
	//		return false;
	//	}

	//	return true;
	//}

	//public static void PreLoadCloth(int preLoads)
	//{
	//	if (!IsInstanceInit())
	//	{
	//		return;
	//	}
	//	for (int i = 0; i < preLoads; i++)
	//	{
	//		PoolManager.PoolNetworkPreLoad(Instance.uniCloth);
	//		PoolManager.PoolNetworkPreLoad(Instance.uniHeadSet);
	//	}
	//}


	public static GameObject CreateCloth(ClothingData ClothingData, Vector3 worldPos, Transform parent=null)
	{

		var clothObj = PoolManager.PoolNetworkInstantiate(uniCloth, worldPos, parent);
		var Clothing = clothObj.GetComponent<clothing>();
		//Clothing.SpriteInfo = StaticSpriteHandler.SetUpSheetForClothingData(ClothingData, Clothing);

		return clothObj;
	}

	/// <summary>
	/// Returns the cloth prefab that should be used for spawning the object with the specified hier
	/// </summary>
	/// <param name="hierarchy"></param>
	///// <returns></returns>
	//public static GameObject GetClothPrefabForHier(string hierarchy)
	//{
	//	if (hierarchy.Contains(UniItemUtils.ClothingHierIdentifier))
	//	{
	//		return Instance.uniCloth;
	//	}
	//	if (hierarchy.Contains(UniItemUtils.HeadsetHierIdentifier))
	//	{
	//		return Instance.uniHeadSet;
	//	}
	//	if (hierarchy.Contains(UniItemUtils.BackPackHierIdentifier) || hierarchy.Contains(UniItemUtils.BagHierIdentifier))
	//	{
	//		return Instance.uniBackPack;
	//	}
	//	Logger.LogError("Cloth factory could not pick uni item. Falling back to uniCloth", Category.DmMetadata);
	//	return Instance.uniCloth;
	//}
}