using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clothing : MonoBehaviour
{

	public ClothingVariantType Type;
	public int Variant =  -1;

	public ClothingData clothingData;
	public ContainerData containerData;
	public HeadsetData headsetData;

	public Dictionary<ClothingVariantType, int> VariantStore = new Dictionary<ClothingVariantType, int>();
	public List<int> VariantList;
	public SpriteDataForSH SpriteInfo;

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
	void Start()
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
		}
		else if (containerData != null)
		{
			var Item = this.GetComponent<ItemAttributes>();
			var Storage = this.GetComponent<StorageObject>();
			this.SpriteInfo = StaticSpriteHandler.SetupSingleSprite(containerData.Sprites.Equipped);
			Item.SetUpFromClothingData(containerData.Sprites, containerData.ItemAttributes);
			Storage.SetUpFromStorageObjectData(containerData.StorageData);
		}
		else if (headsetData != null)
		{
			
			var Item = this.GetComponent<ItemAttributes>();
			var Headset = this.GetComponent<Headset>();
			this.SpriteInfo = StaticSpriteHandler.SetupSingleSprite(headsetData.Sprites.Equipped);
			Item.SetUpFromClothingData(headsetData.Sprites, headsetData.ItemAttributes);
			Headset.EncryptionKey = headsetData.Key.EncryptionKey;
		}
	}
}

public enum ClothingVariantType
{
	Default,
	Tucked,
	Skirt
}