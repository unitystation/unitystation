using System;
using UnityEngine;
using Mirror;
using Chemistry.Components;
using Chemistry;
using Core.Physics;
using HealthV2;
using Items;
using Objects.Botany;
using Items.Botany;
using Items.Food;
using Logs;
using Scripts.Core.Transform;

namespace Systems.Botany
{
	//Used when spawning the food
	[RequireComponent(typeof(ReagentContainer))]
	[DisallowMultipleComponent]
	public class GrownFood : NetworkBehaviour
	{
		[SerializeField]
		private PlantData plantData;

		public ReagentContainer reagentContainer;
		public Chemistry.Reagent nutrient;
		public GameObject SeedPacket => seedPacket;

		[SerializeField]
		public GameObject seedPacket = null;
		[SerializeField]
		private SpriteRenderer SpriteSizeAdjustment = null;
		[SerializeField]
		private SpriteHandler Sprite;
		[SerializeField]
		private Edible edible = default;

		[SyncVar(hook = nameof(SyncSize))]
		public float sizeScale = 1;

		[SerializeField] private ScaleSync scaleSync;

		private ItemAttributesV2 ItemAttributesV2;
		private UniversalObjectPhysics UniversalObjectPhysics;
		private Integrity Integrity;
		[SyncVar(hook = nameof(SyncSlippery))]
		public bool HasSlippery = false;

		[SyncVar(hook = nameof(SyncBlueSpaceActivity))]
		public bool HasBlueSpaceActivity = false;

		public void SyncBlueSpaceActivity(bool oldScale, bool newScale)
		{
			HasBlueSpaceActivity = newScale;

			if (newScale)
			{
				ItemAttributesV2.AddTrait(CommonTraits.Instance.BluespaceActivity);
			}
			else
			{
				ItemAttributesV2.RemoveTrait(CommonTraits.Instance.BluespaceActivity);
			}
		}

		public void SyncSlippery(bool oldScale, bool newScale)
		{
			HasSlippery = newScale;

			if (newScale)
			{
				ItemAttributesV2.AddTrait(CommonTraits.Instance.Slippery);
			}
			else
			{
				ItemAttributesV2.RemoveTrait(CommonTraits.Instance.Slippery);
			}
		}


		public void SyncSize(float oldScale, float newScale)
		{
			if (scaleSync is not null)
			{
				scaleSync.SetScale(new Vector3(sizeScale, sizeScale, sizeScale));
			}
			else
			{
				sizeScale = newScale;
				SpriteSizeAdjustment.transform.localScale = new Vector3((sizeScale), (sizeScale), (sizeScale));
			}
		}

		public PlantData GetPlantData()
		{
			PlantData _plantData = null;
			if (plantData.FullyGrownSpriteSO == null)
			{
				_plantData = SeedPacket.GetComponent<SeedPacket>().plantData;
			}
			else
			{
				_plantData = plantData;
			}

			return _plantData;
		}


		public void Awake()
		{
			ItemAttributesV2 = this.GetComponentCustom<ItemAttributesV2>();

			UniversalObjectPhysics = this.GetComponentCustom<UniversalObjectPhysics>();

			Integrity = this.GetComponentCustom<Integrity>();
		}

		public void Start()
		{
			SyncSize(sizeScale, sizeScale);
			if (reagentContainer.ReagentMixTotal == 0)
			{
				SetUpFood(plantData, PlantTrayModification.None);
			}
		}

		/// <summary>
		/// Called when plant creates food
		/// </summary>
		public void SetUpFood(PlantData newPlantData, PlantTrayModification modification)
		{

			plantData = PlantData.MutateNewPlant(newPlantData, modification);
			SyncSize(sizeScale, 0.5f + (newPlantData.Potency / 200f));
			SetupChemicalContents();
			if (edible != null)
			{
				SetupEdible();
			}

			foreach (var Tray in plantData.PlantTrays)
			{

				switch (Tray)
				{
					case PlantTrays.Bluespace_Activity:
						SyncBlueSpaceActivity(true, true);
						//Teleporting stuff
						break;
					case PlantTrays.Hypodermic_Needles:
						ItemAttributesV2.OnMelee += OnPrickle;
						break;
					case PlantTrays.Slippery_Skin:
						//Slippery Skin
						SyncSlippery(true, true);
						break;
					case PlantTrays.Densified_Chemicals:
						reagentContainer.Multiply(2);
						// Double reagents
						break;
					case PlantTrays.Liquid_Content:
						UniversalObjectPhysics.OnImpact.AddListener(OnImpact);
						ItemAttributesV2.OnSlipOn += OnSlipOn;
						//so, Causes the plant to squash when thrown or slipped on, applying the reagents inside on the target and destroying the plant.
						break;
					case PlantTrays.Separated_Chemicals:
						reagentContainer.StopReactions = true;
						// Basically no reaction in plant
						break;

					case PlantTrays.Fire_Resistance:
						Integrity.Resistances.FireProof = true;
						Integrity.Resistances.Flammable = false;
						Integrity.Resistances.LavaProof = true;
						break;

				}

			}

		}

		public void OnSlipOn(UniversalObjectPhysics WhoseSlipping)
		{
			reagentContainer.Spill(transform.position.RoundToInt(), reagentContainer.ReagentMixTotal);
		}

		public void OnImpact(UniversalObjectPhysics Impact, Vector2 Force)
		{
			reagentContainer.Spill(transform.position.RoundToInt(), reagentContainer.ReagentMixTotal);
		}

		public void SetPlantData(PlantData newData)
		{
			plantData = newData;
		}



		public int hitCount;
		public void OnPrickle(GameObject gameObject, GameObject victim)
		{
			if (hitCount >= 4) return;

			var LHB = victim.GetComponent<LivingHealthMasterBase>();
			if (LHB.reagentPoolSystem == null) return;

			int hitsRemaining = 4 - hitCount;

			float portion = reagentContainer.ReagentMixTotal / hitsRemaining;

			var InjectingReagents = reagentContainer.TakeReagents(portion);

			LHB.reagentPoolSystem.BloodPool.Add(InjectingReagents);

			hitCount++;

			if (hitCount >= 4)
			{
				_ = Despawn.ServerSingle(gameObject);
			}
		}

		/// <summary>
		/// Takes initial values and scales them based on potency
		/// </summary>
		private void SetupChemicalContents()
		{
			ReagentMix CurrentReagentMix = new ReagentMix();
			foreach (var reagentAndAmount in plantData.ReagentProduction)
			{
				CurrentReagentMix.Add(reagentAndAmount.ChemistryReagent, reagentAndAmount.percentage);
			}

			reagentContainer.Add(CurrentReagentMix);

			reagentContainer.Multiply( plantData.Potency / 100f * 15f );
		}

		/// <summary>
		/// Set NutritionLevel to be equal to nuriment amount
		/// </summary>
		private void SetupEdible()
		{
			//DOES NOT WORK! DO NOT USE THIS!
			// edible.NutritionLevel = Mathf.FloorToInt(reagentContainer[nutrient]);
		}
	}
}
