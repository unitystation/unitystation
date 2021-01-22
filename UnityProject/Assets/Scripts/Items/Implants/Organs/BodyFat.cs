using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public class BodyFat : BodyPart, PlayerMove.IMovementEffect
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

		public float ReleaseNutrimentAtPercent = 0.05f;

		public float AbsorbNutrimentAtPercent = 0.25f;

		public float ReleaseAmount = 0.5f;

		public float MaxAmount = 500;

		public float AbsorbedAmount = 25;

		public bool IsFull => Math.Abs(MaxAmount - AbsorbedAmount) < 0.01f;

		public float DebuffCutInPoint = 100; //some fat is ok

		public bool WasApplyingDebuff = false;

		public override void ImplantPeriodicUpdate(LivingHealthMasterBase healthMaster)
		{
			base.ImplantPeriodicUpdate(healthMaster);
			float NutrimentPercentage = (BloodContainer[Nutriment] / BloodContainer.ReagentMixTotal);
			// Logger.Log("NutrimentPercentage >" + NutrimentPercentage);
			if (NutrimentPercentage < ReleaseNutrimentAtPercent)
			{
				float ToRelease = ReleaseAmount;
				if (ToRelease > AbsorbedAmount)
				{
					ToRelease = AbsorbedAmount;
				}

				BloodContainer.CurrentReagentMix.Add(Nutriment, ToRelease);
				AbsorbedAmount -= ToRelease;
				// Logger.Log("ToRelease >" + ToRelease);
			}
			else if (NutrimentPercentage > AbsorbNutrimentAtPercent && AbsorbedAmount < MaxAmount)
			{
				float ToAbsorb = BloodContainer[Nutriment];
				if (AbsorbedAmount + ToAbsorb > MaxAmount)
				{
					ToAbsorb = ToAbsorb - ((AbsorbedAmount + ToAbsorb) - MaxAmount);
				}

				float Absorbing = BloodContainer.CurrentReagentMix.Remove(Nutriment, ToAbsorb);
				AbsorbedAmount += Absorbing;
				// Logger.Log("Absorbing >" + Absorbing);
			}

			// Logger.Log("AbsorbedAmount >" + AbsorbedAmount);
			//TODOH Proby doesn't need to be updated so often
			if (DebuffCutInPoint < AbsorbedAmount)
			{
				WasApplyingDebuff = true;
				float DeBuffMultiplier = (AbsorbedAmount - DebuffCutInPoint) / (MaxAmount - DebuffCutInPoint);
				// Logger.Log("DeBuffMultiplier >" + DeBuffMultiplier);
				runSpeedDebuff = MaxRunSpeedDebuff * DeBuffMultiplier;
				WalkingDebuff = MaxWalkingDebuff * DeBuffMultiplier;
				CrawlDebuff = MaxCrawlDebuff * DeBuffMultiplier;
				var playerHealthV2 = healthMaster as PlayerHealthV2;
				if (playerHealthV2 != null)
				{
					playerHealthV2.PlayerMove.UpdateSpeeds();
				}
			}
		}

		public override void Initialisation()
		{
			base.Initialisation();
			var playerHealthV2 = healthMaster as PlayerHealthV2;
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

		public override void AddedToBody(LivingHealthMasterBase livingHealthMasterBase)
		{
			base.AddedToBody(livingHealthMasterBase);
			var playerHealthV2 = livingHealthMasterBase as PlayerHealthV2;
			if (playerHealthV2 != null)
			{
				playerHealthV2.PlayerMove.AddModifier(this);
			}
		}
	}
}