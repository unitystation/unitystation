using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.Botany;
using Chemistry;
using Chemistry.Components;
using Items;
using Items.Botany;
using Logs;

namespace Objects.Botany
{
	/// <summary>
	/// Where the magic happens in botany. This tray grows all of the plants
	/// </summary>
	public class HydroponicsTray : ManagedNetworkBehaviour, IInteractable<HandApply>, IServerSpawn
	{
		public bool HasPlant => plantData?.FullyGrownSpriteSO != null;
		public bool ReadyToHarvest => plantCurrentStage == PlantSpriteStage.FullyGrown;


		private bool showHarvestFlag;
		private bool showWeedsFlag;
		private bool showWaterFlag;
		private bool showNutrimenetFlag;
		private PlantSpriteStage plantCurrentStage;
		private int growingPlantStage;

		[SerializeField] private RegisterTile registerTile;
		[SerializeField] private bool isSoilPile = false;
		[Tooltip("If this is set the plant will not grow/die over time, use it to keep wild findable plants alive")]
		[SerializeField]
		private bool isWild = false;

		[Tooltip("Chooses what plants to place in the tray if the weed level gets too high.")]
		[SerializeField] private List<SeedPacket> potentialWeeds = new List<SeedPacket>();
		[Tooltip("Chooses what plants to place in the tray if it is a wild tray.")]
		[SerializeField] private List<SeedPacket> potentialWildPlants = new List<SeedPacket>();

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

		[SerializeField] private PlantData plantData;

		private readonly List<GameObject> readyProduce = new List<GameObject>();
		private float tickCount;
		private float weedLevel;

		#region Lifecycle

		public void Start()
		{
			if (isSoilPile) return;
			waterNotifier.PushClear();
			weedNotifier.PushClear();
			nutrimentNotifier.PushClear();
			harvestNotifier.PushClear();
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			EnsureInit();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			EnsureInit();
			ServerInit();
		}

		public void ServerInit()
		{
			if (isWild)
			{
				var data = potentialWildPlants.PickRandom();
				plantData = PlantData.CreateNewPlant(data.plantData);
				UpdatePlantStage(PlantSpriteStage.None, PlantSpriteStage.FullyGrown);
				UpdatePlantGrowthStage(growingPlantStage, plantData.GrowthSpritesSOs.Count - 1);
				ProduceCrop();
			}
			else
			{
				plantData = null;
				UpdatePlantStage(PlantSpriteStage.None, PlantSpriteStage.FullyGrown);
				UpdatePlantGrowthStage(growingPlantStage, 0);
			}

			UpdateHarvestFlag(showHarvestFlag, false);
			UpdateWeedsFlag(showWeedsFlag, false);
			UpdateWaterFlag(showWaterFlag, false);
			UpdateNutrimentFlag(showNutrimenetFlag, false);
		}

		private void EnsureInit()
		{
			if (registerTile == null) registerTile = GetComponent<RegisterTile>();

			if (isSoilPile) return;

			waterNotifier.PushClear();
			weedNotifier.PushClear();
			nutrimentNotifier.PushClear();
			harvestNotifier.PushClear();
		}

		#endregion Lifecycle

		/// <summary>
		/// Server updates plant status and updates clients as needed
		/// Server Side Only
		/// </summary>
		public override void UpdateMe()
		{
			if (isServer == false) return;
			//Only server checks plant status, wild plants do not grow
			if (isWild) return;

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
					weedLevel = weedLevel + ((0.1f) * (plantData.WeedGrowthRate / 10f));
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
					reagentContainer.Subtract(new ReagentMix(water, .01f));
				}
				else if (!plantData.PlantTrays.Contains(PlantTrays.Fungal_Vitality))
				{
					plantData.Health += (plantData.Endurance - 101f) / 100f;
				}


