using System.Collections.Generic;
using UnityEngine;
using Mirror;

//Used when spawning the food
[RequireComponent(typeof(CustomNetTransform))]
[DisallowMultipleComponent]
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
				plantData = new PlantData();
				plantData.SetValues(DefaultPlantData.PlantDictionary[PlantSyncString].plantData);
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
				ChemicalDictionary[Chemical.Name] = (Chemical.Ammount * (plantData.Potency / 100f));
			}
			reagentContainer.AddReagents(ChemicalDictionary);

		}
	}

	/// <summary>
	/// Gets seeds for plant and replaces held food with seeds
	/// Might not work as activate eats instead?
	/// </summary>
	/// <param name="interaction"></param>
	public void ServerPerformInteraction(HandActivate interaction)
	{
		if (plantData != null)
		{
			var seedObject = Spawn.ServerPrefab(SeedPacket, interaction.Performer.RegisterTile().WorldPositionServer, parent: interaction.Performer.transform.parent).GameObject;
			var seedPacket = seedObject.GetComponent<SeedPacket>();
			seedPacket.plantData = new PlantData();
			seedPacket.plantData.SetValues(plantData);

			seedPacket.SyncPlant(null, plantData.Name);

			var slot = interaction.HandSlot;
			Inventory.ServerAdd(seedObject, interaction.HandSlot, ReplacementStrategy.DespawnOther);
		}


	}
}

