using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using Chemistry;
using Mirror;
using Chemistry.Components;

/// <summary>
/// Where the magic happens in botany. This tray grows all of the plants
/// </summary>
public class HydroponicsTray : ManagedNetworkBehaviour, IInteractable<HandApply>
{
	public bool HasPlant => plantData?.FullyGrownSprite?.Texture != null;
	public bool ReadyToHarvest => plantCurrentStage == PlantSpriteStage.FullyGrown;


	[SyncVar(hook = nameof(UpdateHarvestFlag))]
	private bool showHarvestFlag;

	[SyncVar(hook = nameof(UpdateWeedsFlag))]
	private bool showWeedsFlag;

	[SyncVar(hook = nameof(UpdateWaterFlag))]
	private bool showWaterFlag;

	[SyncVar(hook = nameof(UpdateNutrimentFlag))]
	private bool showNutrimenetFlag;

	[SyncVar(hook = nameof(UpdatePlantStage))]
	private PlantSpriteStage plantCurrentStage;

	[SyncVar(hook = nameof(UpdatePlantGrowthStage))]
	private int growingPlantStage;

	[SyncVar(hook = nameof(UpdatePlant))] private string plantSyncString;

	[SerializeField] private RegisterTile registerTile;
	[SerializeField] private bool isSoilPile = false;

	[Tooltip("If this is set the plant will not grow/die over time, use it to keep wild findable plants alive")]
	[SerializeField]
	private bool isWild = false;

	[SerializeField] private List<DefaultPlantData> potentialWeeds = new List<DefaultPlantData>();
	[SerializeField] private PlantTrayModification modification = PlantTrayModification.None;
	[SerializeField] private ReagentContainer reagentContainer = null;
	[SerializeField] private Chemistry.Reagent nutriment = null;
	[SerializeField] private Chemistry.Reagent water = null;
	[SerializeField] private Chemistry.Reagent mutagen = null;
	[SerializeField] private SpriteHandler plantSprite = null;
	[SerializeField] private SpriteHandler harvestNotifier = null;
	[SerializeField] private SpriteHandler weedNotifier = null;
	[SerializeField] private SpriteHandler waterNotifier = null;
	[SerializeField] private SpriteHandler nutrimentNotifier = null;
	[SerializeField] private float tickRate = 0;


	private static readonly System.Random random = new System.Random();

	[SerializeField] private PlantData plantData;

	private readonly List<GameObject> readyProduce = new List<GameObject>();
	private float tickCount;
	private float weedLevel;

	public override void OnStartServer()
	{
		base.OnStartServer();
		EnsureInit();
		ServerInit();
	}

	public void ServerInit()
	{
		if (isSoilPile)
		{
			//only select plants that have valid produce
			plantData = PlantData.CreateNewPlant(DefaultPlantData.PlantDictionary.Values
				.Where(plant => plant.plantData.ProduceObject != null).PickRandom());
			UpdatePlant(null, plantData.Name);
			UpdatePlantStage(PlantSpriteStage.None, PlantSpriteStage.FullyGrown);
			UpdatePlantGrowthStage(growingPlantStage, plantData.GrowthSprites.Count - 1);
			ProduceCrop();
		}
		else
		{
			plantData = null;
			UpdatePlantStage(PlantSpriteStage.None, PlantSpriteStage.FullyGrown);
			UpdatePlant(plantSyncString, null);
			UpdatePlantGrowthStage(growingPlantStage, 0);
		}

		UpdateHarvestFlag(showHarvestFlag, false);
		UpdateWeedsFlag(showWeedsFlag, false);
		UpdateWaterFlag(showWaterFlag, false);
		UpdateNutrimentFlag(showNutrimenetFlag, false);
	}

