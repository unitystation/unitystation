using System.Collections.Generic;
using System;
using Chemistry;
using UnityEngine;
using Player.Movement;
using Items.Implants.Organs;

namespace HealthV2
{
	public class DigestiveSystemBase : MonoBehaviour, IMovementEffect
	{
		//Assuming 20 bodyparts (including organs) on average with a consumption of one.
		//That is 2 Hunger per second. We want around 0.055 per second to have a full stomach (100u) last half an hour
		//2 / 0.055 is 36.
		private const float HUNGER_DEPLETION_DIVIDER = 36f;
		private float hungerConsumptionRate = 20;

		private bool isActive = true;
		private float secondsToProcess = 10f;
		public float CurrentHunger { get; private set; } = 0;
		private float maxHunger = 100;

		private List<IStomachProcess> stomachList = new List<IStomachProcess>();
		public List<BodyFat> BodyFat { get; private set; } = new List<BodyFat>();

		private LivingHealthMasterBase livingHealthMaster;

		public HungerState HungerState
		{
			get
			{
				return GetCurrentHungerState();
			}
		}

		#region MovementFields

		[SerializeField] private float maxRunSpeedDebuff = -2;
		[SerializeField] private float maxWalkingDebuff = -1.5f;
		[SerializeField] private float maxCrawlDebuff = -0.2f;

		public float RunningSpeedModifier { get; private set; }

		public float WalkingSpeedModifier { get; private set; }

		public float CrawlingSpeedModifier { get; private set; }

		#endregion

		public void OnEnable()
		{
			if (isActive) UpdateManager.Add(ProcessStomachs, secondsToProcess);
		}

		public void OnDisable()
		{
			if (isActive) UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ProcessStomachs);
		}

		private void ProcessStomachs()
		{
			hungerConsumptionRate = 0;
			foreach (BodyPart part in livingHealthMaster.BodyPartList) //Consume food from body parts
			{
				hungerConsumptionRate += part.HungerConsumption / HUNGER_DEPLETION_DIVIDER;
			}
			CurrentHunger = Math.Max(0, CurrentHunger - hungerConsumptionRate); 


			List<IStomachProcess> stomachs = new List<IStomachProcess>(stomachList);
			foreach (IStomachProcess stomach in stomachs) //Get Stomachs to digest food
			{
				if (stomach == null)
				{
					stomachList.Remove(stomach);
					continue;
				}
				if (CurrentHunger >= maxHunger) break;
				CurrentHunger += stomach.ProcessContent();
			}
			CurrentHunger = Math.Min(CurrentHunger, maxHunger);


			List<BodyFat> fats = new List<BodyFat>(BodyFat); //We might destroy a fat object.
			foreach (BodyFat fat in fats) //If still not at full hunger, get food from fat stores
			{
				if (maxHunger <= CurrentHunger) break;

				CurrentHunger += fat.ConsumeNutrient(maxHunger - CurrentHunger);
				if (fat.CurrentNutrient <= 0)
				{
					BodyFat.Remove(fat);
					Despawn.ServerSingle(fat.gameObject);
				}
			}
		}

		public void ClampHunger(HungerState hungerState) //Used by Nutrient Pump Implants to prevent hunger from falling below certain values.
		{
			//See GetCurrentHungerState for explanantion

			float clampValue = (4 - (int)hungerState) * hungerConsumptionRate;

			CurrentHunger = Math.Max(CurrentHunger, clampValue);
		}

		public void Initialised(LivingHealthMasterBase livingHealth)
		{
			livingHealthMaster = livingHealth;
			ClampHunger(HungerState.Full);

			var playerHealthV2 = livingHealthMaster as PlayerHealthV2;
			if (playerHealthV2 != null)
			{
				playerHealthV2.PlayerMove.AddModifier(this);
			}
			foreach(IStomachProcess stomach in stomachList)
			{
				stomach.InitialiseHunger(this);
			}
		}

		public void PeriodicUpdate()
		{
			InfluenceSpeed();
		}

		public void AddStomach(IStomachProcess stomach)
		{
			stomachList.Add(stomach);
			maxHunger = CalculateMaxHunger();
		}

		public void RemoveStomach(IStomachProcess stomach)
		{
			stomachList.Remove(stomach);
			maxHunger = CalculateMaxHunger();
		}

		public List<IStomachProcess> GetStomachs()
		{
			return stomachList;
		}

		public float CalculateMaxHunger()
		{
			float capacity = 0;
			foreach(IStomachProcess stomach in stomachList)
			{
				capacity += stomach.GetStomachMaxHunger();
			}
			foreach(BodyFat fat in BodyFat)
			{
				capacity += fat.maxNutrientStore;
			}

			return capacity;
		}

		private HungerState GetCurrentHungerState()
		{
			//If the body needs no food, it makes no sense for it to report being hunger. As such our hunger state relies on our consumption.
			//Assuming 20 body parts on the average player, with 100 capacity on the human stomach.
			//This gives us hunger levels of 80 for full, 60 for normal, 40 for hungry, 20 for mal and 0 for starving.
			//But if we only have a brain, which consumes 1 food, we will need to have less than 5u of food to begin starving.

			float hungerConsump = hungerConsumptionRate == 0 ? hungerConsumptionRate + 0.1f : hungerConsumptionRate; // divide by 0 protect
			return (HungerState)(Math.Clamp(5 - Math.Ceiling(CurrentHunger / hungerConsump), 0 , 4)); 
		}

		private void InfluenceSpeed()
		{
			var playerHealthV2 = livingHealthMaster as PlayerHealthV2;
			if (playerHealthV2 == null) return;

			float DeBuffMultiplier = (int)HungerState / (int)HungerState.Starving;

			RunningSpeedModifier = maxRunSpeedDebuff * DeBuffMultiplier;
			WalkingSpeedModifier = maxWalkingDebuff * DeBuffMultiplier;
			CrawlingSpeedModifier = maxCrawlDebuff * DeBuffMultiplier;

			playerHealthV2.PlayerMove.UpdateSpeeds();		
		}
	}

	public interface IStomachProcess
	{
		public float ProcessContent();

		public float GetStomachMaxHunger();

		public bool AddObjectToStomach(Consumable edible);

		public float TryAddReagentsToStomach(ReagentMix reagentMix);

		public void InitialiseHunger(DigestiveSystemBase digestiveSystem);
	}
}
