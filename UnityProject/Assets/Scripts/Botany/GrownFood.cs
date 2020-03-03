using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class GrownFood : NetworkBehaviour, IInteractable<HandActivate>
{
	public GameObject SeedPacket;
	public SpriteRenderer SpriteSizeAdjustment;
	public SpriteHandler SpriteHandler;
	public PlantData plantData;
	public ReagentContainer reagentContainer;
	public ItemAttributesV2 ItemAttributesV2;

	[SyncVar(hook = nameof(SyncPlant))]
	public string PlantSyncString;

	public void SyncPlant(string _OldPlantSyncString, string _PlantSyncString)
	{
		PlantSyncString = _PlantSyncString;
		if (!isServer)
		{

			if (DefaultPlantData.PlantDictionary.ContainsKey(PlantSyncString))
			{
				plantData = DefaultPlantData.PlantDictionary[PlantSyncString].plantData;
			}
		}
		SpriteHandler.spriteData = SpriteFunctions.SetupSingleSprite(plantData.ProduceSprite);
		SpriteHandler.PushTexture();
		if (ItemAttributesV2 == null)
		{
			ItemAttributesV2 = this.GetComponent<ItemAttributesV2>();
		}
		if (isServer && ItemAttributesV2 != null)
		{
			ItemAttributesV2.ServerSetArticleDescription(plantData.Description);
			ItemAttributesV2.ServerSetArticleName(plantData.Plantname);
		}
		this.name = plantData.Plantname;

	}


	[SyncVar(hook = nameof(SyncSize))]
	public float SizeScale;

	public void SyncSize(float oldScale, float newScale)
	{
		SizeScale = newScale;
		SpriteSizeAdjustment.transform.localScale = new Vector3((SizeScale), (SizeScale), (SizeScale));
	}


	public override void OnStartClient()
	{
		SyncPlant(null, this.PlantSyncString);
		SyncSize(this.SizeScale, this.SizeScale);
		base.OnStartClient();
	}

	public void SetUpFood()
	{
		SyncPlant(null, plantData.Name);
		SpriteHandler.spriteData = SpriteFunctions.SetupSingleSprite(plantData.ProduceSprite);
		SpriteHandler.PushTexture();
		SetupChemicalContents();
		SyncSize(SizeScale, 0.5f + (plantData.Potency / 100f));
	}


	public void SetupChemicalContents()
	{
		if (plantData.ReagentProduction.Count > 0)
		{
			var ChemicalDictionary = new Dictionary<string, float>();
			foreach (var Chemical in plantData.ReagentProduction)
			{
				ChemicalDictionary[Chemical.String] = (Chemical.Int * (plantData.Potency / 100f));
			}
			reagentContainer.AddReagents(ChemicalDictionary);

		}
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		if (plantData != null)
		{
			var _Object = Spawn.ServerPrefab(SeedPacket, interaction.Performer.transform.position, parent: interaction.Performer.transform.parent).GameObject;
			var seedPacket = _Object.GetComponent<SeedPacket>();
			seedPacket.plantData = plantData;

			seedPacket.SyncPlant(null, plantData.Name);

			var slot = interaction.HandSlot;
			Inventory.ServerAdd(_Object, interaction.HandSlot, ReplacementStrategy.DespawnOther);
		}


	}
}

