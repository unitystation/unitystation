using UnityEngine;

namespace HealthV2.Living.Mutations.Metabolism
{
	[CreateAssetMenu(fileName = "AnaerobicMetabolism", menuName = "ScriptableObjects/Mutations/AnaerobicMetabolism")]
	public class AnaerobicMetabolism : MutationSO
	{
		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InAnaerobicMetabolism(BodyPart,_RelatedMutationSO);
		}

		private class InAnaerobicMetabolism : Mutation
		{


			public InAnaerobicMetabolism(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				BodyPart.SetIsBloodReagentConsumed(false);
			}

			public override void Remove()
			{
				BodyPart.SetIsBloodReagentConsumed(true);
			}

		}
	}
}
