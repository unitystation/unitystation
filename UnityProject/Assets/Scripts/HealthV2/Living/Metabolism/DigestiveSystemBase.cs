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
		private const float NORMAL_THRESHOLD = 85f;
		private const float HUNGRY_THRESHOLD = 50f;
		private const float MALNURISHED_THRESHOLD = 25f;
		private const float STARVING_THRESHOLD = 5f;

		//Assuming 20 bodyparts (including organs) on average with a consumption of one.
		//That is 2 Hunger per second. We want around 0.055 per second to have a full stoamch last half an hour
		//2 / 0.055 is 36.
		private const float HUNGER_DEPLETION_DIVIDER = 36f; 

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

		public void Start()
		{
			if (isActive) UpdateManager.Add(ProcessStomachs, secondsToProcess);
		}

		private void ProcessStomachs()
		{
			foreach (BodyPart part in livingHealthMaster.BodyPartList) //Consume food from body parts
			{
				CurrentHunger -= part.HungerConsumption / HUNGER_DEPLETION_DIVIDER;
			}
			CurrentHunger = Math.Max(CurrentHunger, 0); 


			foreach (IStomachProcess stomach in stomachList) //Get Stomachs to digest food
			{
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
			float clampValue;

			switch(hungerState)
			{
				case HungerState.Normal:
					clampValue = NORMAL_THRESHOLD;
					break;
				case HungerState.Hungry:
					clampValue = HUNGRY_THRESHOLD;
					break;
				case HungerState.Starving:
					clampValue = STARVING_THRESHOLD;
					break;
				case HungerState.Malnourished:
					clampValue = MALNURISHED_THRESHOLD;
					break;
				default:
					clampValue = MALNURISHED_THRESHOLD;
					break;
			}

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

		private float CalculateMaxHunger()
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
			float percentFull = (CurrentHunger / maxHunger) * 100;

			switch(percentFull)
			{
				case > NORMAL_THRESHOLD:
					return HungerState.Full;
				case > HUNGRY_THRESHOLD:
					return HungerState.Normal;
				case > MALNURISHED_THRESHOLD:
					return HungerState.Hungry;
				case > STARVING_THRESHOLD:
					return HungerState.Malnourished;
				default:
					return HungerState.Starving;
			}
		}

		private void InfluenceSpeed()
		{
			float DeBuffMultiplier = (int)HungerState / (int)HungerState.Starving;

			RunningSpeedModifier = maxRunSpeedDebuff * DeBuffMultiplier;
			WalkingSpeedModifier = maxWalkingDebuff * DeBuffMultiplier;
			CrawlingSpeedModifier = maxCrawlDebuff * DeBuffMultiplier;
			var playerHealthV2 = livingHealthMaster as PlayerHealthV2;
			if (playerHealthV2 != null)
			{
				playerHealthV2.PlayerMove.UpdateSpeeds();
			}
		}
	}

	public interface IStomachProcess
	{
		public float ProcessContent();

		public float GetStomachMaxHunger();

		public bool AddObjectToStomach(Consumable edible);

		public float TryAddReagentsToStomach(ReagentMix reagentMix);
	}
}
