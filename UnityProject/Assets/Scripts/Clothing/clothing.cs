using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class Clothing : NetworkBehaviour
{

	public ClothingVariantType Type;
	public int Variant = -1;

	public ClothingData clothingData;
	public ContainerData containerData;
	public HeadsetData headsetData;

	public bool Initialised;

	[HideInInspector]
	[SyncVar(hook = nameof(SyncFindData))]
	public string SynchronisedString;




	public Dictionary<ClothingVariantType, int> VariantStore = new Dictionary<ClothingVariantType, int>();
	public List<int> VariantList;
	public SpriteDataForSH SpriteInfo;

	public override void OnStartServer()
	{
		SyncFindData(this.SynchronisedString);
		base.OnStartServer();
	}

	public override void OnStartClient()
	{
		SyncFindData(this.SynchronisedString);
		base.OnStartClient();
	}

	public void SyncFindData(string SynchString)
	{		this.SynchronisedString = SynchString;
		if (ClothFactory.Instance.ClothingStoredData.ContainsKey(SynchString))
		{
			clothingData = ClothFactory.Instance.ClothingStoredData[SynchString];
			Start();
		}
		else if (ClothFactory.Instance.BackpackStoredData.ContainsKey(SynchString))
		{
			containerData = ClothFactory.Instance.BackpackStoredData[SynchString];
			Start();
		} else if (ClothFactory.Instance.HeadSetStoredData.ContainsKey(SynchString))
		{
			headsetData = ClothFactory.Instance.HeadSetStoredData[SynchString];
			Start();
		}
	}


	public int ReturnState(ClothingVariantType CVT)
	{
		if (VariantStore.ContainsKey(CVT))
		{
			return (VariantStore[CVT]);
		}
		return (0);
	}
	public int ReturnVariant(int VI)
	{
		if (VariantList.Count > VI)
		{
			return (VariantList[VI]);
		}
		return (0);
	}
	public void SetSynchronise(ClothingData CD = null, ContainerData ConD = null, HeadsetData HD = null)
	{
		if (CD != null)
		{
			SynchronisedString = CD.name;
			clothingData = CD;
		}
		else if (ConD != null)
		{
			SynchronisedString = ConD.name;
			containerData = ConD;
		}
		else if (HD != null)
		{
			SynchronisedString = HD.name;			headsetData = HD;
		}

	}




	public void Start()
	{
		if (Initialised != true)
		{

			if (clothingData != null)
			{
				var _Clothing = this.GetComponent<Clothing>();
				var Item = this.GetComponent<ItemAttributes>();
				_Clothing.SpriteInfo = StaticSpriteHandler.SetUpSheetForClothingData(clothingData, this);
				Item.SetUpFromClothingData(clothingData.Base, clothingData.ItemAttributes);
				switch (Type)
				{
					case ClothingVariantType.Default:
						if (Variant > -1)
						{
							if (!(clothingData.Variants.Count >= Variant))
							{
								Item.SetUpFromClothingData(clothingData.Variants[Variant], clothingData.ItemAttributes);
							}
						}
						break;
					case ClothingVariantType.Skirt:
						Item.SetUpFromClothingData(clothingData.DressVariant, clothingData.ItemAttributes);
						break;
					case ClothingVariantType.Tucked:
						Item.SetUpFromClothingData(clothingData.Base_Adjusted, clothingData.ItemAttributes);
						break;

				}
				Initialised = true;
			}
			else if (containerData != null)
			{
				var Item = this.GetComponent<ItemAttributes>();
				var Storage = this.GetComponent<StorageObject>();
				this.SpriteInfo = StaticSpriteHandler.SetupSingleSprite(containerData.Sprites.Equipped);
				Item.SetUpFromClothingData(containerData.Sprites, containerData.ItemAttributes);
				Storage.SetUpFromStorageObjectData(containerData.StorageData);
				Initialised = true;
			}
			else if (headsetData != null)
			{

				var Item = this.GetComponent<ItemAttributes>();
				var Headset = this.GetComponent<Headset>();
				this.SpriteInfo = StaticSpriteHandler.SetupSingleSprite(headsetData.Sprites.Equipped);
				Item.SetUpFromClothingData(headsetData.Sprites, headsetData.ItemAttributes);
				Headset.EncryptionKey = headsetData.Key.EncryptionKey;
				Initialised = true;
			}
		}
	}
}

public enum ClothingVariantType
{
	Default,
	Tucked,
	Skirt
}