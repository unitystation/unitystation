using UnityEngine;

namespace HealthV2.Living.Mutations.Bones
{
	[CreateAssetMenu(fileName = "Anemia", menuName = "ScriptableObjects/Mutations/Anemia")]
	public class Anemia : MutationSO
	{
		public float BloodRegenerationRemove = 28f;

		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InAnemia(BodyPart,_RelatedMutationSO);
		}

		private class InAnemia: Mutation
		{

			public Anemia Anemia => RelatedMutationSO as Anemia;

			public Items.Implants.Organs.Bones Bone;

			public InAnemia(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				Bone = BodyPart.GetComponent<Items.Implants.Organs.Bones>();
				Bone.BloodGeneratedByOneNutriment -= Anemia.BloodRegenerationRemove;
			}

			public override void Remove()
			{
				Bone.BloodGeneratedByOneNutriment += Anemia.BloodRegenerationRemove;
			}

		}
	}
}
