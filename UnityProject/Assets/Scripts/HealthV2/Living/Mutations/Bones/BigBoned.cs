using UnityEngine;

namespace HealthV2.Living.Mutations.Bones
{
	[CreateAssetMenu(fileName = "BigBoned", menuName = "ScriptableObjects/Mutations/BigBoned")]

	public class BigBoned : MutationSO
	{
		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InBigBoned(BodyPart,_RelatedMutationSO);
		}

		private class InBigBoned: Mutation
		{

			public Items.Implants.Organs.Bones Bone;

			public InBigBoned(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				Bone = BodyPart.GetComponent<Items.Implants.Organs.Bones>();
				Bone.BloodGeneratedByOneNutriment += 30;
			}

			public override void Remove()
			{
				Bone.BloodGeneratedByOneNutriment -= 30;
			}

		}
	}
}
