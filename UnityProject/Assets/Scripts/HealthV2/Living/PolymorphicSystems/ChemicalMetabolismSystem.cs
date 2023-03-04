using System.Collections;
using System.Collections.Generic;
using HealthV2;
using HealthV2.Living.CirculatorySystem;
using HealthV2.Living.PolymorphicSystems;
using UnityEngine;

public class ChemicalMetabolismSystem : HealthSystemBase, IAreaReactionBase
{
	public Dictionary<MetabolismReaction, List<MetabolismComponent>> PrecalculatedMetabolismReactions =
		new Dictionary<MetabolismReaction, List<MetabolismComponent>>();

	public List<MetabolismReaction> MetabolismReactions { get; } = new();

	public List<MetabolismReaction> ALLMetabolismReactions = new List<MetabolismReaction>(); //TOOD Move somewhere static maybe

	public List<MetabolismComponent> MetabolismComponents = new List<MetabolismComponent>();

	private ReagentPoolSystem _reagentPoolSystem;

	public override void InIt()
	{
		_reagentPoolSystem = Base.reagentPoolSystem; //idk Shouldn't change
	}


	public override void BodyPartAdded(BodyPart bodyPart)
	{
		var component = bodyPart.GetComponent<MetabolismComponent>();
		if (component != null)
		{
			MetabolismComponents.Add(component);
			BodyPartListChange();
		}
	}

	public override void BodyPartRemoved(BodyPart bodyPart)
	{
		var component = bodyPart.GetComponent<MetabolismComponent>();
		if (component != null)
		{
			if (MetabolismComponents.Contains(component))
			{
				MetabolismComponents.Remove(component);
			}

			BodyPartListChange();
		}
	}

	public void BodyPartListChange()
	{
		PrecalculatedMetabolismReactions.Clear();

		foreach (var MR in ALLMetabolismReactions)
		{
			foreach (var bodyPart in MetabolismComponents)
			{

				if (bodyPart.RelatedPart.ItemAttributes.HasAllTraits(MR.InternalAllRequired) &&
				    bodyPart.RelatedPart.ItemAttributes.HasAnyTrait(MR.InternalBlacklist) == false)
				{
					if (PrecalculatedMetabolismReactions.ContainsKey(MR) == false)
					{
						PrecalculatedMetabolismReactions[MR] = new List<MetabolismComponent>();
					}

					PrecalculatedMetabolismReactions[MR].Add(bodyPart);
				}
			}
		}
	}

	public override void SystemUpdate()
	{
		MetaboliseReactions();
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
				ProcessingAmount += bodyPart.ReagentMetabolism * bodyPart.reagentCirculatedComponent.Throughput *
				                    bodyPart.GetCurrentBloodSaturation *
				                    Mathf.Max(0.10f, bodyPart.RelatedPart.TotalModified);
			}

			if (ProcessingAmount == 0) continue;

			//Reaction.React(PrecalculatedMetabolismReactions[Reaction], _reagentPoolSystem.BloodPool, ProcessingAmount);
		}
	}

	public override HealthSystemBase CloneThisSystem()
	{
		return new ChemicalMetabolismSystem()
		{
			ALLMetabolismReactions = ALLMetabolismReactions
		};
	}
}