using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SeedPacket : NetworkBehaviour
{
	public SpriteHandler Sprite;
	public PlantData plantData; //Stats and stuff

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
		//FFGD Sprite.spriteData = SpriteFunctions.SetupSingleSprite(plantData.PacketsSprite);
		//Sprite.PushTexture();
	}

	public override void OnStartClient()
	{
	}



	void Start()
	{
	}
}


