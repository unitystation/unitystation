using Chemistry;
using Items.Food;
using System.Collections.Generic;
using UnityEngine;
using HealthV2;

namespace Items.Implants.Organs
{
	public class FatProducingStomach : Stomach
	{
		[SerializeField] private int MaxFat = 5;
		[SerializeField] private GameObject fatPrefab;
		[SerializeField] private float StartingFatCount = 100;

		public override float ProcessContent()
		{
			foreach (ItemSlot slot in stomachContents.GetItemSlots()) //Gets reagent from food in stomach, and then despawns food once digested.
			{
				if (slot.IsEmpty == true) continue;

				ReagentMix chemicalsFromFood = new ReagentMix();
				if (slot.ItemObject.TryGetComponent<Edible>(out var edible) == false)
				{
					if (slot.ItemObject.TryGetComponent<NotHandEdible>(out var notHandEdible) == false) continue;
					chemicalsFromFood.Add(notHandEdible.TakeReagentsFromFood(DigesterAmountPerSecond));
				}
				else chemicalsFromFood.Add(edible.TakeReagentsFromFood(DigesterAmountPerSecond));

				if (chemicalsFromFood.Total == 0)
				{
					Inventory.ServerDespawn(slot);
					continue;
				}

				reagentContainer.Add(chemicalsFromFood);
				break;
			}

			return Digest(); //Adds nutrient to fat and returns left over.
		}

		public override float Digest()
		{
			float newNutrient = 0;

			foreach (Reagent nutrientReagent in nutrientReagents)
			{
				float amount = reagentContainer.AmountOfReagent(nutrientReagent);
				if (amount > 0)
				{
					newNutrient += amount;
					reagentContainer.CurrentReagentMix.reagents.Remove(nutrientReagent);
				}
			}
			RelatedPart.HealthMaster.CirculatorySystem.BloodPool.Add(reagentContainer.CurrentReagentMix.Take(DigesterAmountPerSecond)); //Adds non nutrient chemicals to blood pool.

			List<BodyFat> bodyFat = RelatedPart.HealthMaster.DigestiveSystem.BodyFat;

			foreach (BodyFat fat in bodyFat)
			{
				newNutrient = fat.AddNutrient(newNutrient);
				if (newNutrient == 0) break;
			}

			return SpawnFat(newNutrient);
		}

		public float SpawnFat(float nutrient)
		{
			List<BodyFat> bodyFat = RelatedPart.HealthMaster.DigestiveSystem.BodyFat;

			while (nutrient > 0 && bodyFat.Count < 5) //Unable to get rid of all nutrient to fat stores;
			{
				GameObject newFat = Spawn.ServerPrefab(fatPrefab, SpawnDestination.At(GetComponent<UniversalObjectPhysics>().OfficialPosition)).GameObject;
				Inventory.ServerAdd(newFat.GetComponent<Pickupable>(), RelatedPart.OrganStorage.GetNextFreeIndexedSlot());

				if (newFat.TryGetComponent<BodyFat>(out var fat) == false) return nutrient;
				nutrient = fat.AddNutrient(nutrient);
			}

			return nutrient;
		}

		public override void InitialiseHunger(DigestiveSystemBase digestiveSystem) //On round start we give the player all 5 fat they can have.
		{
			SpawnFat(StartingFatCount);
		}
	}
}
