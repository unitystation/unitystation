using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;

/// <summary>
/// Where the magic happens in botany. This tray grows all of the plants
/// </summary>
public class HydroponicsTray : NetworkBehaviour, IInteractable<HandApply>
{
	public bool syncHarvestNotifier;
	public bool syncWeedNotifier;
	public bool syncWaterNotifier;
	public bool syncNutrimentNotifier;

	private RegisterTile registerTile;

	public PlantSpriteStage plantSyncStage;
	public int growingPlantStage;
	public string plantSyncString;

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

	private static System.Random random = new System.Random();

	public float tickRate = 0.1f;
	public float tickCount;
	public float weedLevel; //10
	public float nutritionLevel = 100;

	public int numberOfUpdatesAlive;
	public int plantSubStage;
	public float plantHealth = 100;
	public bool readyToHarvest;
	private bool IsServer; //separate flag as NetworkBehaviour isServer is not accurate when destroying an object

	public override void OnStartServer()
	{
		EnsureInit();
		UpdateManager.Instance.Add(ServerUpdate);
		IsServer = true;
		if (isSoilPile)
		{
			hasPlant = false;
			plantData = new PlantData();
			plantData.SetValues(DefaultPlantData.PlantDictionary.Values.PickRandom());
			SyncPlant(plantData.Name);
			//NaturalMutation();
			SyncStage(PlantSpriteStage.FullyGrown);
			readyToHarvest = true;
			ProduceCrop();
		}
	}

