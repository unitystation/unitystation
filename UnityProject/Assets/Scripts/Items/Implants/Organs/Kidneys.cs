using System.Collections.Generic;
using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;

namespace Items.Implants.Organs
{
	public class Kidneys : BodyPartFunctionality
	{
		public List<Reagent> BlacklistReagents = new List<Reagent>();
		//add Special nutrients in body


		public ReagentCirculatedComponent _ReagentCirculatedComponent;


		public float ProcessingPercentage = 0.05f;

		public override void Awake()
		{
			base.Awake();
			_ReagentCirculatedComponent = this.GetComponent<ReagentCirculatedComponent>();
		}


		public override void ImplantPeriodicUpdate()
		{
			base.ImplantPeriodicUpdate();

			var poolToClean = _ReagentCirculatedComponent.AssociatedSystem.BloodPool.Take(
				ProcessingPercentage*_ReagentCirculatedComponent.AssociatedSystem.BloodPool.Total);

			foreach (var Reagent in BlacklistReagents)
			{
				poolToClean.Remove(Reagent, 1000);
			}

			_ReagentCirculatedComponent.AssociatedSystem.BloodPool.Add(poolToClean);
			//Debug.Log("Kidney: " + BloodContainer[requiredReagent]/bloodType.GetGasCapacity(BloodContainer.CurrentReagentMix));
		}
	}
}
