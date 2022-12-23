using UnityEngine;

namespace HealthV2.Living.Mutations.Metabolism
{
	[CreateAssetMenu(fileName = "HeightenedChemicalMetabolism", menuName = "ScriptableObjects/Mutations/HeightenedChemicalMetabolism")]
	public class HeightenedChemicalMetabolism : MutationSO
	{
		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InHeightenedChemicalMetabolism(BodyPart,_RelatedMutationSO);
		}

		private class InHeightenedChemicalMetabolism: Mutation
		{

			public BodyPart Related;

			public InHeightenedChemicalMetabolism(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				Related = BodyPart;
				Related.ReagentMetabolism += 3;
			}

			public override void Remove()
			{
				Related.ReagentMetabolism -= 3;
			}

		}
	}
}
