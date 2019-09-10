using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;


/// <summary>
/// Handles logic related to unicloths, which require special logic for instantiation because
/// they all share the same base prefabs and only get their unique appearance by passing a hier string
/// to the ItemAttributes behavior.
/// </summary>
public class ClothFactory : NetworkBehaviour
{
	public static ClothFactory Instance;


	public GameObject uniCloth;
	public GameObject uniBackpack;
	public GameObject uniHeadSet;

	public Dictionary<string, PlayerTextureData> RaceData = new Dictionary<string, PlayerTextureData>();
	public Dictionary<string, ClothingData> ClothingStoredData = new Dictionary<string, ClothingData>();
	public Dictionary<string, ContainerData> BackpackStoredData = new Dictionary<string, ContainerData>();
	public Dictionary<string, HeadsetData> HeadSetStoredData = new Dictionary<string, HeadsetData>();
	public Dictionary<PlayerCustomisation, Dictionary<string, PlayerCustomisationData>> playerCustomisationData = new Dictionary<PlayerCustomisation, Dictionary<string, PlayerCustomisationData>>();


	private void Awake()
	{
		//Instance.uniCloth = null;  //In case of not being able to find the prefab //oh yeah its on Pool manager
		//Logger.Log(Instance.uniCloth.name)s;
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(this);
		}

	}



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

	public static GameObject CreateHeadsetCloth(HeadsetData headsetData, Vector3 worldPos, Transform parent = null, ClothingVariantType CVT = ClothingVariantType.Default, int variant = -1, GameObject PrefabOverride = null)
	{

		if (Instance.uniHeadSet == null)
		{
			Logger.Log("oh no!");
		}
		Logger.Log(headsetData.name);
		GameObject clothObj;
		if (PrefabOverride != null)
		{ clothObj = PoolManager.PoolNetworkInstantiate(PrefabOverride, worldPos, parent); }
		else { clothObj = PoolManager.PoolNetworkInstantiate(Instance.uniHeadSet, worldPos, parent); }

		var _Clothing = clothObj.GetComponent<Clothing>();
		var Item = clothObj.GetComponent<ItemAttributes>();
		var Headset = clothObj.GetComponent<Headset>();
		_Clothing.SpriteInfo = StaticSpriteHandler.SetupSingleSprite(headsetData.Sprites.Equipped);
		_Clothing.SetSynchronise(HD : headsetData);
		Item.SetUpFromClothingData(headsetData.Sprites, headsetData.ItemAttributes);
		Headset.EncryptionKey = headsetData.Key.EncryptionKey;
		return clothObj;
	}

	public static GameObject CreateBackpackCloth(ContainerData ContainerData, Vector3 worldPos, Transform parent = null, ClothingVariantType CVT = ClothingVariantType.Default, int variant = -1, GameObject PrefabOverride = null)
	{

		if (Instance.uniBackpack == null)
		{
			Logger.Log("oh no!");
		}
		Logger.Log(ContainerData.name);
		GameObject clothObj;
		if (PrefabOverride != null)
		{ clothObj = PoolManager.PoolNetworkInstantiate(PrefabOverride, worldPos, parent); }
		else { clothObj = PoolManager.PoolNetworkInstantiate(Instance.uniBackpack, worldPos, parent); }

		var _Clothing = clothObj.GetComponent<Clothing>();
		var Item = clothObj.GetComponent<ItemAttributes>();
		var Storage = clothObj.GetComponent<StorageObject>();
		_Clothing.SpriteInfo = StaticSpriteHandler.SetupSingleSprite(ContainerData.Sprites.Equipped);
		Item.SetUpFromClothingData(ContainerData.Sprites, ContainerData.ItemAttributes);
		_Clothing.SetSynchronise(ConD: ContainerData);
		Storage.SetUpFromStorageObjectData(ContainerData.StorageData);
		return clothObj;
	}


	public static GameObject CreateCloth(ClothingData ClothingData, Vector3 worldPos, Transform parent = null, ClothingVariantType CVT = ClothingVariantType.Default, int variant = -1, GameObject PrefabOverride = null)
	{

		if (Instance.uniCloth == null)
		{
			Logger.Log("oh no!");
		}
		Logger.Log(ClothingData.name);
		GameObject clothObj;
		if (PrefabOverride != null)
		{ clothObj = PoolManager.PoolNetworkInstantiate(PrefabOverride, worldPos, parent); }
		else { clothObj = PoolManager.PoolNetworkInstantiate(Instance.uniCloth, worldPos, parent); }

		var _Clothing = clothObj.GetComponent<Clothing>();
		var Item = clothObj.GetComponent<ItemAttributes>();
		_Clothing.SpriteInfo = StaticSpriteHandler.SetUpSheetForClothingData(ClothingData, _Clothing);
		_Clothing.SetSynchronise(CD: ClothingData);
		Item.SetUpFromClothingData(ClothingData.Base, ClothingData.ItemAttributes);
		switch (CVT)
		{
			case ClothingVariantType.Default:
				if (variant > -1)
				{
					if (!(ClothingData.Variants.Count >= variant))
					{
						Item.SetUpFromClothingData(ClothingData.Variants[variant], ClothingData.ItemAttributes);
					}
				}
				break;
			case ClothingVariantType.Skirt:
				Item.SetUpFromClothingData(ClothingData.DressVariant, ClothingData.ItemAttributes);
				break;
			case ClothingVariantType.Tucked:
				Item.SetUpFromClothingData(ClothingData.Base_Adjusted, ClothingData.ItemAttributes);
				break;

		}
		clothObj.name = ClothingData.name;
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