				//Growth and harvest checks
				if (!ReadyToHarvest)
				{
					plantData.NextGrowthStageProgress += (int)Math.Ceiling((plantData.GrowthSpeed / 160f) * plantData.GrowthSpritesSOs.Count) ;

					if (plantData.NextGrowthStageProgress > 100)
					{
						plantData.NextGrowthStageProgress = 0;
						if (reagentContainer[nutriment] > 0 || plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
						{
							if (!plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
							{
								if (reagentContainer[nutriment] > 0)
								{
									reagentContainer.Subtract(new ReagentMix(nutriment, 0.5f));
								}
							}

							if ((growingPlantStage + 1) < plantData.GrowthSpritesSOs.Count)
							{
								growingPlantStage = growingPlantStage + 1;
								UpdateSprite();
								plantCurrentStage = PlantSpriteStage.Growing;
							}
							else
							{
								if (!ReadyToHarvest)
								{
									//plantData.NaturalMutation(modification);
									plantCurrentStage = PlantSpriteStage.FullyGrown;
									ProduceCrop();
								}
								UpdateHarvestFlag(showHarvestFlag, true);
								UpdateSprite();
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

				// If there is no living plant in the tray and weed level is at least 10, choose a seed from the "Potential Weeds" list to grow in the tray.
				if (plantData == null)
				{
					if (weedLevel >= 10)
					{
						var data = potentialWeeds.PickRandom();
						plantData = PlantData.CreateNewPlant(data.plantData);
						growingPlantStage = 0;
						plantCurrentStage = PlantSpriteStage.Growing;
						weedLevel = 0;
						//hasPlant = true;
						UpdateSprite();
					}
				}
			}


			UpdateNutrimentFlag(showNutrimenetFlag, reagentContainer[nutriment] < 25);

			UpdateWaterFlag(showWaterFlag, reagentContainer[water] < 25);
			UpdateWeedsFlag(showWeedsFlag, weedLevel > 5);
		}

		/// <summary>
		/// Shows harvest ready sprite on tray if flag is set and tray is not a soil pile
		/// </summary>
		/// <param name="oldNotifier"></param>
		/// <param name="newNotifier"></param>
		private void UpdateHarvestFlag(bool oldNotifier, bool newNotifier)
		{
			if (isSoilPile) return;
			if (oldNotifier == newNotifier) return;
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
			if (oldNotifier == newNotifier) return;
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
			if (oldNotifier == newNotifier) return;

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
			if (oldNotifier == newNotifier) return;
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
					plantSprite.SetSpriteSO(plantData.FullyGrownSpriteSO);
					break;
				case PlantSpriteStage.Dead:
					plantSprite.SetSpriteSO(plantData.DeadSpriteSO);
					break;
				case PlantSpriteStage.Growing:
					if (growingPlantStage >= plantData.GrowthSpritesSOs.Count)
					{
						Loggy.Log(
							$"Plant data does not contain growthsprites for index: {growingPlantStage} in plantData.GrowthSprites. Plant: {plantData.PlantName}", Category.Botany);
						return;
					}

					plantSprite.SetSpriteSO(plantData.GrowthSpritesSOs[growingPlantStage]);
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
			UpdateSprite();
			plantData = null;
			readyProduce.Clear();
			UpdateHarvestFlag(showHarvestFlag, false);
		}

		/// <summary>
		/// Spawns hidden produce ready for player to harvest
		/// Sets food component if it exists on the produce
		/// </summary>
		private void ProduceCrop()
		{
			//Divides the yield value by 10 and then rounds it to the nearest integer to get the amount of objects harvested.
			for (int i = 0;
				i < (int)Math.Round(plantData.Yield / 10f);
				i++)
			{
				var produceObject = Spawn
					.ServerPrefab(plantData.ProduceObject, registerTile.WorldPositionServer, transform.parent)
					?.GameObject;

				if (produceObject == null)
				{
					Loggy.Log("plantData.ProduceObject returned an empty gameobject on spawn, skipping this crop produce",
						Category.Botany);
					continue;
				}

				UniversalObjectPhysics ObjectPhysics  = produceObject.GetComponent<UniversalObjectPhysics>();
				var food = produceObject.GetComponent<GrownFood>();
				if (food != null)
				{
					food.SetUpFood(plantData, modification);
				}

				ObjectPhysics.DisappearFromWorld();
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
				if (plantData.MutatesInToGameObject.Count > 0)
				{
					var objectContainer = slot?.Item.OrNull()?.GetComponent<ReagentContainer>();
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


			var objectItemAttributes = slot?.Item.OrNull()?.GetComponent<ItemAttributesV2>();
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
			var foodObject = slot?.Item.OrNull()?.GetComponent<GrownFood>();
			if (foodObject != null)
			{
				if (HasPlant)
				{
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You compost the {foodObject.name} in the {gameObject.ExpensiveName()}.",
						$"{interaction.Performer.name} composts {foodObject.name} in the {gameObject.ExpensiveName()}.");
					reagentContainer.Add(new ReagentMix(nutriment, foodObject.GetPlantData().Potency));
					_ = Despawn.ServerSingle(interaction.HandObject);
					return;
				}
				else
				{
					PlantData _plantData = foodObject.GetPlantData();
					plantData = PlantData.CreateNewPlant(_plantData);
					UpdatePlantGrowthStage(0, 0);
					UpdatePlantStage(PlantSpriteStage.None, PlantSpriteStage.Growing);
					UpdateHarvestFlag(showHarvestFlag, false);
					Inventory.ServerVanish(slot);
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You plant the {foodObject.name} in the {gameObject.ExpensiveName()}.",
						$"{interaction.Performer.name} plants the {foodObject.name} in the {gameObject.ExpensiveName()}.");
				}
			}

			//If hand slot contains seeds, plant the seeds
			var Object = slot?.Item.OrNull()?.GetComponent<SeedPacket>();
			if (Object != null)
			{
				plantData = PlantData.CreateNewPlant(slot.Item.GetComponent<SeedPacket>().plantData);
				UpdatePlantGrowthStage(0, 0);
				UpdatePlantStage(PlantSpriteStage.None, PlantSpriteStage.Growing);
				UpdateHarvestFlag(showHarvestFlag, false);
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
					UniversalObjectPhysics ObjectPhysics = readyProduce[i].GetComponent<UniversalObjectPhysics>();
					ObjectPhysics.AppearAtWorldPositionServer(registerTile.WorldPositionServer);
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
}
