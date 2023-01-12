using HealthV2;
using System;
using UnityEngine;

namespace Items.Implants.Organs
{
	public class BodyFat : BodyPartFunctionality
	{
		public float CurrentNutrient { get; private set; } = 0;
		[field: SerializeField] public int maxNutrientStore { get; private set; } = 20;
		[SerializeField] private int maxNutrientDischarge = 5; //The max amount of nutrient one piece of fat can donate each update

		public float AddNutrient(float amount)
		{
			float newAmount = Math.Min(amount, maxNutrientStore - CurrentNutrient);
			CurrentNutrient += newAmount;

			return amount - newAmount; //left over
		}

		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			if (livingHealth.DigestiveSystem == null) return;
			if(livingHealth.DigestiveSystem.BodyFat.Contains(this)) livingHealth.DigestiveSystem.BodyFat.Remove(this);
			livingHealth.DigestiveSystem.CalculateMaxHunger();
		}

		public override void AddedToBody(LivingHealthMasterBase livingHealth)
		{
			if (livingHealth.DigestiveSystem == null) return;
			if (livingHealth.DigestiveSystem.BodyFat.Contains(this) == false) livingHealth.DigestiveSystem.BodyFat.Add(this);
			livingHealth.DigestiveSystem.CalculateMaxHunger();
		}

		public float ConsumeNutrient(float amount)
		{
			float consumedAmount = Math.Min(Math.Min(amount, maxNutrientDischarge), maxNutrientDischarge);
			CurrentNutrient -= consumedAmount;

			return consumedAmount;
		}
		
	}
}