
using UnityEngine;

/// <summary>
/// Spawns an indicated cloth. Can also be used as a slot populator.
/// </summary>
[CreateAssetMenu(fileName = "SpawnableCloth", menuName = "Spawnable/SpawnableCloth")]
public class SpawnableCloth : Spawnable
{
	[SerializeField]
	[Tooltip("Cloth data indicating the cloth to create and put in this cloth.")]
	private BaseClothData ClothData;

	[SerializeField]
	[Tooltip("Variant of the cloth to spawn.")]
	private ClothingVariantType ClothingVariantType = ClothingVariantType.Default;

	[SerializeField]
	[Tooltip("Index of the variant of the cloth to spawn. ")]
	private int ClothVariantIndex = -1;

	[SerializeField]
	[Tooltip("Prefab override to use. Leave blank to use the default prefab for this cloth.")]
	private GameObject PrefabOverride;

	public override SpawnableResult SpawnIt(SpawnDestination destination)
	{
		return new Spawnable(ClothData, ClothingVariantType, ClothVariantIndex, PrefabOverride).SpawnAt(destination);
	}

	/// <summary>
	/// Creates a spawnable for spawning the cloth with the indicated settings.
	/// </summary>
	/// <param name="baseClothData"></param>
	/// <param name="clothingVariantType"></param>
	/// <param name="clothVariantIndex"></param>
	/// <param name="prefabOverride"></param>
	/// <returns></returns>
	public static ISpawnable For(BaseClothData baseClothData,
		ClothingVariantType clothingVariantType = ClothingVariantType.Default,
		int clothVariantIndex = -1, GameObject prefabOverride = null)
	{
		return new Spawnable(baseClothData, clothingVariantType, clothVariantIndex, prefabOverride);
	}

	/// <summary>
	/// Creates a spawnable cloth whose settings match clothing
	/// </summary>
	/// <param name="clothing"></param>
	/// <returns></returns>
	public static ISpawnable For(Clothing clothing)
	{
		return new Spawnable(clothing.clothingData, clothing.Type, clothing.Variant, null);
	}

	/// <summary>
	/// Used internally so we don't need to create an asset at runtime when we want to spawn a cloth
	/// by dynamically (rather than using a predefined SpawnableCloth asset).
	/// Private so we don't expose this implementation detail / clutter the namespace.
	/// </summary>
	private class Spawnable : ISpawnable
	{
		private readonly BaseClothData clothData;
		private readonly ClothingVariantType clothingVariantType;
		private readonly int clothVariantIndex;
		private readonly GameObject prefabOverride;

		public Spawnable(BaseClothData clothData, ClothingVariantType clothingVariantType, int clothVariantIndex, GameObject prefabOverride)
		{
			this.clothData = clothData;
			this.clothingVariantType = clothingVariantType;
			this.clothVariantIndex = clothVariantIndex;
			this.prefabOverride = prefabOverride;
		}

		public SpawnableResult SpawnAt(SpawnDestination destination)
		{
			var result = CreateBaseCloth(destination);
			if (result == null)
			{
				return SpawnableResult.Fail(destination);
			}
			return SpawnableResult.Single(result, destination);
		}

		private GameObject CreateBaseCloth(SpawnDestination destination)
		{
			if (clothData == null)
			{
				Logger.LogError("Cannot spawn, cloth data is null", Category.ItemSpawn);
				return null;
			}
			if (clothData is HeadsetData headsetData)
			{
				return CreateHeadsetCloth(headsetData, destination);
			}
			else if (clothData is ContainerData containerData)
			{
				return CreateBackpackCloth(containerData, destination);
			}
			else if (clothData is ClothingData clothingData)
			{
				return CreateCloth(clothingData, destination);
			}
			else
			{
				Logger.LogErrorFormat("Unrecognize BaseClothData subtype {0}, please add logic" +
				                      " to ClothFactory to handle spawning this type.", Category.ItemSpawn,
					clothData.GetType().Name);
				return null;
			}
		}

		private GameObject CreateCloth(ClothingData clothData, SpawnDestination destination)
		{
			var clothObj = SpawnCloth("UniCloth", destination);

			var _Clothing = clothObj.GetComponent<Clothing>();
			var Item = clothObj.GetComponent<ItemAttributes>();
			_Clothing.spriteInfo = StaticSpriteHandler.SetUpSheetForClothingData(clothData, _Clothing);
			_Clothing.SetSynchronise(CD: clothData);
			Item.SetUpFromClothingData(clothData.Base, clothData.ItemAttributes);
			switch (clothingVariantType)
			{
				case ClothingVariantType.Default:
					if (clothVariantIndex > -1)
					{
						if (!(clothData.Variants.Count >= clothVariantIndex))
						{
							Item.SetUpFromClothingData(clothData.Variants[clothVariantIndex], clothData.ItemAttributes);
						}
					}

					break;
				case ClothingVariantType.Skirt:
					Item.SetUpFromClothingData(clothData.DressVariant, clothData.ItemAttributes);
					break;
				case ClothingVariantType.Tucked:
					Item.SetUpFromClothingData(clothData.Base_Adjusted, clothData.ItemAttributes);
					break;
			}

			clothObj.name = clothData.name;
			return clothObj;
		}

		private GameObject CreateBackpackCloth(ContainerData containerData, SpawnDestination destination)
		{
			var clothObj = SpawnCloth("UniBackPack", destination);
			if (clothObj == null) return null;

			var _Clothing = clothObj.GetComponent<Clothing>();
			var Item = clothObj.GetComponent<ItemAttributes>();
			_Clothing.spriteInfo = StaticSpriteHandler.SetupSingleSprite(containerData.Sprites.Equipped);
			Item.SetUpFromClothingData(containerData.Sprites, containerData.ItemAttributes);
			_Clothing.SetSynchronise(ConD: containerData);
			return clothObj;
		}



		private GameObject CreateHeadsetCloth(HeadsetData headsetData, SpawnDestination destination)
		{
			var clothObj = SpawnCloth("UniHeadSet", destination);

			var _Clothing = clothObj.GetComponent<Clothing>();
			var Item = clothObj.GetComponent<ItemAttributes>();
			var Headset = clothObj.GetComponent<Headset>();
			_Clothing.spriteInfo = StaticSpriteHandler.SetupSingleSprite(headsetData.Sprites.Equipped);
			_Clothing.SetSynchronise(HD: headsetData);
			Item.SetUpFromClothingData(headsetData.Sprites, headsetData.ItemAttributes);
			Headset.EncryptionKey = headsetData.Key.EncryptionKey;
			return clothObj;
		}


		private GameObject SpawnCloth(string prefabName, SpawnDestination destination)
		{
			var prefab = prefabOverride;
			if (prefab == null)
			{
				prefab = Spawn.GetPrefabByName(prefabName);
			}

			if (prefab == null)
			{
				Logger.LogError(prefabName + " Prefab not found", Category.SpriteHandler);
				return null;
			}

			GameObject clothObj = Spawn.ServerPrefab(prefab, destination.WorldPosition, destination.Parent).GameObject;
			return clothObj;
		}
	}
}