	/// <summary>
	/// Load values passed from server when client connects
	/// </summary>
	public override void OnStartClient()
	{
		base.OnStartClient();
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
		//Only server checks plant status, wild plants do not grow
		if (!isServer || isWild) return;

		//Only update at set rate
		tickCount += Time.deltaTime;
		if (tickCount < tickRate)
		{
			return;
		}

		tickCount = 0f;


		if (HasPlant)
		{
			//Up plants age
			plantData.Age++;

			//Weeds checks
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
				plantData.Health += (((plantData.WeedResistance - 110f) / 100f) * (weedLevel / 10f) * 5);
				//Logger.Log("plantData.weed > " + plantData.PlantHealth);
			}

			//Water Checks
			if (reagentContainer[water] > 0)
			{
				reagentContainer.Subtract(new ReagentMix(water, .1f));
			}
			else if (!plantData.PlantTrays.Contains(PlantTrays.Fungal_Vitality))
			{
				plantData.Health += (plantData.Endurance - 101f) / 100f;
			}


			//Growth and harvest checks
			if (!ReadyToHarvest)
			{
				plantData.NextGrowthStageProgress += (int) Math.Ceiling(plantData.GrowthSpeed / 5f);

				if (plantData.NextGrowthStageProgress > 100)
				{
					plantData.NextGrowthStageProgress = 0;
					if (reagentContainer[nutriment] > 0 || plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
					{
						if (!plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
						{
							if (reagentContainer[nutriment] > 0)
							{
								reagentContainer.Subtract(new ReagentMix(nutriment, 1));
							}
						}

						if ((growingPlantStage + 1) < plantData.GrowthSprites.Count)
						{
							growingPlantStage = growingPlantStage + 1;
							plantCurrentStage = PlantSpriteStage.Growing;
							netIdentity.isDirty = true;
						}
						else
						{
							if (!ReadyToHarvest)
							{
								//plantData.NaturalMutation(modification);
								plantCurrentStage = PlantSpriteStage.FullyGrown;
								ProduceCrop();
							}

							showHarvestFlag = true;
							netIdentity.isDirty = true;
						}
					}
					else
					{
						plantData.Health += (((plantData.Endurance - 101f) / 100f) * 5);
						//Logger.Log("plantData.Nutriment > " + plantData.PlantHealth);
					}
				}
			}

			//Health checks
			if (plantData.Health < 0)
			{
				CropDeath();
			}
			else if (plantData.Age > plantData.Lifespan * 2500)
			{
				CropDeath();
			}
		}
		//Empty tray checks
		else
		{
			if (weedLevel < 10)
			{
				weedLevel += 0.01f;
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
					plantData = PlantData.CreateNewPlant(data.plantData);
					plantSyncString = plantData.Name;
					growingPlantStage = 0;
					plantCurrentStage = PlantSpriteStage.Growing;
					weedLevel = 0;
					//hasPlant = true;
					netIdentity.isDirty = true;
				}
			}
		}

		showNutrimenetFlag = reagentContainer[nutriment] == null || reagentContainer[nutriment] < 25;
		showWaterFlag = reagentContainer[water] == null || reagentContainer[water] < 25;
		showWeedsFlag = weedLevel > 5;
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
		plantSyncString = newPlantSyncString;
		if (newPlantSyncString == null)
		{
			plantData = null;
		}
		else if (DefaultPlantData.PlantDictionary.ContainsKey(plantSyncString))
		{
			plantData = PlantData.CreateNewPlant(DefaultPlantData.PlantDictionary[plantSyncString].plantData);
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
					Logger.Log(
						$"Plant data does not contain growthsprites for index: {growingPlantStage} in plantData.GrowthSprites. Plant: {plantData.Plantname}");
					return;
				}

				plantSprite.spriteData = SpriteFunctions.SetupSingleSprite(plantData.GrowthSprites[growingPlantStage]);
				plantSprite.PushTexture();
				break;
		}
	}


	private void CropDeath()
	{
		if (plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
		{
			reagentContainer.Add(new ReagentMix(nutriment, plantData.Potency));
		}

		if (plantData.PlantTrays.Contains(PlantTrays.Fungal_Vitality))
		{
			reagentContainer.Add(new ReagentMix(water, plantData.Potency));
		}

		growingPlantStage = 0;
		plantCurrentStage = PlantSpriteStage.Dead;
		plantData = null;
		readyProduce.Clear();
		showHarvestFlag = false;
		netIdentity.isDirty = true;
	}

	/// <summary>
	/// Spawns hidden produce ready for player to harvest
	/// Sets food component if it exists on the produce
	/// </summary>
	private void ProduceCrop()
	{
		for (int i = 0;
			i < (int) Math.Round(plantData.Yield / 10f);
			i++)
		{
			var produceObject = Spawn
				.ServerPrefab(plantData.ProduceObject, registerTile.WorldPositionServer, transform.parent)
				?.GameObject;

			if (produceObject == null)
			{
				Logger.Log("plantData.ProduceObject returned an empty gameobject on spawn, skipping this crop produce",
					Category.Botany);
				continue;
			}

			CustomNetTransform netTransform = produceObject.GetComponent<CustomNetTransform>();
			var food = produceObject.GetComponent<GrownFood>();
			if (food != null)
			{
				food.SetUpFood(plantData, modification);
			}

			netTransform.DisappearFromWorldServer();
			readyProduce.Add(produceObject);
		}
	}


	/// <summary>
	/// Server handles hand interaction with tray
	/// </summary>
	[Server]
	public void ServerPerformInteraction(HandApply interaction)
	{
		var slot = interaction.HandSlot;

		//If hand slot contains mutagen, use 5 mutagen mutate plant
		if (HasPlant)
		{
			if (plantData.MutatesInTo.Count > 0)
			{
				var objectContainer = slot?.Item?.GetComponent<ReagentContainer>();
				if (objectContainer != null)
				{
					objectContainer.MoveReagentsTo(5, reagentContainer);
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You add reagents to the {gameObject.ExpensiveName()}.",
						$"{interaction.Performer.name} adds reagents to the {gameObject.ExpensiveName()}.");
					if (reagentContainer[mutagen] >= 5)
					{
						reagentContainer.Subtract(new ReagentMix(mutagen, 5));
						plantData.Mutation();
						return;
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
					$"{interaction.Performer.name} waters the {gameObject.ExpensiveName()}.");
				reagentContainer.Add(new ReagentMix(water, 100));
				return;
			}

			//If hand slot contains Trowel remove plants
			if (objectItemAttributes.HasTrait(CommonTraits.Instance.Trowel))
			{
				if (HasPlant)
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
			if (HasPlant)
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You compost the {foodObject.name} in the {gameObject.ExpensiveName()}.",
					$"{interaction.Performer.name} composts {foodObject.name} in the {gameObject.ExpensiveName()}.");
				reagentContainer.Add(new ReagentMix(nutriment, foodObject.plantData.Potency));
				Despawn.ServerSingle(interaction.HandObject);
				return;
			}
			else
			{
				plantData = PlantData.CreateNewPlant(foodObject.plantData);
				UpdatePlant(null, plantData.Name);
				UpdatePlantGrowthStage(0, 0);
				UpdatePlantStage(PlantSpriteStage.None, PlantSpriteStage.Growing);
				Inventory.ServerVanish(slot);
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You plant the {foodObject.name} in the {gameObject.ExpensiveName()}.",
					$"{interaction.Performer.name} plants the {foodObject.name} in the {gameObject.ExpensiveName()}.");
			}
		}

		//If hand slot contains seeds, plant the seeds
		var Object = slot?.Item?.GetComponent<SeedPacket>();
		if (Object != null)
		{
			plantData = PlantData.CreateNewPlant(slot.Item.GetComponent<SeedPacket>().plantData);
			UpdatePlant(null, plantData.Name);
			UpdatePlantGrowthStage(0, 0);
			UpdatePlantStage(PlantSpriteStage.None, PlantSpriteStage.Growing);
			Inventory.ServerVanish(slot);
			Chat.AddActionMsgToChat(interaction.Performer,
				$"You plant the {Object.name} in the {gameObject.ExpensiveName()}.",
				$"{interaction.Performer.name} plants the {Object.name} in the {gameObject.ExpensiveName()}.");
			return;
		}

		//If plant is ready to harvest then make produce visible and update plant state
		if (plantData != null && ReadyToHarvest)
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
				plantData.NextGrowthStageProgress = 0;
				UpdatePlantGrowthStage(growingPlantStage, 0);
				UpdatePlantStage(plantCurrentStage, PlantSpriteStage.Growing);
				UpdateHarvestFlag(harvestNotifier, false);
			}
			//Else remove plant from tray
			else
			{
				plantData = null;
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