using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SeedPacket : NetworkBehaviour
{


	public SpriteHandler Sprite;
	//public DefaultPlantsStats defaultPlantsStats;
	public PlantData plantData; //Stats and stuff
	public DefaultPlantData defaultPlantData;

	[SyncVar(hook = nameof(SyncPlant))]
	public string PlantSyncString;

	public void SyncPlant(string _PlantSyncString)
	{
		PlantSyncString = _PlantSyncString;
		if (DefaultPlantData.PlantDictionary.ContainsKey(PlantSyncString))
		{
			plantData = DefaultPlantData.PlantDictionary[PlantSyncString].plantData;
		}
		Sprite.Infos = StaticSpriteHandler.SetupSingleSprite(plantData.PacketsSprite);
		Sprite.PushTexture();
	}

	public override void OnStartClient()
	{
		SyncPlant(this.PlantSyncString);
		base.OnStartClient();
	}

	void Start()
	{
		if (defaultPlantData != null)
		{
			plantData.SetValues(defaultPlantData);
		}
		else { 
			plantData.SetValues(plantData);
		}
		SyncPlant(plantData.Name);
	}

	// Update is called once per frame
	void Update()
	{

	}
}


