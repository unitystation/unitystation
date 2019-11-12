using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;

public class hydroponicsTray : NBHandApplyInteractable
{

	[SyncVar(hook = nameof(SyncHarvest))]
	public bool SyncHarvestNotifier; 

	[SyncVar(hook = nameof(SyncWeed))]
	public bool SyncWeedNotifier;

	[SyncVar(hook = nameof(SyncWater))]
	public bool SyncWaterNotifier;

	[SyncVar(hook = nameof(SyncNutriment))]
	public bool SyncNutrimentNotifier;



	//OH It hurts
	public void SyncHarvest(bool _SyncHarvestNotifier) {
		if (SyncHarvestNotifier != _SyncHarvestNotifier)
		{
			SyncHarvestNotifier = _SyncHarvestNotifier;
			if (SyncHarvestNotifier)
			{
				HarvestNotifier.PushTexture();
			}
			else {
				HarvestNotifier.PushClear();
			}
		}
	}

	public void SyncWeed(bool _SyncWeedNotifier)
	{
		if (SyncWeedNotifier != _SyncWeedNotifier)
		{
			SyncWeedNotifier = _SyncWeedNotifier;
			if (SyncWeedNotifier)
			{
				WeedNotifier.PushTexture();
			}
			else {
				WeedNotifier.PushClear();
			}
		}
	}

	public void SyncWater(bool _SyncWaterNotifier)
	{
		if (SyncWaterNotifier != _SyncWaterNotifier)
		{

			SyncWaterNotifier = _SyncWaterNotifier;
			if (SyncWaterNotifier)
			{
				WaterNotifier.PushTexture();
			}
			else {

				WaterNotifier.PushClear();
			}
		}
	}


	public void SyncNutriment(bool _SyncNutrimentNotifier)
	{
		if (SyncNutrimentNotifier != _SyncNutrimentNotifier)
		{
			SyncNutrimentNotifier = _SyncNutrimentNotifier;
			if (SyncNutrimentNotifier)
			{
				NutrimentNotifier.PushTexture();
			}
			else {

				NutrimentNotifier.PushClear();
			}
		}
	}



	[SyncVar(hook = nameof(SyncStage))]
	public PlantSpriteStage PlantSyncStage;


	public void SyncStage(PlantSpriteStage _PlantSyncStage)
	{
		if (PlantSyncStage != _PlantSyncStage)
		{
			PlantSyncStage = _PlantSyncStage;
			switch (PlantSyncStage)
			{
				case PlantSpriteStage.None:
					PlantSprite.PushClear();
					break;

				case PlantSpriteStage.FullyGrown:
					PlantSprite.Infos = StaticSpriteHandler.SetupSingleSprite(plantData.FullyGrownSprite);
					PlantSprite.PushTexture();
					break;
				case PlantSpriteStage.Dead:
					PlantSprite.Infos = StaticSpriteHandler.SetupSingleSprite(plantData.DeadSprite);
					PlantSprite.PushTexture();
					break;
				case PlantSpriteStage.Growing:
					PlantSprite.Infos = StaticSpriteHandler.SetupSingleSprite(plantData.GrowthSprites[GrowingPlantStage]);
					PlantSprite.PushTexture();
					break;
			}
		}
	}

	[SyncVar(hook = nameof(SyncGrowingPlantStage))]
	public int GrowingPlantStage;

	public void SyncGrowingPlantStage(int _GrowingPlantStage)
	{
		GrowingPlantStage = _GrowingPlantStage;
	}


	[SyncVar(hook = nameof(SyncPlant))]
	public string PlantSyncString;

	public void SyncPlant(string _PlantSyncString)
	{
		PlantSyncString = _PlantSyncString;
		if (DefaultPlantData.PlantDictionary.ContainsKey(PlantSyncString)) {
			plantData = DefaultPlantData.PlantDictionary[PlantSyncString].plantData;
		}
	}


	public List<DefaultPlantData> PotentialWeeds = new List<DefaultPlantData>();
	public List<GameObject> ReadyProduce = new List<GameObject>();
	public PlantTrayModification Modification;
	public ReagentContainer reagentContainer;
	public SpriteHandler PlantSprite;

