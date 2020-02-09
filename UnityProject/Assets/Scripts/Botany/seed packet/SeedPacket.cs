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

	public void SyncPlant(string _OldPlantSyncString, string _PlantSyncString)
	{
		EnsureInit();
		PlantSyncString = _PlantSyncString;
		if (!isServer)
		{
			if (DefaultPlantData.PlantDictionary.ContainsKey(PlantSyncString))
			{
				plantData = DefaultPlantData.PlantDictionary[PlantSyncString].plantData;
			}
		}
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
		if (plantData == null && defaultPlantData != null)
		{
			plantData = new PlantData();
			plantData.SetValues(defaultPlantData);
		}
	}

	void Start()
	{
		EnsureInit();
		SyncPlant(null, plantData.Name);
	}
}


