using System;
using System.Collections.Generic;
using Chemistry;
using Logs;
using Mirror;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.Core.Utils;
using US13.Items;
using US13.Items.Botany;
using US13.Items.Tool;
using US13.Items.Traits;
using US13.Managers.UpdateManager;
using US13.Systems.Botany;
using US13.Systems.Construction;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Objects;
using Util;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.Objects.Botany
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

		[SerializeField] private ReagentContainer reagentContainer = null;
		[SerializeField] private global::Chemistry.Reagent nutriment = null;
		[SerializeField] private global::Chemistry.Reagent water = null;
		[SerializeField] private global::Chemistry.Reagent mutagen = null;

		[SerializeField] private global::Chemistry.Reagent PestKiller = null;
		[SerializeField] private global::Chemistry.Reagent WeedKiller = null;
		[SerializeField] private global::Chemistry.Reagent Left4Zed = null;

		[SerializeField] private global::Chemistry.Reagent RobustHarvest  = null;
		[SerializeField] private global::Chemistry.Reagent EZNutrient  = null;


		[SerializeField] private SpriteHandler plantSprite = null;
		[SerializeField] private SpriteHandler harvestNotifier = null;
		[SerializeField] private SpriteHandler weedNotifier = null;
		[SerializeField] private SpriteHandler waterNotifier = null;
		[SerializeField] private SpriteHandler nutrimentNotifier = null;
		[SerializeField] private float tickRate = 0;
		[SerializeField] private bool RandomisedReagents = true;
		private PlantData plantData;
		public PlantData PlantData => plantData;

		private readonly List<GameObject> readyProduce = new List<GameObject>();
		private float tickCount;
		private float weedLevel;
		private float pestLevel;


		private Machine Machine;
		private bool HasMachine;

		#region Lifecycle

		public void Awake()
		{
			Machine = this.GetComponent<Machine>();
			HasMachine = Machine;
		}

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
			if (RandomisedReagents && info.WasMapspawn)
			{
				reagentContainer.TakeReagents(99);
				var mix = new ReagentMix();
				mix.Add(water, RNG.GetRandomNumber(1f, 40f));
				mix.Add(nutriment, RNG.GetRandomNumber(1f, 40f));
				reagentContainer.Add(mix);
			}

			if (info.WasMapspawn)
			{
				weedLevel = RNG.GetRandomNumber(0f, 4f);
				pestLevel = RNG.GetRandomNumber(0f, 4f);
			}

			EnsureInit();
			ServerInit();
		}

		public void ServerInit()
		{
			if (isWild)
			{
				var data = EnumerableExt.PickRandom(potentialWildPlants);
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

		public void ReagentChecks()
		{
			if (reagentContainer[PestKiller] >= 1)
			{
				reagentContainer.Subtract(new ReagentMix(PestKiller, 1));
				pestLevel -= 1;

				if (pestLevel < 0)
				{
					pestLevel = 0;
				}
				plantData.Health -= 1;
			}

			if (reagentContainer[WeedKiller] >= 1)
			{
				reagentContainer.Subtract(new ReagentMix(WeedKiller, 1));
				weedLevel -= 1;
				if (weedLevel < 0)
				{
					weedLevel = 0;
				}
				plantData.Health -= 1;
			}

			if (reagentContainer[mutagen] >= 5)
			{
				reagentContainer.Subtract(new ReagentMix(mutagen, 5));
				plantData.Mutation();
			}

		}

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
				ReagentChecks();

				//Weeds checks
				if (weedLevel < 10)
				{
					weedLevel = weedLevel + ((0.19125f) * (plantData.WeedGrowthRate / 10f));
					if (weedLevel > 10)
					{
						weedLevel = 10;
					}
				}

				if (weedLevel > 9.5f && !plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
				{
					plantData.Health += (((plantData.WeedResistance - 110f) / 100f) * (weedLevel / 10f) * 5);
					//Loggy.Log("plantData.weed > " + plantData.PlantHealth);
				}

				if (isSoilPile == false)
				{
					if (pestLevel > 10)
					{
						plantData.Health -= 0.5f / GetMachineMultiplier();
					}
					else
					{
						pestLevel += 0.0025f / GetMachineMultiplier();
					}

				}


				//Water Checks
				if (reagentContainer[water] > 0)
				{
					reagentContainer.Subtract(new ReagentMix(water, 0.01275f / GetMachineMultiplier()));
				}
				else if (plantData.PlantTrays.Contains(PlantTrays.Fungal_Vitality) == false)
				{
					plantData.Health += (plantData.Endurance - 101f) / 100f;
				}


				//Growth and harvest checks
				if (ReadyToHarvest == false)
				{
					plantData.NextGrowthStageProgress += (int)Math.Ceiling(((plantData.GrowthSpeed *  GetMachineMultiplier()) / 160f) * plantData.GrowthSpritesSOs.Count) ;

					if (plantData.NextGrowthStageProgress > 100)
					{
						plantData.NextGrowthStageProgress = 0;
						if (reagentContainer[nutriment] > 0 || plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation))
						{
							if (plantData.PlantTrays.Contains(PlantTrays.Weed_Adaptation) == false)
							{
								if (reagentContainer[nutriment] > 0)
								{
									reagentContainer.Subtract(new ReagentMix(nutriment, 0.375f /   GetMachineMultiplier()));
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
								if (ReadyToHarvest == false)
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
							//Loggy.Log("plantData.Nutriment > " + plantData.PlantHealth);
						}
					}
				}



				//Health checks
				if (plantData.Health < 0)
				{
					CropDeath();
				}
				else if (plantData.Age > plantData.Lifespan * 2500 *  GetMachineMultiplier())
				{
					CropDeath();
				}
				else if (plantData.Health > 30)
				{
					plantData.Health = 30;
				}
			}
			//Empty tray checks
			else
			{
				if (weedLevel < 10)
				{
					weedLevel += 0.005f /   GetMachineMultiplier();
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
						var data = EnumerableExt.PickRandom(potentialWeeds);
						plantData = PlantData.CreateNewPlant(data.plantData);
						growingPlantStage = 0;
						plantCurrentStage = PlantSpriteStage.Growing;
						weedLevel = 0;
						//hasPlant = true;
						UpdateSprite();
					}
				}
			}


			UpdateNutrimentFlag(showNutrimenetFlag, reagentContainer[nutriment] < 10);

			UpdateWaterFlag(showWaterFlag, reagentContainer[water] < 10);
			UpdateWeedsFlag(showWeedsFlag, weedLevel > 5 || pestLevel > 5);
		}

		/// <summary>w
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
						Loggy.Info(
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
			pestLevel = 0;
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
			bool? IncreasesMutationChanceState = null;

			PlantTrayModification modification = PlantTrayModification.None;
			if (reagentContainer[Left4Zed] >= 2.4f)
			{
				reagentContainer.Subtract(new ReagentMix(Left4Zed, 2.5f));
				IncreasesMutationChanceState = true;
			}

			if (reagentContainer[RobustHarvest] >= 2.4f)
			{
				reagentContainer.Subtract(new ReagentMix(RobustHarvest, 2.5f));
				IncreasesMutationChanceState = false;
				modification = PlantTrayModification.Yield;
			}

			if (reagentContainer[EZNutrient] >= 2.4f)
			{
				reagentContainer.Subtract(new ReagentMix(EZNutrient, 2.5f));
				IncreasesMutationChanceState = null;
				modification = PlantTrayModification.Potency;
			}

			//Divides the yield value by 10 and then rounds it to the nearest integer to get the amount of objects harvested.
			var Number = Mathf.Min(Mathf.Round(plantData.Yield / 10f), 10);

			for (int i = 0;
				i < (int)Number;
				i++)
			{
				var produceObject = Spawn
					.ServerPrefab(plantData.ProduceObject, registerTile.WorldPositionServer, transform.parent)
					?.GameObject;

				if (produceObject == null)
				{
					Loggy.Info("plantData.ProduceObject returned an empty gameobject on spawn, skipping this crop produce",
						Category.Botany);
					continue;
				}

				PlantData.StatMutationType StatMutationTypeModifyer = PlantData.StatMutationType.Normal;

				if (GetMachineMultiplier() > 3)
				{
					StatMutationTypeModifyer = PlantData.StatMutationType.Special;
				}

				UniversalObjectPhysics ObjectPhysics  = produceObject.GetComponent<UniversalObjectPhysics>();
				var food = produceObject.GetComponent<GrownFood>();
				if (food != null)
				{
					food.SetUpFood(plantData, modification, IncreasesMutationChanceState, StatMutationTypeModifyer);
				}

				ObjectPhysics.DisappearFromWorld();
				readyProduce.Add(produceObject);
			}
		}

		public float GetMachineMultiplier()
		{
			if (HasMachine == false)
			{
				return 1;
			}

			return Machine.GetPartMultiplier();
		}

		/// <summary>
		/// Server handles hand interaction with tray
		/// </summary>
		[Server]
		public void ServerPerformInteraction(HandApply interaction)
		{
			var slot = interaction.HandSlot;

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

					if ((pestLevel > 5) == false)
					{
						UpdateWeedsFlag(showWeedsFlag, false);
					}

					weedLevel = 0;
					return;
				}


				//If hand slot contains Trowel remove plants
				if (objectItemAttributes.HasTrait(CommonTraits.Instance.Trowel))
				{
					if (HasPlant)
					{
						ToolUtils.ServerUseToolWithActionMessages(
							interaction, 3,
							$"You start digging up {gameObject.ExpensiveName()}'s plants!",
							$"{interaction.Performer.ExpensiveName()} Starts to dig out {gameObject.ExpensiveName()}'s plants!",
							$"You dig out all of the {gameObject.ExpensiveName()}'s plants!",
							$"{interaction.Performer.name} digs out the plants in the {gameObject.ExpensiveName()}!",
							DigUp
						);
					}
					else
					{
						UpdatePlantStage(plantCurrentStage, PlantSpriteStage.None);
					}


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
					reagentContainer.Add(new ReagentMix(nutriment, foodObject.reagentContainer[nutriment]*4));
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
			if (Object != null && HasPlant == false)
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
			else if (plantData == null)
			{
				UpdatePlantStage(plantCurrentStage, PlantSpriteStage.None);
			}
		}

		public void DigUp()
		{
			CropDeath();
			UpdatePlantStage(plantCurrentStage, PlantSpriteStage.None);
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