	public SpriteHandler HarvestNotifier;
	public SpriteHandler WeedNotifier;
	public SpriteHandler WaterNotifier;
	public SpriteHandler NutrimentNotifier;
	public PlantData plantData;
	public bool hasplant = false;

	public static System.Random random = new System.Random();

	public float tickRate = 0.1f;
	public float tickCount;
	public float WeedLevel = 0; //10
	public float PestLevel;
	public float ToxicityLevel;
	public float NutritionLevel = 100;

	public int MaxWeedLevel = 10;
	public int MaxPestLevel = 10;
	public int MaxToxicityLevel = 100;
	public int MaxNutritionLevel = 10;


	public int NumberOfUpdatesAlive = 0;
	public int PlantAge = 0;
	public int PlantSubStage = 0;
	public float PlantHealth = 100;
	public bool ReadyToHarvest = false;
	public bool HasFullyGrown = false;


	public override void OnStartClient()
	{
		SyncPlant(this.PlantSyncString);
		SyncStage(this.PlantSyncStage);
		SyncGrowingPlantStage(this.GrowingPlantStage);
		SyncNutriment(this.SyncNutrimentNotifier);
		SyncWater(this.SyncWaterNotifier);
		SyncWeed(this.SyncWeedNotifier);
		SyncHarvest(this.SyncHarvestNotifier);
		base.OnStartClient();
	}

	void Update()
	{
		if (isServer)
		{
			tickCount += Time.deltaTime;
			if (tickCount > tickRate)
			{
				DoTick();
				tickCount = 0f;
			}
		}
	}

