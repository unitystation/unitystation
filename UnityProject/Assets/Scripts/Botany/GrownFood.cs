using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;


public class GrownFood : NetworkBehaviour, IInteractable<HandActivate>
{
	public GameObject SeedPacket;
	public SpriteRenderer SpriteSizeAdjustment;
	public SpriteHandler SpriteHandler;
	public PlantData plantData;
	public ReagentContainer reagentContainer;


	[SyncVar(hook = nameof(SyncPlant))]
	public string PlantSyncString;

	public void SyncPlant(string _PlantSyncString)
	{
		PlantSyncString = _PlantSyncString;
		if (DefaultPlantData.PlantDictionary.ContainsKey(PlantSyncString))
		{
			plantData = DefaultPlantData.PlantDictionary[PlantSyncString].plantData;
		}
		SpriteHandler.Infos = StaticSpriteHandler.SetupSingleSprite(plantData.ProduceSprite);
		SpriteHandler.PushTexture();
	}


	[SyncVar(hook = nameof(SyncSize))]
	public float SizeScale;

	public void SyncSize(float _SizeScale)
	{
		SizeScale = _SizeScale;
		SpriteSizeAdjustment.transform.localScale = new Vector3((SizeScale), (SizeScale), (SizeScale));
	}


	public override void OnStartClient()
	{
		SyncPlant(this.PlantSyncString);
		SyncSize(this.SizeScale);
		base.OnStartClient();
	}

	public void SetUpFood()
	{
		SyncPlant(plantData.Name);
		SpriteHandler.Infos = StaticSpriteHandler.SetupSingleSprite(plantData.ProduceSprite);
		SpriteHandler.PushTexture();
		SetupChemicalContents();
		SyncSize(0.5f + (plantData.Potency / 200f));
	}


	public void SetupChemicalContents() {
		if (plantData.ReagentProduction.Count > 0) {			var ChemicalDictionary = new Dictionary<string, float>();
			foreach (var Chemical in plantData.ReagentProduction) {
				ChemicalDictionary[Chemical.String] = (Chemical.Int * (plantData.Potency / 100f));
			}
			reagentContainer.AddReagents(ChemicalDictionary);

		}
	}
    // Start is called before the first frame update

	public bool Interact(HandActivate interaction)
	{
		//try ejecting the mag
		if (plantData != null)
		{
			var _Object = PoolManager.PoolNetworkInstantiate(SeedPacket, interaction.Performer.transform.position, parent: interaction.Performer.transform.parent);
			CustomNetTransform netTransform = _Object.GetComponent<CustomNetTransform>();
			var seedPacket = _Object.GetComponent<SeedPacket>();
			seedPacket.plantData = plantData;
			var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.HandSlot.equipSlot);
			//InventoryManager.ClearInvSlot(slot);
			InventoryManager.EquipInInvSlot(slot, _Object);
			PoolManager.PoolNetworkDestroy(this.gameObject);
			netTransform.DisappearFromWorldServer();
			//Destroy(gameObject);
			return true;
		}

		return false;
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}