	public void OnEnable()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (registerTile != null) return;
		registerTile = GetComponent<RegisterTile>();
	}

	public void OnDisable()
	{
		if (IsServer)
		{
			UpdateManager.Instance.Remove(ServerUpdate);
		}
	}

	void ServerUpdate()
	{
		if (!isServer) return;

		tickCount += Time.deltaTime;
		if (tickCount > tickRate)
		{
			DoTick();
			tickCount = 0f;
		}
	}

	public void DoTick()
	{
		if (hasPlant)
		{
			if (weedLevel < 10)
			{
				weedLevel = weedLevel + ((0.1f) * (plantData.WeedGrowthRate / 10f));
				if (weedLevel > 10)
				{
					weedLevel = 10;
				}
			}

			if (weedLevel > 9.5f && !plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
			{
				plantHealth = plantHealth + (((plantData.WeedResistance - 11f) / 10f) * (weedLevel / 10f) * 5);
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
				plantSubStage = plantSubStage + plantData.GrowthSpeed;

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
							SyncGrowingPlantStage(growingPlantStage + 1);
							SyncStage(PlantSpriteStage.Growing);
						}
						else
						{
							if (!readyToHarvest)
							{
								NaturalMutation();
								SyncStage(PlantSpriteStage.FullyGrown);
								readyToHarvest = true;
								ProduceCrop();
								SyncHarvest(true);
							}
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
					SyncPlant(plantData.Name);
					SyncGrowingPlantStage(0);
					SyncStage(PlantSpriteStage.Growing);
					weedLevel = 0;
					hasPlant = true;
				}
			}
		}

		if (nutritionLevel < 25)
		{
			SyncNutriment(true);
		}
		else
		{
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
		else
		{
			SyncWater(true);
		}

		if (weedLevel > 5)
		{
			SyncWeed(true);
		}
		else
		{
			SyncWeed(false);
		}
	}

	[Server]
	private void SendUpdateToNearbyPlayers()
	{
		PlantTrayMessage.SendToNearbyPlayers(gameObject, plantSyncString, growingPlantStage, plantSyncStage,
			syncHarvestNotifier, syncWeedNotifier, syncWaterNotifier, syncNutrimentNotifier);
	}

	private void SyncHarvest(bool newNotifier)
	{
		if (isSoilPile
			|| newNotifier == syncHarvestNotifier) return;

		syncHarvestNotifier = newNotifier;
		if (syncHarvestNotifier)
		{
			harvestNotifier.PushTexture();
		}
		else
		{
			harvestNotifier.PushClear();
		}

		//Force a refresh on nearby clients
		if (isServer) SendUpdateToNearbyPlayers();
	}

	private void SyncWeed(bool newNotifier)
	{
		if (isSoilPile ||
			newNotifier == syncWeedNotifier) return;

		syncWeedNotifier = newNotifier;
		if (syncWeedNotifier)
		{
			weedNotifier.PushTexture();
		}
		else
		{
			weedNotifier.PushClear();
		}

		//Force a refresh on nearby clients
		if (isServer) SendUpdateToNearbyPlayers();
	}

	private void SyncWater(bool newNotifier)
	{
		if (isSoilPile ||
			newNotifier == syncWaterNotifier) return;

		syncWaterNotifier = newNotifier;
		if (syncWaterNotifier)
		{
			waterNotifier.PushTexture();
		}
		else
		{
			waterNotifier.PushClear();

		}

		//Force a refresh on nearby clients
		if (isServer) SendUpdateToNearbyPlayers();
	}

	private void SyncNutriment(bool newNotifier)
	{
		if (isSoilPile ||
			newNotifier == syncNutrimentNotifier) return;

		syncNutrimentNotifier = newNotifier;
		if (syncNutrimentNotifier)
		{

			nutrimentNotifier.PushTexture();
		}
		else
		{

			nutrimentNotifier.PushClear();
		}

		//Force a refresh on nearby clients
		if (isServer) SendUpdateToNearbyPlayers();
	}

	private void SyncStage(PlantSpriteStage newStage)
	{
		if (newStage == plantSyncStage) return;

		plantSyncStage = newStage;
		if (plantData == null)
		{
			//FIXME: BOD PLZ FIX BOTANY PLANT DATA IS NULL!
			return;
		}
		switch (plantSyncStage)
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

		//Force a refresh on nearby clients
		if (isServer) SendUpdateToNearbyPlayers();
	}

	private void SyncGrowingPlantStage(int newStage)
	{
		growingPlantStage = newStage;

		//Force a refresh on nearby clients
		if (isServer) SendUpdateToNearbyPlayers();
	}

	private void SyncPlant(string newPlantString)
	{
		if (newPlantString == plantSyncString) return;

		plantSyncString = newPlantString;

		if (DefaultPlantData.PlantDictionary.ContainsKey(plantSyncString))
		{
			plantData = DefaultPlantData.PlantDictionary[plantSyncString].plantData;
		}

		//Force a refresh on nearby clients
		if (isServer) SendUpdateToNearbyPlayers();
	}

	public void ReceiveMessage(string plantString, int growingStage, PlantSpriteStage spriteStage,
		bool harvestSync, bool weedSync, bool waterSync, bool nutrimentSync)
	{
		plantSyncString = plantString;

		SyncHarvest(harvestSync);
		SyncWeed(weedSync);
		SyncWater(waterSync);
		SyncNutriment(nutrimentSync);

		if (DefaultPlantData.PlantDictionary.ContainsKey(plantSyncString))
		{
			plantData = DefaultPlantData.PlantDictionary[plantSyncString].plantData;
		}

		growingPlantStage = growingStage;

		plantSyncStage = spriteStage;

		switch (plantSyncStage)
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
				plantSprite.spriteData =
					SpriteFunctions.SetupSingleSprite(plantData.GrowthSprites[growingPlantStage]);
				plantSprite.PushTexture();
				break;
		}
	}

	private void NaturalMutation()
	{
		plantData.WeedResistance = StatMutation(plantData.WeedResistance, 10);
		plantData.WeedGrowthRate = StatMutation(plantData.WeedGrowthRate, -10);
		plantData.GrowthSpeed = StatMutation(plantData.GrowthSpeed, 10);
		plantData.Potency = StatMutation(plantData.Potency, 100);
		plantData.Endurance = StatMutation(plantData.Endurance, 100);
		plantData.Yield = StatMutation(plantData.Yield, 10);
		plantData.Lifespan = StatMutation(plantData.Lifespan, 100);
		switch (modification)
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
				plantData.GrowthSpeed = SpecialStatMutation(plantData.GrowthSpeed, 10);
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

		CheckMutation(plantData.WeedResistance, 0, 10);
		CheckMutation(plantData.WeedGrowthRate, 0, 10);
		CheckMutation(plantData.GrowthSpeed, 0, 10);
		CheckMutation(plantData.Potency, 0, 100);
		CheckMutation(plantData.Endurance, 0, 100);
		CheckMutation(plantData.Yield, 0, 10);

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

		SyncGrowingPlantStage(0);
		plantSubStage = 0;
		plantHealth = 100;
		readyToHarvest = false;
		numberOfUpdatesAlive = 0;
		SyncStage(PlantSpriteStage.Dead);
		plantData = null;
		hasPlant = false;
		readyProduce.Clear();
		SyncHarvest(false);
	}

	private void ProduceCrop()
	{
		for (int i = 0;
			i < plantData.Yield;
			i++)
		{
			var _Object = Spawn
				.ServerPrefab(plantData.ProduceObject, registerTile.WorldPositionServer, transform.parent)
				.GameObject;

			if (_Object == null)
			{
				Logger.Log("plantData.ProduceObject returned an empty gameobject on spawn, skipping this crop produce", Category.Botany);
				continue;
			}

			CustomNetTransform netTransform = _Object.GetComponent<CustomNetTransform>();
			var food = _Object.GetComponent<GrownFood>();
			if (food != null)
			{
				food.plantData = new PlantData();
				food.plantData.SetValues(plantData);
				food.SetUpFood();
			}

			netTransform.DisappearFromWorldServer();
			readyProduce.Add(_Object);
		}
	}

	private void Mutation()
	{
		if (plantData.MutatesInTo.Count == 0) return;

		var tint = random.Next(plantData.MutatesInTo.Count);
		var data = plantData.MutatesInTo[tint];
		plantData.MutateTo(data);
		SyncPlant(plantData.Name);
	}


	public void ServerPerformInteraction(HandApply interaction)
	{
		var slot = interaction.HandSlot;

		var objectContainer = slot?.Item?.GetComponent<ReagentContainer>();
		if (hasPlant)
		{
			if (plantData.MutatesInTo.Count > 0)
			{
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

			if (objectItemAttributes.HasTrait(CommonTraits.Instance.Bucket))
			{
				Chat.AddActionMsgToChat(interaction.Performer, $"You water the {gameObject.ExpensiveName()}.",
					$"{interaction.Performer.name} waters the tray.");
				reagentContainer.Contents["water"] = 100f;
				return;
			}

			if (objectItemAttributes.HasTrait(CommonTraits.Instance.Trowel))
			{
				if (hasPlant)
				{
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You dig out all of the {gameObject.ExpensiveName()}'s plants!",
						$"{interaction.Performer.name} digs out the plants in the {gameObject.ExpensiveName()}!");
					CropDeath();
				}

				SyncStage(PlantSpriteStage.None);
				return;
			}
		}

		var foodObject = slot?.Item?.GetComponent<GrownFood>();
		if (foodObject != null)
		{
			nutritionLevel = nutritionLevel + foodObject.plantData.Potency;
			Despawn.ServerSingle(interaction.HandObject);
			return;
		}

		var Object = slot?.Item?.GetComponent<SeedPacket>();
		if (Object != null)
		{
			hasPlant = true;
			plantData = new PlantData();
			plantData.SetValues(slot.Item.GetComponent<SeedPacket>().plantData);
			SyncPlant(plantData.Name);
			SyncGrowingPlantStage(0);
			SyncStage(PlantSpriteStage.Growing);
			Inventory.ServerVanish(slot);

			//Force a quick refresh:
			SendUpdateToNearbyPlayers();
			return;
		}

		if (plantData != null)
		{
			if (readyToHarvest)
			{
				for (int i = 0; i < readyProduce.Count; i++)
				{
					CustomNetTransform netTransform = readyProduce[i].GetComponent<CustomNetTransform>();
					netTransform.AppearAtPosition(registerTile.WorldPositionServer);
					netTransform.AppearAtPositionServer(registerTile.WorldPositionServer);
				}

				readyProduce.Clear();
				if (plantData.PlantTrays.Contains(PlantTrays.Perennial_Growth))
				{
					hasPlant = true;
					readyToHarvest = false;
					plantSubStage = 0;
					SyncGrowingPlantStage(0);
					SyncStage(PlantSpriteStage.Growing);
					SyncHarvest(false);
				}
				else
				{
					plantData = null;
					hasPlant = false;
					readyToHarvest = false;
					SyncStage(PlantSpriteStage.None);
					SyncHarvest(false);
				}
			}
		}
		else
		{
			SyncStage(PlantSpriteStage.None);
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