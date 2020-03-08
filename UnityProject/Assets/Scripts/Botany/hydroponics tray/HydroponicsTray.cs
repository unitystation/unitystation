using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;

/// <summary>
/// Where the magic happens in botany. This tray grows all of the plants
/// </summary>
public class HydroponicsTray : ManagedNetworkBehaviour, IInteractable<HandApply>, IServerSpawn
{
	[SyncVar(hook = nameof(UpdateHarvestFlag))]
	public bool showHarvestFlag;
	[SyncVar(hook = nameof(UpdateWeedsFlag))]
	public bool showWeedsFlag;
	[SyncVar(hook = nameof(UpdateWaterFlag))]
	public bool showWaterFlag;
	[SyncVar(hook = nameof(UpdateNutrimentFlag))]
	public bool showNutrimenetFlag;
	[SyncVar(hook = nameof(UpdatePlantStage))]
	public PlantSpriteStage plantCurrentStage;
	[SyncVar(hook = nameof(UpdatePlantGrowthStage))]
	public int growingPlantStage;
	[SyncVar(hook = nameof(UpdatePlant))]
	public string plantSyncString;

	private RegisterTile registerTile;

	
	
	public bool isSoilPile;
	public List<DefaultPlantData> potentialWeeds = new List<DefaultPlantData>();
	public List<GameObject> readyProduce = new List<GameObject>();
	public PlantTrayModification modification;
	public ReagentContainer reagentContainer;
	public SpriteHandler plantSprite;

	public SpriteHandler harvestNotifier;
	public SpriteHandler weedNotifier;
	public SpriteHandler waterNotifier;
	public SpriteHandler nutrimentNotifier;
	public PlantData plantData;
	public bool hasPlant;

	private static readonly System.Random random = new System.Random();

	public float tickRate = 0.1f;
	public float tickCount;
	public float weedLevel; //10
	public float nutritionLevel = 100;

	public int numberOfUpdatesAlive;
	
	public int plantSubStage;
	public float plantHealth = 100;
	public bool readyToHarvest;
	//private bool IsServer; //separate flag as NetworkBehaviour isServer is not accurate when destroying an object

	public void OnSpawnServer(SpawnInfo info)
	{
		EnsureInit();
		if (isSoilPile)
		{
			hasPlant = false;
			plantData = new PlantData();
			plantData.SetValues(DefaultPlantData.PlantDictionary.Values.PickRandom());
			UpdatePlant(null, plantData.Name);
			//NaturalMutation();
			UpdatePlantStage(PlantSpriteStage.None, PlantSpriteStage.FullyGrown);
			readyToHarvest = true;
			ProduceCrop();
		}
	}

	/// <summary>
	/// Load values passed from server when client connects
	/// </summary>
	public void OnConnectedToServer()
	{
		EnsureInit();
		UpdateHarvestFlag(false, showHarvestFlag);
		UpdateWeedsFlag(false, showWeedsFlag);
		UpdateWaterFlag(false, showWaterFlag);
		UpdateNutrimentFlag(false, showNutrimenetFlag);
		UpdatePlant(null, plantSyncString);
		UpdatePlantStage(PlantSpriteStage.None, plantCurrentStage);
		UpdatePlantGrowthStage(0, growingPlantStage);
	}


	private void EnsureInit()
	{
		if (registerTile != null) return;
		registerTile = GetComponent<RegisterTile>();
	}

