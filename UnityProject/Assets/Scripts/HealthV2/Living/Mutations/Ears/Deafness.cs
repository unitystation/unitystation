using UnityEngine;

namespace HealthV2.Living.Mutations.Ears
{
	[CreateAssetMenu(fileName = "Deafness", menuName = "ScriptableObjects/Mutations/Deafness")]
	public class Deafness  : MutationSO
	{
		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InDeafnessMutation(BodyPart,_RelatedMutationSO);
		}

		private class InDeafnessMutation: Mutation
		{

			public Items.Implants.Organs.Ears RelatedEars;

			public InDeafnessMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				RelatedEars = BodyPart.GetComponent<Items.Implants.Organs.Ears>();
				RelatedEars.MutationMultiplier = 0;
				RelatedEars.UpDateTotalValue();
			}

			public override void Remove()
			{
				RelatedEars.MutationMultiplier = 1;
				RelatedEars.UpDateTotalValue();
			}

		}
	}
}
