using System.Collections;
using System.Collections.Generic;
using HealthV2.Living.PolymorphicSystems;
using UnityEngine;

public class ChemicalMetabolismSystem : HealthSystemBase
{

	public Dictionary<MetabolismReaction, List<MetabolismComponent>> PrecalculatedMetabolismReactions = new  Dictionary<MetabolismReaction, List<MetabolismComponent>>();
	public List<MetabolismReaction> MetabolismReactions { get; } = new();

	private ReagentPoolSystem _reagentPoolSystem;

	public override void InIt()
	{
		_reagentPoolSystem = Base.reagentPoolSystem; //idk Shouldn't change
	}

	public void MetaboliseReactions()
	{
		MetabolismReactions.Clear();

		foreach (var Reaction in PrecalculatedMetabolismReactions)
		{
			Reaction.Key.Apply(this, _reagentPoolSystem.BloodPool);
		}

		foreach (var Reaction in MetabolismReactions)
		{
			float ProcessingAmount = 0;
			foreach (var bodyPart in PrecalculatedMetabolismReactions[Reaction]) //TODO maybe lag? Alternative?
			{
				ProcessingAmount += bodyPart.ReagentMetabolism * bodyPart.reagentCirculatedComponent.Throughput * bodyPart.GetCurrentBloodSaturation * Mathf.Max(0.10f, bodyPart.RelatedPart.TotalModified);
			}

			if (ProcessingAmount == 0) continue;

			Reaction.React(PrecalculatedMetabolismReactions[Reaction], _reagentPoolSystem.BloodPool, ProcessingAmount);
		}
	}

	public override HealthSystemBase CloneThisSystem()
	{
		return new ChemicalMetabolismSystem(); //TODO
	}
}