	/// <summary>
	/// Server updates plant status and updates clients as needed
	/// </summary>
	public override void UpdateMe()
	{
		//Only server checks plant status
		if (!isServer) return;

		//Only update at set rate
		tickCount += Time.deltaTime;
		if (tickCount < tickRate)
		{
			return;
		}
		tickCount = 0f;


		if (hasPlant)
		{
			if (weedLevel < 10)
			{
				weedLevel = weedLevel + ((0.1f) * (plantData.WeedGrowthRate / 100f));
				if (weedLevel > 10)
				{
					weedLevel = 10;
				}
			}

			if (weedLevel > 9.5f && !plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
			{
				plantHealth = plantHealth + (((plantData.WeedResistance - 110f) / 100f) * (weedLevel / 10f) * 5);
				//Logger.Log("plantData.weed > " + plantData.PlantHealth);
			}

			if (reagentContainer.Contents.ContainsKey("water"))
			{
				if (reagentContainer.Contents["water"] > 0)
				{
					reagentContainer.Contents["water"] = reagentContainer.Contents["water"] - 0.1f;
				}
				else if (reagentContainer.Contents["water"] <= 0 &&
						 !plantData.PlantTrays.Contains(PlantTrays.Fungal_Vitality))
				{
					plantHealth = plantHealth + (((plantData.Endurance - 101f) / 100f) * 1);
				}
			}
			else if (!plantData.PlantTrays.Contains(PlantTrays.Fungal_Vitality))
			{
				plantHealth = plantHealth + (((plantData.Endurance - 101f) / 100f) * 1);
			}

			numberOfUpdatesAlive++;
			//Logger.Log(plantData.NumberOfUpdatesAlive.ToString());
			if (!readyToHarvest)
			{
				plantSubStage = plantSubStage + (int)Math.Round(plantData.GrowthSpeed / 10f);

				if (plantSubStage > 100)
				{
					//Logger.Log("NutritionLevel" + NutritionLevel);
					plantSubStage = 0;
					if (nutritionLevel > 0 || plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
					{
						if (!plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
						{
							if (nutritionLevel != 0)
							{
								nutritionLevel = nutritionLevel - 1;
							}

							if (nutritionLevel < 0)
							{
								nutritionLevel = 0;
							}
						}

						if ((growingPlantStage + 1) < plantData.GrowthSprites.Count)
						{
							UpdatePlantGrowthStage(growingPlantStage ,growingPlantStage + 1);
							UpdatePlantStage(plantCurrentStage, PlantSpriteStage.Growing);
						}
						else
						{
							if (!readyToHarvest)
							{
								NaturalMutation();
								UpdatePlantStage(plantCurrentStage, PlantSpriteStage.FullyGrown);
								readyToHarvest = true;
								ProduceCrop();
								
							}
							UpdateHarvestFlag(harvestNotifier, true);
						}
					}
					else
					{
						plantHealth = plantHealth + (((plantData.Endurance - 101f) / 100f) * 5);
						//Logger.Log("plantData.Nutriment > " + plantData.PlantHealth);
					}
				}
			}

			if (plantHealth < 0)
			{
				CropDeath();
			}
			else if (numberOfUpdatesAlive > plantData.Lifespan * 500)
			{
				CropDeath();
			}
		}
		else
		{
			if (weedLevel < 10)
			{
				weedLevel = weedLevel + ((0.1f) * (0.1f));
				if (weedLevel > 10)
				{
					weedLevel = 10;
				}
			}

			if (plantData != null)
			{
				if (weedLevel >= 10 && !plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
				{
					var data = potentialWeeds[random.Next(potentialWeeds.Count)];
					plantData = new PlantData();
					plantData.SetValues(data.plantData);
					UpdatePlant(null, plantData.Name);
					UpdatePlantGrowthStage(growingPlantStage, 0);
					UpdatePlantStage(plantCurrentStage, PlantSpriteStage.Growing);
					weedLevel = 0;
					hasPlant = true;
				}
			}
		}

		if (nutritionLevel < 25)
		{
			UpdateNutrimentFlag(showNutrimenetFlag, true);
		}
		else
		{
			UpdateNutrimentFlag(showNutrimenetFlag, false);
		}

		if (reagentContainer.Contents.ContainsKey("water"))
		{
			if (reagentContainer.Contents["water"] < 25)
			{
				UpdateWaterFlag(showWaterFlag, true);
			}
			else
			{
				UpdateWaterFlag(showWaterFlag, false);
			}
		}
		else
		{
			UpdateWaterFlag(showWaterFlag, true);
		}

		if (weedLevel > 5)
		{
			UpdateWeedsFlag(showWeedsFlag, true);
		}
		else
		{
			UpdateWeedsFlag(showWeedsFlag, false);
		}
	}

	/// <summary>
	/// Shows harvest ready sprite on tray if flag is set and tray is not a soil pile
	/// </summary>
	/// <param name="oldNotifier"></param>
	/// <param name="newNotifier"></param>
	private void UpdateHarvestFlag(bool oldNotifier, bool newNotifier)
	{
		if (isSoilPile) return;

		showHarvestFlag = newNotifier;
		if (showHarvestFlag)
		{
			harvestNotifier.PushTexture();
		}
		else
		{
			harvestNotifier.PushClear();
		}
	}

	/// <summary>
	/// Shows high weeds sprite on tray if flag is set and tray is not a soil pile
	/// </summary>
	/// <param name="oldNotifier"></param>
	/// <param name="newNotifier"></param>
	private void UpdateWeedsFlag(bool oldNotifier, bool newNotifier)
	{
		if (isSoilPile) return;

		showWeedsFlag = newNotifier;
		if (showWeedsFlag)
		{
			weedNotifier.PushTexture();
		}
		else
		{
			weedNotifier.PushClear();
		}
	}

	/// <summary>
	/// Shows low water sprite on tray if flag is set and tray is not a soil pile
	/// </summary>
	/// <param name="oldNotifier"></param>
	/// <param name="newNotifier"></param>
	private void UpdateWaterFlag(bool oldNotifier, bool newNotifier)
	{
		if (isSoilPile) return;

		showWaterFlag = newNotifier;
		if (showWaterFlag)
		{
			waterNotifier.PushTexture();
		}
		else
		{
			waterNotifier.PushClear();

		}
	}

	/// <summary>
	/// Shows low nutriment sprite on tray if flag is set and tray is not a soil pile
	/// </summary>
	/// <param name="oldNotifier"></param>
	/// <param name="newNotifier"></param>
	private void UpdateNutrimentFlag(bool oldNotifier, bool newNotifier)
	{
		if (isSoilPile) return;

		showNutrimenetFlag = newNotifier;
		if (showNutrimenetFlag)
		{

			nutrimentNotifier.PushTexture();
		}
		else
		{

			nutrimentNotifier.PushClear();
		}
	}

	
	private void UpdatePlant(string oldPlantSyncString, string newPlantSyncString)
	{
		//if (plantSyncString == newPlantSyncString) return;

		plantSyncString = newPlantSyncString;
		if(newPlantSyncString == null)
		{
			plantData = null;
		}
		else if (DefaultPlantData.PlantDictionary.ContainsKey(plantSyncString))
		{
			plantData = new PlantData();
			plantData.SetValues(DefaultPlantData.PlantDictionary[plantSyncString].plantData);
		}
		UpdateSprite();
	}

	private void UpdatePlantStage(PlantSpriteStage oldValue, PlantSpriteStage newValue)
	{
		plantCurrentStage = newValue;
		UpdateSprite();
	}

	private void UpdatePlantGrowthStage(int oldgrowingPlantStage, int newgrowingPlantStage)
	{
		growingPlantStage = newgrowingPlantStage;
		UpdateSprite();
	}

	/// <summary>
	/// Checks plant state and updates to correct sprite
	/// </summary>
	private void UpdateSprite()
	{
		if (plantData == null)
		{
			plantSprite.PushClear();
			return;
		}
		switch (plantCurrentStage)
		{
			case PlantSpriteStage.None:
				plantSprite.PushClear();
				break;

			case PlantSpriteStage.FullyGrown:
				plantSprite.spriteData = SpriteFunctions.SetupSingleSprite(plantData.FullyGrownSprite);
				plantSprite.PushTexture();
				break;
			case PlantSpriteStage.Dead:
				plantSprite.spriteData = SpriteFunctions.SetupSingleSprite(plantData.DeadSprite);
				plantSprite.PushTexture();
				break;
			case PlantSpriteStage.Growing:
				if (growingPlantStage >= plantData.GrowthSprites.Count)
				{
					Logger.Log($"Plant data does not contain growthsprites for index: {growingPlantStage} in plantData.GrowthSprites. Plant: {plantData.Plantname}");
					return;
				}
				plantSprite.spriteData = SpriteFunctions.SetupSingleSprite(plantData.GrowthSprites[growingPlantStage]);
				plantSprite.PushTexture();
				break;
		}

	}

	private void NaturalMutation()
	{
		//Chance to actually mutate
		if (random.Next(1, 2) != 1) return;

		//Stat mutations
		plantData.WeedResistance = StatMutation(plantData.WeedResistance, 100);
		plantData.WeedGrowthRate = BadStatMutation(plantData.WeedGrowthRate, 100);
		plantData.GrowthSpeed = StatMutation(plantData.GrowthSpeed, 100);
		plantData.Potency = StatMutation(plantData.Potency, 100);
		plantData.Endurance = StatMutation(plantData.Endurance, 100);
		plantData.Yield = StatMutation(plantData.Yield, 100);
		plantData.Lifespan = StatMutation(plantData.Lifespan, 100);
		switch (modification)
		{
			case PlantTrayModification.None:
				break;
			case PlantTrayModification.WeedResistance:
				plantData.WeedResistance = SpecialStatMutation(plantData.WeedResistance, 100);
				break;
			case PlantTrayModification.WeedGrowthRate:
				plantData.WeedGrowthRate = SpecialStatMutation(plantData.WeedGrowthRate, 100);
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
				plantData.Yield = SpecialStatMutation(plantData.Yield, 100);
				break;
			case PlantTrayModification.Lifespan:
				plantData.Lifespan = SpecialStatMutation(plantData.Lifespan, 100);
				break;
		}

		CheckMutation(plantData.WeedResistance, 0, 100);
		CheckMutation(plantData.WeedGrowthRate, 0, 100);
		CheckMutation(plantData.GrowthSpeed, 0, 100);
		CheckMutation(plantData.Potency, 0, 100);
		CheckMutation(plantData.Endurance, 0, 100);
		CheckMutation(plantData.Yield, 0, 100);
		CheckMutation(plantData.Lifespan, 0, 100);
		if (random.Next(100) > 95)
		{
			Mutation();
		}
	}

	private int CheckMutation(int num, int min, int max)
	{
		if (num < min)
		{
			return (min);
		}

		else if (num > max)
		{
			return (max);
		}

		return (num);
	}

	private static int BadStatMutation(float stat, float maxStat)
	{
		return ((int)stat + random.Next(-(int)Math.Ceiling((maxStat / 100f) * 5),
					(int)Math.Ceiling((maxStat / 100f) * 2)));
	}

	private static int SpecialStatMutation(float stat, float maxStat)
	{
		return ((int)stat + random.Next(0, (int)Math.Ceiling((maxStat / 100f) * 7)));
	}
	
	private static int StatMutation(float stat, float maxStat)
	{
		return ((int)stat + random.Next(-(int)Math.Ceiling((maxStat / 100f) * 2),
					(int)Math.Ceiling((maxStat / 100f) * 5)));
	}

	private void CropDeath()
	{
		if (plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
		{
			nutritionLevel = nutritionLevel + plantData.Potency;
			if (nutritionLevel > 100)
			{
				nutritionLevel = 100;
			}
		}

		if (plantData.PlantTrays.Contains(PlantTrays.Fungal_Vitality))
		{
			Dictionary<string, float> reagent = new Dictionary<string, float> { ["water"] = plantData.Potency };
			reagentContainer.AddReagents(reagent);
		}

		UpdatePlantGrowthStage(growingPlantStage, 0);
		plantSubStage = 0;
		plantHealth = 100;
		readyToHarvest = false;
		numberOfUpdatesAlive = 0;
		UpdatePlantStage(plantCurrentStage, PlantSpriteStage.Dead);
		plantData = null;
		hasPlant = false;
		readyProduce.Clear();
		UpdateHarvestFlag(harvestNotifier, false);
	}

	/// <summary>
	/// Spawns hidden produce ready for player to harvest
	/// Sets food component if it exists on the produce
	/// </summary>
	private void ProduceCrop()
	{
		for (int i = 0;
			i < (int)Math.Round(plantData.Yield / 10f);
			i++)
		{
			var produceObject = Spawn
				.ServerPrefab(plantData.ProduceObject, registerTile.WorldPositionServer, transform.parent)
				.GameObject;

			if (produceObject == null)
			{
				Logger.Log("plantData.ProduceObject returned an empty gameobject on spawn, skipping this crop produce", Category.Botany);
				continue;
			}

			CustomNetTransform netTransform = produceObject.GetComponent<CustomNetTransform>();
			var food = produceObject.GetComponent<GrownFood>();
			if (food != null)
			{
				food.plantData = new PlantData();
				food.plantData.SetValues(plantData);
				food.SetUpFood();
			}

			netTransform.DisappearFromWorldServer();
			readyProduce.Add(produceObject);
		}
	}

	/// <summary>
	/// Triggers plant in tray to mutate if possible
	/// Loads a random mutation out of the plants MutatesInTo list
	/// </summary>
	private void Mutation()
	{
		if (plantData.MutatesInTo.Count == 0) return;

		var tint = random.Next(plantData.MutatesInTo.Count);
		var data = plantData.MutatesInTo[tint];
		var oldPlantData = plantData;
		plantData.MutateTo(data);
		UpdatePlant(oldPlantData.Name, plantData.Name);
	}

	/// <summary>
	/// Server handles hand interaction with tray
	/// </summary>
	[Server]
	public void ServerPerformInteraction(HandApply interaction)
	{
		var slot = interaction.HandSlot;

		//If hand slot contains mutagen, use 5 mutagen mutate plant
		if (hasPlant)
		{
			if (plantData.MutatesInTo.Count > 0)
			{
				var objectContainer = slot?.Item?.GetComponent<ReagentContainer>();
				if (objectContainer != null)
				{
					if (!objectContainer.InSolidForm)
					{
						objectContainer.MoveReagentsTo(5, reagentContainer);
						if (reagentContainer.Contains("mutagen", 5))
						{
							reagentContainer.Contents["mutagen"] = reagentContainer.Contents["mutagen"] - 5;
							Mutation();
							return;
						}
					}
				}
			}
		}

		
		var objectItemAttributes = slot?.Item?.GetComponent<ItemAttributesV2>();
		if (objectItemAttributes != null)
		{
			//If hand slot contains Cultivator remove weeds
			if (objectItemAttributes.HasTrait(CommonTraits.Instance.Cultivator))
			{
				if (weedLevel > 0)
				{
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You remove the weeds from the {gameObject.ExpensiveName()}.",
						$"{interaction.Performer.name} uproots the weeds.");
				}
				weedNotifier.PushClear();
				weedLevel = 0;
				return;
			}

			//If hand slot contains Bucket water plants
			if (objectItemAttributes.HasTrait(CommonTraits.Instance.Bucket))
			{
				Chat.AddActionMsgToChat(interaction.Performer, $"You water the {gameObject.ExpensiveName()}.",
					$"{interaction.Performer.name} waters the tray.");
				reagentContainer.Contents["water"] = 100f;
				return;
			}

			//If hand slot contains Trowel remove plants
			if (objectItemAttributes.HasTrait(CommonTraits.Instance.Trowel))
			{
				if (hasPlant)
				{
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You dig out all of the {gameObject.ExpensiveName()}'s plants!",
						$"{interaction.Performer.name} digs out the plants in the {gameObject.ExpensiveName()}!");
					CropDeath();
				}

				UpdatePlantStage(plantCurrentStage, PlantSpriteStage.None);
				return;
			}
		}

		//If hand slot contains grown food, plant the food
		//This temporarily replaces the seed machine until it is implemented, see commented code for original compost behavior
		var foodObject = slot?.Item?.GetComponent<GrownFood>();
		if (foodObject != null)
		{
			hasPlant = true;
			plantData = new PlantData();
			plantData.SetValues(foodObject.plantData);
			UpdatePlant(null, plantData.Name);
			UpdatePlantGrowthStage(0, 0);
			UpdatePlantStage(PlantSpriteStage.None, PlantSpriteStage.Growing);
			Inventory.ServerVanish(slot);
			/*nutritionLevel = nutritionLevel + foodObject.plantData.Potency;
			Despawn.ServerSingle(interaction.HandObject);
			return;*/
		}

		//If hand slot contains seeds, plant the seeds
		var Object = slot?.Item?.GetComponent<SeedPacket>();
		if (Object != null)
		{
			hasPlant = true;
			plantData = new PlantData();
			plantData.SetValues(slot.Item.GetComponent<SeedPacket>().plantData);
			UpdatePlant(null, plantData.Name);
			UpdatePlantGrowthStage(0, 0);
			UpdatePlantStage(PlantSpriteStage.None, PlantSpriteStage.Growing);
			Inventory.ServerVanish(slot);

			return;
		}

		//If plant is ready to harvest then make produce visible and update plant state
		if (plantData != null && readyToHarvest)
		{
			for (int i = 0; i < readyProduce.Count; i++)
			{
				CustomNetTransform netTransform = readyProduce[i].GetComponent<CustomNetTransform>();
				netTransform.AppearAtPosition(registerTile.WorldPositionServer);
				netTransform.AppearAtPositionServer(registerTile.WorldPositionServer);
			}
			readyProduce.Clear();

			//If plant is Perennial then reset growth to the start of growing stage
			if (plantData.PlantTrays.Contains(PlantTrays.Perennial_Growth))
			{
				hasPlant = true;
				readyToHarvest = false;
				plantSubStage = 0;
				UpdatePlantGrowthStage(growingPlantStage,0);
				UpdatePlantStage(plantCurrentStage, PlantSpriteStage.Growing);
				UpdateHarvestFlag(harvestNotifier, false);
			}
			//Else remove plant from tray
			else
			{
				plantData = null;
				hasPlant = false;
				readyToHarvest = false;
				UpdatePlant(plantSyncString, null);
				UpdatePlantStage(plantCurrentStage, PlantSpriteStage.None);
				UpdateHarvestFlag(harvestNotifier, false);
			}
		}
		//Commenting unless this causes issues
		/*else
		{
			UpdatePlantStage(plantCurrentStage, PlantSpriteStage.None);
		}*/
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