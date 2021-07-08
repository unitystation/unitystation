using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public class BodyFat : BodyPartModification, PlayerMove.IMovementEffect
	{
		public float MaxRunSpeedDebuff = -2;
		public float MaxWalkingDebuff = -1.5f;
		public float MaxCrawlDebuff = -0.2f;

		private float runSpeedDebuff;
		private float WalkingDebuff;
		private float CrawlDebuff;

		public float RunningAdd
		{
			get => runSpeedDebuff;
			set { }
		}

		public float WalkingAdd
		{
			get => WalkingDebuff;
			set { }
		}


		public float CrawlAdd
		{
			get => CrawlDebuff;
			set { }
		}


		public Stomach RelatedStomach;

		public float ReleaseNutrimentAtPercent = 0.20f;

		public float AbsorbNutrimentAtPercent = 0.35f;

		public float ReleaseAmount = 1f;

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
			if (NutrimentPercentage < ReleaseNutrimentAtPercent)
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
			else if (isFreshBlood && NutrimentPercentage > AbsorbNutrimentAtPercent && AbsorbedAmount < MaxAmount)
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
				runSpeedDebuff = MaxRunSpeedDebuff * DeBuffMultiplier;
				WalkingDebuff = MaxWalkingDebuff * DeBuffMultiplier;
				CrawlDebuff = MaxCrawlDebuff * DeBuffMultiplier;
				var playerHealthV2 = RelatedPart.HealthMaster as PlayerHealthV2;
				if (playerHealthV2 != null)
				{
					playerHealthV2.PlayerMove.UpdateSpeeds();
				}
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

		public override void RemovedFromBody(LivingHealthMasterBase livingHealthMasterBase)
		{
			base.RemovedFromBody(livingHealthMasterBase);
			var playerHealthV2 = livingHealthMasterBase as PlayerHealthV2;
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