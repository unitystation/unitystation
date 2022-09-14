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
	public void AddMutation()
	{
		var  Mutation = CapableMutations[0];

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


		var ActiveMutation = Mutation.GetMutation(bodyPart);
		ActiveMutation.Stability = MutationVariants[Mutation].Stability;


		ActiveMutations.Add(ActiveMutation);
		ActiveMutation.SetUp();
	}

	public void RemoveMutation()
	{
		Mutation Mutation = ActiveMutations[0];
		ActiveMutations.Remove(Mutation);
		Mutation.Remove();
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
