using System.Collections.Generic;
using Chemistry;
using HealthV2;

namespace Items.Implants.Organs
{
	public class Kidneys : BodyPartFunctionality
	{
		public List<Reagent> BlacklistReagents = new List<Reagent>();
		//add Special nutrients in body



		public float ProcessingPercentage = 0.05f;

		public override void ImplantPeriodicUpdate()
		{
			base.ImplantPeriodicUpdate();

			var poolToClean = RelatedPart.HealthMaster.CirculatorySystem.BloodPool.Take(
				ProcessingPercentage*RelatedPart.HealthMaster.CirculatorySystem.BloodPool.Total);

			foreach (var Reagent in BlacklistReagents)
			{
				poolToClean.Remove(Reagent, 1000);
			}

			RelatedPart.HealthMaster.CirculatorySystem.BloodPool.Add(poolToClean);
			//Debug.Log("Kidney: " + BloodContainer[requiredReagent]/bloodType.GetGasCapacity(BloodContainer.CurrentReagentMix));
		}
	}
}
