using System.Collections;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;

namespace HealthV2
{
	public class Kidneys : BodyPartFunctionality
	{
		public List<Reagent> WhiteListReagents = new List<Reagent>();
		//add Special nutrients in body

		public Dictionary<Reagent, float> ContainedBADReagents = new Dictionary<Reagent, float>();

		public float ProcessingPercentage = 0.2f;

		public override void SetUpSystems()
		{
			base.SetUpSystems();
			if(WhiteListReagents.Count == 0)
			{
				WhiteListReagents.Add(RelatedPart.requiredReagent);
				WhiteListReagents.Add(RelatedPart.wasteReagent);
				WhiteListReagents.Add(RelatedPart.Nutriment);
			}
		}

		public override void ImplantPeriodicUpdate()
		{
			base.ImplantPeriodicUpdate();
			ContainedBADReagents.Clear();

			foreach (var Reagent in RelatedPart.HealthMaster.CirculatorySystem.BloodPool.reagents.m_dict)
			{
				if (WhiteListReagents.Contains(Reagent.Key) == false && Reagent.Key is BloodType == false)
				{
					ContainedBADReagents.Add(Reagent.Key, Reagent.Value * ProcessingPercentage * RelatedPart.TotalModified);

				}
			}

			foreach (var Reagents in ContainedBADReagents)
			{
				RelatedPart.HealthMaster.CirculatorySystem.BloodPool.Remove(Reagents.Key, Reagents.Value);
			}
			//Debug.Log("Kidney: " + BloodContainer[requiredReagent]/bloodType.GetGasCapacity(BloodContainer.CurrentReagentMix));
		}
	}
}
