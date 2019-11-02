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

	/// <summary>
	/// Spawns the indicated cloth.
	/// </summary>
	/// <param name="ClothingData">data describing the cloth to spawn</param>
	/// <param name="worldPos"></param>
	/// <param name="parent"></param>
	/// <param name="CVT"></param>
	/// <param name="variant"></param>
	/// <param name="PrefabOverride">prefab to use instead of the default for this cloth type</param>
	/// <returns></returns>
	public static GameObject CreateCloth(BaseClothData clothData, Vector3? worldPos = null, Transform parent = null,
		ClothingVariantType CVT = ClothingVariantType.Default, int variant = -1, GameObject PrefabOverride = null)
	{
		if (clothData is HeadsetData headsetData)
		{
			return CreateHeadsetCloth(headsetData, worldPos, parent, CVT, variant, PrefabOverride);
		}
		else if (clothData is ContainerData containerData)
		{
			return CreateBackpackCloth(containerData, worldPos, parent, CVT, variant, PrefabOverride);
		}
		else if (clothData is ClothingData clothingData)
		{
			return CreateCloth(clothingData, worldPos, parent, CVT, variant, PrefabOverride);
		}
		else
		{
			Logger.LogErrorFormat("Unrecognize BaseClothData subtype {0}, please add logic" +
			                      " to ClothFactory to handle spawning this type.", Category.ItemSpawn,
				clothData.GetType().Name);
			return null;
		}
	}

	private static GameObject CreateHeadsetCloth(HeadsetData headsetData, Vector3? worldPos = null, Transform parent = null,
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

	private static GameObject CreateBackpackCloth(ContainerData ContainerData, Vector3? worldPos = null, Transform parent = null,
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
		_Clothing.SpriteInfo = StaticSpriteHandler.SetupSingleSprite(ContainerData.Sprites.Equipped);
		Item.SetUpFromClothingData(ContainerData.Sprites, ContainerData.ItemAttributes);
		_Clothing.SetSynchronise(ConD: ContainerData);
		return clothObj;
	}


	private static GameObject CreateCloth(ClothingData ClothingData, Vector3? worldPos = null, Transform parent = null,
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