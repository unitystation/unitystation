using System;
using System.Collections;
using System.Collections.Generic;
using Player.Movement;
using UnityEngine;

namespace HealthV2
{
	public class BodyFat : Organ, IMovementEffect
	{
		[SerializeField] private float maxRunSpeedDebuff = -2;
		[SerializeField] private float maxWalkingDebuff = -1.5f;
		[SerializeField] private float maxCrawlDebuff = -0.2f;

		public float RunningSpeedModifier { get; private set; }

		public float WalkingSpeedModifier { get; private set; }

		public float CrawlingSpeedModifier { get; private set; }


		public Stomach RelatedStomach;

		public float ReleaseNutrimentAtPer1UBloodFlow = 0.005f;

		public float AbsorbNutrimentAtPer1UBloodFlow  = 0.01f;

		public float ReleaseAmount = 5f;

		public float MaxAmount = 500;

		public float AbsorbedAmount = 100;

		public bool IsFull => Math.Abs(MaxAmount - AbsorbedAmount) < 0.01f;

		public float DebuffCutInPoint = 100; //some fat is ok

		public bool WasApplyingDebuff = false;

		public bool isFreshBlood;

		public override void ImplantPeriodicUpdate()
		{
			base.ImplantPeriodicUpdate();
			// Logger.Log("Absorbing >" + Absorbing);
			float NutrimentPercentage = (RelatedPart.BloodContainer[RelatedPart.Nutriment] / RelatedPart.BloodContainer.ReagentMixTotal);
			//Logger.Log("NutrimentPercentage >" + NutrimentPercentage);
			if (NutrimentPercentage < ReleaseNutrimentAtPer1UBloodFlow * RelatedPart.BloodThroughput)
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
			else if (isFreshBlood && NutrimentPercentage > AbsorbNutrimentAtPer1UBloodFlow *  RelatedPart.BloodThroughput && AbsorbedAmount < MaxAmount)
			{
				float ToAbsorb = RelatedPart.BloodContainer[RelatedPart.Nutriment];
				if (AbsorbedAmount + ToAbsorb > MaxAmount)
				{
					ToAbsorb = ToAbsorb - ((AbsorbedAmount + ToAbsorb) - MaxAmount);
				}

				float Absorbing = RelatedPart.BloodContainer.CurrentReagentMix.Remove(RelatedPart.Nutriment, ToAbsorb);
				AbsorbedAmount += Absorbing;
				// Logger.Log("Absorbing >" + Absorbing);
			}

			//Logger.Log("AbsorbedAmount >" + AbsorbedAmount);
			//TODOH Proby doesn't need to be updated so often
			if (DebuffCutInPoint < AbsorbedAmount)
			{
				WasApplyingDebuff = true;
				float DeBuffMultiplier = (AbsorbedAmount - DebuffCutInPoint) / (MaxAmount - DebuffCutInPoint);
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

		public override void Initialisation()
		{
			base.Initialisation();
			var playerHealthV2 = RelatedPart.HealthMaster as PlayerHealthV2;
			if (playerHealthV2 != null)
			{
				playerHealthV2.PlayerMove.AddModifier(this);
			}
		}

		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			base.RemovedFromBody(livingHealth);
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