using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Items.Implants.Organs;
using UnityEngine;

[CreateAssetMenu(fileName = "Anemia", menuName = "ScriptableObjects/Mutations/Anemia")]
public class Anemia : MutationSO
{
	public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
	{
		return new InAnemia(BodyPart,_RelatedMutationSO);
	}

	private class InAnemia: Mutation
	{

		public Bones Bone;

		public InAnemia(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
		{

		}

		public override void SetUp()
		{
			Bone = BodyPart.GetComponent<Bones>();
			Bone.BloodGeneratedByOneNutriment -= 28;
		}

		public override void Remove()
		{
			Bone.BloodGeneratedByOneNutriment += 28;
		}

	}
}
