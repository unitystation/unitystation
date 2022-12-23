using UnityEngine;

namespace HealthV2.Living.Mutations.Bones
{
	[CreateAssetMenu(fileName = "Polycythemia", menuName = "ScriptableObjects/Mutations/Polycythemia")]
	public class Polycythemia : MutationSO
	{
		public float GenerationOvershoot = 1;
		public float AddedBloodGeneratedByOneNutriment = 50;

		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InPolycythemia(BodyPart,_RelatedMutationSO);
		}

		private class InPolycythemia: Mutation
		{

			public Polycythemia Polycythemia => RelatedMutationSO as Polycythemia;
			public Items.Implants.Organs.Bones Bone;

			public InPolycythemia(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				Bone = BodyPart.GetComponent<Items.Implants.Organs.Bones>();
				Bone.GenerationOvershoot += Polycythemia.GenerationOvershoot;
				Bone.BloodGeneratedByOneNutriment += Polycythemia.AddedBloodGeneratedByOneNutriment;
			}

			public override void Remove()
			{
				Bone.GenerationOvershoot -= Polycythemia.GenerationOvershoot;
				Bone.BloodGeneratedByOneNutriment -= Polycythemia.AddedBloodGeneratedByOneNutriment;
			}

		}
	}
}
