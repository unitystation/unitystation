using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Items.Implants.Organs;
using UnityEngine;


[CreateAssetMenu(fileName = "BigBoned", menuName = "ScriptableObjects/Mutations/BigBoned")]

public class BigBoned : MutationSO
{
	public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
	{
		return new InBigBoned(BodyPart,_RelatedMutationSO);
	}

	private class InBigBoned: Mutation
	{

		public Bones Bone;

		public InBigBoned(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
		{

		}

		public override void SetUp()
		{
			Bone = BodyPart.GetComponent<Bones>();
			Bone.BloodGeneratedByOneNutriment += 30;
		}

		public override void Remove()
		{
			Bone.BloodGeneratedByOneNutriment -= 30;
		}

	}
}
