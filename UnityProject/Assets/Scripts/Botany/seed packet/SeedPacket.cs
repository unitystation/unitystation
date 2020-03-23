using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SeedPacket : NetworkBehaviour
{
	public SpriteHandler Sprite;
	public PlantData plantData; //Stats and stuff
	public DefaultPlantData defaultPlantData;

	[SyncVar(hook = nameof(SyncPlant))]
	public string PlantSyncString;

	private SeedPacket() { }

	/*public static SeedPacket CreateSeedPacketInstance(SeedPacket seedPacket)
	{
		return new SeedPacket
		{
			name = seedPacket.name,
			Sprite = seedPacket.Sprite,
			plantData = PlantData.CreateNewPlant(seedPacket.plantData),
			defaultPlantData = seedPacket.defaultPlantData,
			PlantSyncString = seedPacket.plantData.Name
		};
	}*/

	public void SyncPlant(string _OldPlantSyncString, string _PlantSyncString)
	{
		//EnsureInit();
		PlantSyncString = _PlantSyncString;
		/*if (!isServer)
		{
			if (DefaultPlantData.PlantDictionary.ContainsKey(PlantSyncString))
			{
				plantData = DefaultPlantData.PlantDictionary[PlantSyncString].plantData;
			}
		}*/
		Sprite.spriteData = SpriteFunctions.SetupSingleSprite(plantData.PacketsSprite);
		Sprite.PushTexture();
	}

	public override void OnStartClient()
	{
		EnsureInit();
		SyncPlant(null, this.PlantSyncString);
	}

	private void EnsureInit()
	{
		if (string.IsNullOrEmpty(plantData?.Name) && defaultPlantData != null)
		{
			plantData = PlantData.CreateNewPlant(defaultPlantData);
			PlantSyncString = plantData.Name;
		}
	}

	void Start()
	{
		EnsureInit();
		SyncPlant(null, plantData.Name);
	}
}


