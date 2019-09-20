using System.Collections.Generic;
using UnityEngine;
using Mirror;
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

	public Dictionary<PlayerCustomisation, Dictionary<string, PlayerCustomisationData>> playerCustomisationData =
		new Dictionary<PlayerCustomisation, Dictionary<string, PlayerCustomisationData>>();

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

	public static GameObject CreateHeadsetCloth(HeadsetData headsetData, Vector3 worldPos, Transform parent = null,
		ClothingVariantType CVT = ClothingVariantType.Default, int variant = -1, GameObject PrefabOverride = null)
	{
		if (Instance.uniHeadSet == null)
		{
			Logger.LogError("UniHeadSet Prefab not found", Category.SpriteHandler);
			return null;
		}

		GameObject clothObj;

		if (PrefabOverride != null)
		{
			clothObj = PoolManager.PoolNetworkInstantiate(PrefabOverride, worldPos, parent);
		}
		else
		{
			clothObj = PoolManager.PoolNetworkInstantiate(Instance.uniHeadSet, worldPos, parent);
		}

		var _Clothing = clothObj.GetComponent<Clothing>();
		var Item = clothObj.GetComponent<ItemAttributes>();
		var Headset = clothObj.GetComponent<Headset>();
		_Clothing.SpriteInfo = StaticSpriteHandler.SetupSingleSprite(headsetData.Sprites.Equipped);
		_Clothing.SetSynchronise(HD: headsetData);
		Item.SetUpFromClothingData(headsetData.Sprites, headsetData.ItemAttributes);
		Headset.EncryptionKey = headsetData.Key.EncryptionKey;
		return clothObj;
	}

	public static GameObject CreateBackpackCloth(ContainerData ContainerData, Vector3 worldPos, Transform parent = null,
		ClothingVariantType CVT = ClothingVariantType.Default, int variant = -1, GameObject PrefabOverride = null)
	{
		if (Instance.uniBackpack == null)
		{
			Logger.LogError("UniBackPack Prefab not found", Category.SpriteHandler);
			return null;
		}

		GameObject clothObj;
		if (PrefabOverride != null)
		{
			clothObj = PoolManager.PoolNetworkInstantiate(PrefabOverride, worldPos, parent);
		}
		else
		{
			clothObj = PoolManager.PoolNetworkInstantiate(Instance.uniBackpack, worldPos, parent);
		}

		var _Clothing = clothObj.GetComponent<Clothing>();
		var Item = clothObj.GetComponent<ItemAttributes>();
		var Storage = clothObj.GetComponent<StorageObject>();
		_Clothing.SpriteInfo = StaticSpriteHandler.SetupSingleSprite(ContainerData.Sprites.Equipped);
		Item.SetUpFromClothingData(ContainerData.Sprites, ContainerData.ItemAttributes);
		_Clothing.SetSynchronise(ConD: ContainerData);
		Storage.SetUpFromStorageObjectData(ContainerData.StorageData);
		return clothObj;
	}


	public static GameObject CreateCloth(ClothingData ClothingData, Vector3 worldPos, Transform parent = null,
		ClothingVariantType CVT = ClothingVariantType.Default, int variant = -1, GameObject PrefabOverride = null)
	{
		if (Instance.uniCloth == null)
		{
			Logger.LogError("UniCloth Prefab not found", Category.SpriteHandler);
			return null;
		}

		GameObject clothObj;
		if (PrefabOverride != null)
		{
			clothObj = PoolManager.PoolNetworkInstantiate(PrefabOverride, worldPos, parent);
		}
		else
		{
			clothObj = PoolManager.PoolNetworkInstantiate(Instance.uniCloth, worldPos, parent);
		}

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
}