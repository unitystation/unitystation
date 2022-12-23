using UnityEngine;

namespace HealthV2.Living.Mutations.Bones
{
	[CreateAssetMenu(fileName = "Polycythemia", menuName = "ScriptableObjects/Mutations/Polycythemia")]
	public class Polycythemia : MutationSO
	{
		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InPolycythemia(BodyPart,_RelatedMutationSO);
		}

		private class InPolycythemia: Mutation
		{

			public Items.Implants.Organs.Bones Bone;

			public InPolycythemia(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				Bone = BodyPart.GetComponent<Items.Implants.Organs.Bones>();
				Bone.GenerationOvershoot += 1f;
				Bone.BloodGeneratedByOneNutriment += 50;
			}

			public override void Remove()
			{
				Bone.GenerationOvershoot -= 1f;
				Bone.BloodGeneratedByOneNutriment -= 50;
			}

		}
	}
}
