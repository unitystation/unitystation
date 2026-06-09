using System;
using UnityEngine;
using US13.HealthV2.Living;
using US13.HealthV2.Living.Metabolism;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;
using US13.HealthV2.Living.PolymorphicSystems.Hunger.HungerCalculationMethods;
using US13.Player.Movement;
using Util;

namespace US13.Items.Implants.Organs
{
	public class BodyFat : BodyPartFunctionality, IMovementEffect
	{
		[SerializeField] private float maxRunSpeedDebuff = -2;
		[SerializeField] private float maxWalkingDebuff = -1.5f;
		[SerializeField] private float maxCrawlDebuff = -0.2f;

		public float RunningSpeedModifier { get; private set; }

		public float WalkingSpeedModifier { get; private set; }

		public float CrawlingSpeedModifier { get; private set; }

		public Stomach RelatedStomach;

		public float ReleaseNutrimentPercentage = 0.01f;

		public float AbsorbNutrimentPercentage  = 0.02f;

		public float ReleaseAmount = 2f;

		public float MinuteStoreMaxAmount = 60; //Last for 60 minutes

		[NonSerialized]	public float AbsorbedAmount = 0;

		public bool IsFull => Math.Abs(MinuteStoreMaxAmount - AbsorbedAmount) < 0.01f;

		public float DDebuffInPoint = 35; //some fat is ok

		public float NoticeableDebuffInPoint = 45;

		public bool isFreshBlood;

		public HungerComponent HungerComponent;
		public ReagentCirculatedComponent ReagentCirculatedComponent;

		public void Awake()
		{
			HungerComponent = this.GetCachedComponent<HungerComponent>();
			ReagentCirculatedComponent = this.GetCachedComponent<ReagentCirculatedComponent>();
		}

		public void SetAbsorbedAmount(float newAbsorbedAmount)
		{
			AbsorbedAmount = newAbsorbedAmount;
		}

		public void ReleaseNutriment(float ToRelease)
		{
			if (ToRelease > AbsorbedAmount)
			{
				ToRelease = AbsorbedAmount;
			}
			ReagentCirculatedComponent.AssociatedSystem.BloodPool.Add(HungerComponent.Nutriment, ToRelease);
			AbsorbedAmount -= ToRelease;
		}

		public override void ImplantPeriodicUpdate()
		{

			isFreshBlood = true;
			base.ImplantPeriodicUpdate();
			// Loggy.Log("Absorbing >" + Absorbing);
			float nutrimentPercentage = (ReagentCirculatedComponent.AssociatedSystem.BloodPool[HungerComponent.Nutriment] / ReagentCirculatedComponent.AssociatedSystem.BloodPool.Total);
			//Loggy.Log("NutrimentPercentage >" + NutrimentPercentage);
			if (nutrimentPercentage < ReleaseNutrimentPercentage)
			{
				ReleaseNutriment(ReleaseAmount);
				isFreshBlood = false;
				// Loggy.Log("ToRelease >" + ToRelease);
			}
			else if (isFreshBlood && nutrimentPercentage > AbsorbNutrimentPercentage && AbsorbedAmount < MinuteStoreMaxAmount)
			{
				var hungerSystem = LivingHealthMaster.GetHungerSystem();
				if (hungerSystem.HungerCalculationMethod is NutrimentInBlood c)
				{
					if (c.NutrimentThresholdForNormal >= NoticeableDebuffInPoint)
					{
						//TODO: (Max) this is temporary until I find a better way to handle this.
						float absorbing = ReagentCirculatedComponent.AssociatedSystem.BloodPool.Remove(HungerComponent.Nutriment, 1f);
						AbsorbedAmount += absorbing;
					}
				}
				else
				{
					float ToAbsorb = ReagentCirculatedComponent.AssociatedSystem.BloodPool[HungerComponent.Nutriment];
					if (AbsorbedAmount + ToAbsorb > MinuteStoreMaxAmount)
					{
						ToAbsorb = ToAbsorb - ((AbsorbedAmount + ToAbsorb) - MinuteStoreMaxAmount);
					}

					float Absorbing = ReagentCirculatedComponent.AssociatedSystem.BloodPool.Remove(HungerComponent.Nutriment, ToAbsorb);
					AbsorbedAmount += Absorbing;
				}
				// Loggy.Log("Absorbing >" + Absorbing);
			}

			//Loggy.Log("AbsorbedAmount >" + AbsorbedAmount);
			//TODOH Proby doesn't need to be updated so often
			if (DDebuffInPoint < AbsorbedAmount)
			{
				float DeBuffMultiplier = (AbsorbedAmount - DDebuffInPoint) / (MinuteStoreMaxAmount - DDebuffInPoint);
				// Loggy.Log("DeBuffMultiplier >" + DeBuffMultiplier);
				RunningSpeedModifier = maxRunSpeedDebuff * DeBuffMultiplier;
				WalkingSpeedModifier = maxWalkingDebuff * DeBuffMultiplier;
				CrawlingSpeedModifier = maxCrawlDebuff * DeBuffMultiplier;
				var playerHealthV2 = RelatedPart.HealthMaster as PlayerHealthV2;
				if (playerHealthV2 != null)
				{
					playerHealthV2.PlayerMove.UpdateSpeeds();
				}
			}
			else
			{
				if (RunningSpeedModifier != 0)
				{
					RunningSpeedModifier = 0;
					WalkingSpeedModifier = 0;
					CrawlingSpeedModifier = 0;
					var playerHealthV2 = RelatedPart.HealthMaster as PlayerHealthV2;
					if (playerHealthV2 != null)
					{
						playerHealthV2.PlayerMove.UpdateSpeeds();
					}
				}
			}

			if (AbsorbedAmount == 0)
			{
				HungerComponent.HungerState = HungerState.Malnourished;
			}
			else if (AbsorbedAmount < 5) //Five minutes of food
			{

				HungerComponent.HungerState = HungerState.Hungry;
			}
			else  if (NoticeableDebuffInPoint < AbsorbedAmount)
			{
				HungerComponent.HungerState = HungerState.Full;
			}
			else
			{
				HungerComponent.HungerState = HungerState.Normal;
			}
		}

		[NaughtyAttributes.Button()]
		public void BecomeSkinny()
		{
			AbsorbedAmount = 0;
		}

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			base.OnAddedToBody(livingHealth);
			var playerHealthV2 = RelatedPart.HealthMaster as PlayerHealthV2;
			if (playerHealthV2 != null)
			{
				playerHealthV2.PlayerMove.AddModifier(this);
			}
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			base.OnRemovedFromBody(livingHealth);
			if (RelatedStomach != null)
			{
				RelatedStomach.BodyFats.Remove(this);
			}

			var playerHealthV2 = livingHealth as PlayerHealthV2;
			if (playerHealthV2 != null)
			{
				playerHealthV2.PlayerMove.RemoveModifier(this);
			}
		}

		public override void SetUpSystems()
		{
			base.SetUpSystems();
			var playerHealthV2 = RelatedPart.HealthMaster as PlayerHealthV2;
			if (playerHealthV2 != null)
			{
				playerHealthV2.PlayerMove.AddModifier(this);
			}
		}
	}
}