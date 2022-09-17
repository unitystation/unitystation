using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

public class BodyPartMutations : BodyPartFunctionality
{
	public static Dictionary<MutationSO, NumberAndRoundID> MutationVariants = new Dictionary<MutationSO, NumberAndRoundID>();

	public List<MutationSO> CapableMutations = new List<MutationSO>();
	public List<Mutation> ActiveMutations = new List<Mutation>();

	public int Stability = 0;


	[NaughtyAttributes.Button()]
	public void AddFirstMutation()
	{
		var  Mutation = CapableMutations[0];
		AddMutation(Mutation);
	}

	public void AddMutation(MutationSO Mutation)
	{
		if (MutationVariants.ContainsKey(Mutation) == false)
		{
			MutationVariants[Mutation] = new NumberAndRoundID()
			{
				RoundID = GameManager.Instance.RoundID,
				Stability = Mathf.RoundToInt((Mutation.Stability * Random.Range(0.5f, 1.5f)))

			};
		}
		else
		{
			if (MutationVariants[Mutation].RoundID != GameManager.Instance.RoundID)
			{
				MutationVariants[Mutation].Stability = Mathf.RoundToInt((Mutation.Stability * Random.Range(0.5f, 1.5f)));
				MutationVariants[Mutation].RoundID = GameManager.Instance.RoundID;
			}
		}


		var ActiveMutation = Mutation.GetMutation(bodyPart,Mutation);
		ActiveMutation.Stability = MutationVariants[Mutation].Stability;


		ActiveMutations.Add(ActiveMutation);
		ActiveMutation.SetUp();
		CalculateStability();

		bodyPart.HealthMaster.OrNull()?.BodyPartsChangeMutation();
	}

	public void RemoveMutation()
	{
		Mutation Mutation = ActiveMutations[0];
		ActiveMutations.Remove(Mutation);
		Mutation.Remove();
		CalculateStability();
		bodyPart.HealthMaster.OrNull()?.BodyPartsChangeMutation();
	}

	public List<MutationAndBodyPart> GetAvailableNegativeMutations(List<MutationAndBodyPart> AvailableMutations)
	{
		foreach (var Mutation in CapableMutations)
		{
			if (Mutation.Stability > 0)
			{
				bool AlreadyActive = false;
				foreach (var ActiveMutation in ActiveMutations)
				{
					if (ActiveMutation.RelatedMutationSO == Mutation)
					{
						AlreadyActive = true;
						break;
					}
				}

				if (AlreadyActive == false)
				{
					AvailableMutations.Add(new MutationAndBodyPart(){BodyPartMutations = this, MutationSO = Mutation});
				}
			}
		}

		return AvailableMutations;
	}

	public struct MutationAndBodyPart
	{
		public MutationSO MutationSO;
		public BodyPartMutations BodyPartMutations;

	}



	public void CalculateStability()
	{
		int InStability = 0;
		foreach (var ActiveMutation in ActiveMutations)
		{
			InStability += ActiveMutation.Stability;
		}

		Stability = InStability;
	}


	public void ChangeToSpecies(PlayerHealthData PlayerHealthData, GameObject BodyPart)
	{

		//To do wait some time
		//Work out how On  earth to do messages? It would be assumed that one body part at a time would be changing/ ok, just add text for that then don't have to worry about stacking
		//wait indeterminate amount of time again
		//spawn in body part
		//Transfer sub body parts of this into New body part
		//Remove this
		//transfer new body part into body
	}

	//ok Let's add in species change stuff here to


	public class NumberAndRoundID
	{
		public int Stability;
		public int RoundID;
	}
	/*
	so, body wide system

		Plus and minus points from positive negative,

	If it is negative gives you random negative trays until it's positive ,


	Gameplay loop will be that with multi- slider balance thing,


	Body part functionality???

	Let's say, let's say there was a stomach  script,

	How would you affect the size of stomach from

		Body part functionality??

	Without making a custom one for it

		Body part functionality, -> list possible mutations idk what type?
	-> Instantiates new, give some variables is SO,  does this stuff of it needs to be permanent
*/
}
