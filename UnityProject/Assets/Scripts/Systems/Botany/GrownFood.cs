using UnityEngine;
using Mirror;
using Chemistry.Components;
using Chemistry;
using Objects.Botany;
using Items.Botany;
using Items.Food;
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

		public void Start()
		{
			SyncSize(sizeScale, sizeScale);
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
		}

		public void SetPlantData(PlantData newData)
		{
			plantData = newData;
		}

		/// <summary>
		/// Takes initial values and scales them based on potency
		/// </summary>
		private void SetupChemicalContents()
		{
			ReagentMix CurrentReagentMix = new ReagentMix();
			foreach (var reagentAndAmount in plantData.ReagentProduction)
			{
				CurrentReagentMix.Add(reagentAndAmount.ChemistryReagent, reagentAndAmount.Amount);
			}

			reagentContainer.Add(CurrentReagentMix);

			reagentContainer.Multiply( plantData.Potency / 100f * 2.5f ); //40 Potency = * 1
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
