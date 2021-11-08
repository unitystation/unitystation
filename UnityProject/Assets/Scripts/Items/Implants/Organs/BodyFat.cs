﻿using System;
using System.Collections;
using System.Collections.Generic;
using Player.Movement;
using UnityEngine;

namespace HealthV2
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

		public float MinuteStoreMaxAmount =  60; //Last for 60 minutes

		[NonSerialized] public const float StartAbsorbedAmount = 30;

		[NonSerialized]	public float AbsorbedAmount = 0;

		public bool IsFull => Math.Abs(MinuteStoreMaxAmount - AbsorbedAmount) < 0.01f;

		public float DDebuffInPoint = 35; //some fat is ok

		public bool WasApplyingDebuff = false;

		public bool isFreshBlood;

		public void Awake()
		{
			AbsorbedAmount = StartAbsorbedAmount;
		}

		public void SetAbsorbedAmount(float newAbsorbedAmount)
		{
			AbsorbedAmount = newAbsorbedAmount;
		}

		public override void ImplantPeriodicUpdate()
		{
			base.ImplantPeriodicUpdate();
			// Logger.Log("Absorbing >" + Absorbing);
			float NutrimentPercentage = (RelatedPart.BloodContainer[RelatedPart.Nutriment] / RelatedPart.BloodContainer.ReagentMixTotal);
			//Logger.Log("NutrimentPercentage >" + NutrimentPercentage);
			if (NutrimentPercentage < ReleaseNutrimentPercentage)
			{
				float ToRelease = ReleaseAmount;
				if (ToRelease > AbsorbedAmount)
				{
					ToRelease = AbsorbedAmount;
				}

				RelatedPart.BloodContainer.CurrentReagentMix.Add(RelatedPart.Nutriment, ToRelease);
				AbsorbedAmount -= ToRelease;
				isFreshBlood = false;
				// Logger.Log("ToRelease >" + ToRelease);
			}
			else if (isFreshBlood && NutrimentPercentage > AbsorbNutrimentPercentage && AbsorbedAmount < MinuteStoreMaxAmount)
			{
				float ToAbsorb = RelatedPart.BloodContainer[RelatedPart.Nutriment];
				if (AbsorbedAmount + ToAbsorb > MinuteStoreMaxAmount)
				{
					ToAbsorb = ToAbsorb - ((AbsorbedAmount + ToAbsorb) - MinuteStoreMaxAmount);
				}

				float Absorbing = RelatedPart.BloodContainer.CurrentReagentMix.Remove(RelatedPart.Nutriment, ToAbsorb);
				AbsorbedAmount += Absorbing;
				// Logger.Log("Absorbing >" + Absorbing);
			}

			//Logger.Log("AbsorbedAmount >" + AbsorbedAmount);
			//TODOH Proby doesn't need to be updated so often
			if (DDebuffInPoint < AbsorbedAmount)
			{
				WasApplyingDebuff = true;
				float DeBuffMultiplier = (AbsorbedAmount - DDebuffInPoint) / (MinuteStoreMaxAmount - DDebuffInPoint);
				// Logger.Log("DeBuffMultiplier >" + DeBuffMultiplier);
				RunningSpeedModifier = maxRunSpeedDebuff * DeBuffMultiplier;
				WalkingSpeedModifier = maxWalkingDebuff * DeBuffMultiplier;
				CrawlingSpeedModifier = maxCrawlDebuff * DeBuffMultiplier;
				var playerHealthV2 = RelatedPart.HealthMaster as PlayerHealthV2;
				if (playerHealthV2 != null)
				{
					playerHealthV2.PlayerMove.UpdateSpeeds();
				}
			}

			if (AbsorbedAmount == 0)
			{
				RelatedPart.HungerState = HungerState.Malnourished;
			}
			else if (AbsorbedAmount < 5) //Five minutes of food
			{
				RelatedPart.HungerState = HungerState.Hungry;
			}
			else
			{
				RelatedPart.HungerState = HungerState.Normal;
			}
		}

		public override void BloodWasPumped()
		{
			isFreshBlood = true;
		}

		[NaughtyAttributes.Button()]
		public void BecomeSkinny()
		{
			AbsorbedAmount = 0;
		}

		public override void HealthMasterSet(LivingHealthMasterBase livingHealth)
		{
			base.HealthMasterSet(livingHealth);
			var playerHealthV2 = RelatedPart.HealthMaster as PlayerHealthV2;
			if (playerHealthV2 != null)
			{
				playerHealthV2.PlayerMove.AddModifier(this);
			}
		}

		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			base.RemovedFromBody(livingHealth);
			RelatedStomach.BodyFats.Remove(this);
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