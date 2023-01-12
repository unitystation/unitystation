using Chemistry.Components;
using System;
using Chemistry;
using HealthV2;
using Items.Food;
using UnityEngine;
using System.Collections.Generic;

namespace Items.Implants.Organs
{
	public class Stomach : BodyPartFunctionality, IStomachProcess
	{
		//General Stomach Class does not contain functions for fat as only organic stomachs will produce it. See FatProducingStomach.cs

		protected ItemStorage stomachContents;
		protected ReagentContainer reagentContainer;

		[SerializeField] protected List<Reagent> nutrientReagents; //What reagents this stomach treats as food. Nutrient is base, but stomachs like Moths might consume different reagents for food.

		[SerializeField] protected int DigesterAmountPerSecond = 1; //On average takes 18 seconds to deplete one nutrient.

		public void Start()
		{
			stomachContents = GetComponent<ItemStorage>();
			reagentContainer = GetComponent<ReagentContainer>();
		}

		public bool AddObjectToStomach(Consumable edible)
		{
			return Inventory.ServerAdd(edible.gameObject, stomachContents.GetNextFreeIndexedSlot(), ReplacementStrategy.Cancel);
		}

		public float TryAddReagentsToStomach(ReagentMix reagentMix)
		{
			float consumedAmount = Math.Max(reagentMix.Total, reagentContainer.SpareCapacity);
			
			reagentContainer.Add(reagentMix.Take(consumedAmount));

			return consumedAmount;
		}

		public virtual float ProcessContent() 
		{
			foreach(ItemSlot slot in stomachContents.GetItemSlots()) //Gets reagent from food in stomach, and then despawns food once digested.
			{
				if (slot.IsEmpty == true) continue;

				ReagentMix chemicalsFromFood = new ReagentMix();
				if (slot.ItemObject.TryGetComponent<Edible>(out var edible) == false)
				{
					if (slot.ItemObject.TryGetComponent<NotHandEdible>(out var notHandEdible) == false) continue;
					chemicalsFromFood.Add(notHandEdible.TakeReagentsFromFood(DigesterAmountPerSecond));
				}
				else chemicalsFromFood.Add(edible.TakeReagentsFromFood(DigesterAmountPerSecond));

				if(chemicalsFromFood.Total == 0)
				{
					Inventory.ServerDespawn(slot);
					continue;
				}

				reagentContainer.Add(chemicalsFromFood);
				break;
			}

			return Digest(); //Gets nutrient from reagent store, and adds the remaining nutrients to Blood Supply.
		}

		public virtual float Digest()
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

			return newNutrient;
		}

		public virtual float GetStomachMaxHunger()
		{
			return reagentContainer.MaxCapacity;
		}

		public override void AddedToBody(LivingHealthMasterBase livingHealth)
		{
			RelatedPart.HealthMaster.DigestiveSystem.AddStomach(this);
		}

		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			base.RemovedFromBody(livingHealth);
			RelatedPart.HealthMaster.DigestiveSystem.RemoveStomach(this);
		}

		public void ChangeStomachCapacity(int reagentMaxCapacity)
		{
			reagentContainer.SetMaxCapacity(reagentMaxCapacity);
		}
	}
}