	public void DoTick()
	{
		if (hasplant)
		{

			if (WeedLevel < 10)
			{
				WeedLevel = WeedLevel + ((0.1f) * (plantData.WeedGrowthRate / 10f));
				if (WeedLevel > 10)
				{
					WeedLevel = 10;
				}
			}
			if (WeedLevel > 9.5f && !plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
			{
				PlantHealth = PlantHealth + (((plantData.WeedResistance - 11f) / 10f) * (WeedLevel / 10f) * 5);
				//Logger.Log("plantData.weed > " + plantData.PlantHealth);
			}

			//Logger.Log(WeedLevel.ToString());
			if (reagentContainer.Contents.ContainsKey("water"))
			{
				if (reagentContainer.Contents["water"] > 0)
				{
					reagentContainer.Contents["water"] = reagentContainer.Contents["water"] - 0.1f;
				}
				else if (reagentContainer.Contents["water"] <= 0 && !plantData.PlantTrays.Contains(PlantTrays.Fungal_Vitality))
				{
					PlantHealth = PlantHealth + (((plantData.Endurance - 101f) / 100f) * 1);
				}
			}
			else if (!plantData.PlantTrays.Contains(PlantTrays.Fungal_Vitality))
			{
				PlantHealth = PlantHealth + (((plantData.Endurance - 101f) / 100f) * 1);
			}
			NumberOfUpdatesAlive++;
			//Logger.Log(plantData.NumberOfUpdatesAlive.ToString());
			if (!ReadyToHarvest)
			{

				PlantSubStage = PlantSubStage + plantData.GrowthSpeed;


				if (PlantSubStage > 100)
				{
					//Logger.Log("NutritionLevel" + NutritionLevel);
					PlantSubStage = 0;
					if (NutritionLevel > 0 || plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
					{
						if (!plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
						{
							if (NutritionLevel != 0)
							{
								NutritionLevel = NutritionLevel - 1;
							}
							if (NutritionLevel < 0) {
								NutritionLevel = 0;
							}
						}
					 	
						Logger.Log("oooo");
						SyncGrowingPlantStage(GrowingPlantStage + 1);
						if (GrowingPlantStage < plantData.GrowthSprites.Count)
						{
							SyncStage(PlantSpriteStage.Growing);
						}
						else {
							if (!ReadyToHarvest)
							{
								NaturalMutation();
								SyncStage(PlantSpriteStage.FullyGrown);
								ReadyToHarvest = true;
								HasFullyGrown = true;
								ProduceCrop();
								SyncHarvest(true);
							}
						}
					}
					else {

						PlantHealth = PlantHealth + (((plantData.Endurance - 101f) / 100f) * 5);
						//Logger.Log("plantData.Nutriment > " + plantData.PlantHealth);
					}
				}
			}
			if (PlantHealth < 0)
			{
				CropDeath();
			}
			else if (NumberOfUpdatesAlive > plantData.Lifespan * 100000)
			{
				CropDeath();
			}
		}
		else {

			if (WeedLevel < 10)
			{
				WeedLevel = WeedLevel + ((0.1f) * (0.1f));
				if (WeedLevel > 10)
				{
					WeedLevel = 10;
				}
			}
			if (WeedLevel >= 10)
			{
				var Data = PotentialWeeds[random.Next(PotentialWeeds.Count)];
				plantData = new PlantData();
				plantData.SetValues(Data.plantData);
				PlantSyncString = plantData.Name;
				SyncGrowingPlantStage(0);
				SyncStage(PlantSpriteStage.Growing);
				WeedLevel = 0;
				hasplant = true;
			}
		}
		if (NutritionLevel < 25)
		{
			SyncNutriment(true);
		}
		else {
			SyncNutriment(false);
		}

		if (reagentContainer.Contents.ContainsKey("water"))
		{
			if (reagentContainer.Contents["water"] < 25)
			{
				SyncWater(true);
			}
			else
			{
				SyncWater(false);
			}
		}
		else {
			SyncWater(true);
		}
		if (WeedLevel > 5)
		{
			SyncWeed(true);
		}
		else {
			SyncWeed(false);
		}
	}


	public void NaturalMutation()
	{
		plantData.WeedResistance = StatMutation(plantData.WeedResistance, 10);
		plantData.WeedGrowthRate = StatMutation(plantData.WeedGrowthRate, -10);
		plantData.GrowthSpeed = StatMutation(plantData.GrowthSpeed, 100);
		plantData.Potency = StatMutation(plantData.Potency, 100);
		plantData.Endurance = StatMutation(plantData.Endurance, 100);
		plantData.Yield = StatMutation(plantData.Yield, 10);
		plantData.Lifespan = StatMutation(plantData.Lifespan, 100);

		switch (Modification)
		{
			case PlantTrayModification.None:
				break;
			case PlantTrayModification.WeedResistance:
				plantData.WeedResistance = SpecialStatMutation(plantData.WeedResistance, 10);
				break;
			case PlantTrayModification.WeedGrowthRate:
				plantData.WeedGrowthRate = SpecialStatMutation(plantData.WeedGrowthRate, -10);
				break;
			case PlantTrayModification.GrowthSpeed:
				plantData.GrowthSpeed = SpecialStatMutation(plantData.GrowthSpeed, 100);
				break;
			case PlantTrayModification.Potency:
				plantData.Potency = SpecialStatMutation(plantData.Potency, 100);
				break;
			case PlantTrayModification.Endurance:
				plantData.Endurance = SpecialStatMutation(plantData.Endurance, 100);
				break;
			case PlantTrayModification.Yield:
				plantData.Yield = SpecialStatMutation(plantData.Yield, 10);
				break;
			case PlantTrayModification.Lifespan:
				plantData.Lifespan = SpecialStatMutation(plantData.Lifespan, 100);
				break;
		}

		if (random.Next(100) > 95)
		{
			Mutation();
		}
	}

	public int SpecialStatMutation(float Stat, float maxStat)
	{
		return ((int)Stat + random.Next(0, (int)Math.Ceiling((maxStat / 100f) * 7)));
	}

	public int StatMutation(float Stat, float maxStat)
	{

		return ((int)Stat + random.Next(-(int)Math.Ceiling((maxStat / 100f) * 2), (int)Math.Ceiling((maxStat / 100f) * 5)));
	}


	public void CropDeath()
	{
		if (plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
		{			NutritionLevel = NutritionLevel + plantData.Potency;
			if (NutritionLevel > 100)
			{
				NutritionLevel = 100;
			}
		}

		if (plantData.PlantTrays.Contains(PlantTrays.Fungal_Vitality))
		{
			Dictionary<string, float> Reagent = new Dictionary<string, float> { ["water"] = plantData.Potency };
			reagentContainer.AddReagents(Reagent);
		}

		PlantAge = 0;
		SyncGrowingPlantStage(0);
		PlantSubStage = 0;
		PlantHealth = 100;
		ReadyToHarvest = false;
		HasFullyGrown = false;
		NumberOfUpdatesAlive = 0;
		SyncStage(PlantSpriteStage.Dead);
		plantData = null;
		hasplant = false;
		ReadyProduce.Clear();
		SyncHarvest(false);

	}


	public void ProduceCrop()
	{
		for (int i = 0; i < plantData.Yield; i++)
		{
			var _Object = PoolManager.PoolNetworkInstantiate(plantData.ProduceObject, this.transform.position, parent: this.transform.parent);
			CustomNetTransform netTransform = _Object.GetComponent<CustomNetTransform>();
			var food = _Object.GetComponent<GrownFood>();
			if (food != null)
			{
				food.plantData = new PlantData();
				food.plantData.SetValues(plantData);
				PlantSyncString = plantData.Name;
				food.SetUpFood();
			}
			netTransform.DisappearFromWorldServer();
			ReadyProduce.Add(_Object);
		}
	}

	public void Mutation()
	{
		var tint = random.Next(plantData.MutatesInTo.Count);
		var Data = plantData.MutatesInTo[tint];
		plantData.MutateTo(Data);
		PlantSyncString = Data.plantData.Name;
	}

	protected override bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//Logger.Log("HGGG");
		//start with the default HandApply WillInteract logic.
		if (!base.WillInteract(interaction, side)) return false;
		//Logger.Log("FFFF");
		//only care about interactions targeting us
		if (interaction.TargetObject != gameObject) return false;
		//Logger.Log("GGGG");
		//only try to interact if the user has a wrench or metal in their hand
		//if (!Validations.HasComponent<SeedPacket>(interaction.HandObject)) return false;
		//Logger.Log("AAA");
		return true;
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		Logger.Log("-B");
		var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.HandSlot.equipSlot);
		var ObjectContainer = slot?.Item?.GetComponent<ReagentContainer>();
		if (hasplant)
		{
			if (plantData.MutatesInTo.Count > 0)
			{
				if (ObjectContainer != null)
				{
					if (!ObjectContainer.InSolidForm)
					{
						ObjectContainer.MoveReagentsTo(5, reagentContainer);
						if (reagentContainer.Contains("mutagen", 5))
						{
							reagentContainer.Contents["mutagen"] = reagentContainer.Contents["mutagen"] - 5;
							Mutation();

						}
					}
				}
			}
		}
		Logger.Log("-A");
		var Object = slot?.Item?.GetComponent<SeedPacket>();
		if (Object != null)
		{
			hasplant = true;
			plantData = new PlantData();
			plantData.SetValues(slot.Item.GetComponent<SeedPacket>().plantData);
			PlantSyncString = plantData.Name;
			SyncGrowingPlantStage(0);
			SyncStage(PlantSpriteStage.Growing);
			InventoryManager.ClearInvSlot(slot);
		}
		else {
			Logger.Log("A");
			if (plantData != null)
			{
				Logger.Log("B");
				if (ReadyToHarvest)
				{
					Logger.Log("C");
					for (int i = 0; i < ReadyProduce.Count; i++)
					{
						CustomNetTransform netTransform = ReadyProduce[i].GetComponent<CustomNetTransform>();
						netTransform.AppearAtPosition(this.transform.position);
						netTransform.AppearAtPositionServer(this.transform.position);
					}
					ReadyProduce.Clear();
					if (plantData.PlantTrays.Contains(PlantTrays.Perennial_Growth))
					{
						ReadyToHarvest = false;
						PlantSubStage = 0;
						SyncGrowingPlantStage(0);
						SyncStage(PlantSpriteStage.Growing);
						SyncHarvest(false);
					}
					else {

						plantData = null;
						hasplant = false;
						SyncStage(PlantSpriteStage.None);
						SyncHarvest(false);
					}
				}
			}
			else {
				SyncStage(PlantSpriteStage.None);
			}
		}
	}
}


public enum PlantSpriteStage
{
	None,
	FullyGrown,
	Dead,
	Growing,

}



public enum PlantTrayModification
{
	None,
	WeedResistance,
	WeedGrowthRate,
	GrowthSpeed,
	Potency,
	Endurance,
	Yield,
	Lifespan,
}
