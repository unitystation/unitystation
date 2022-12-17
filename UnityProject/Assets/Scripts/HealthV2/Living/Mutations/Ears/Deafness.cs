using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Items.Implants.Organs;
using UnityEngine;

[CreateAssetMenu(fileName = "Deafness", menuName = "ScriptableObjects/Mutations/Deafness")]
public class Deafness  : MutationSO
{
	public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
	{
		return new InDeafnessMutation(BodyPart,_RelatedMutationSO);
	}

	private class InDeafnessMutation: Mutation
	{

		public Ears RelatedEars;

		public InDeafnessMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
		{

		}

		public override void SetUp()
		{
			RelatedEars = BodyPart.GetComponent<Ears>();
			RelatedEars.MutationMultiplier = 0;
		}

		public override void Remove()
		{
			RelatedEars.MutationMultiplier = 1;
		}

	}
}
