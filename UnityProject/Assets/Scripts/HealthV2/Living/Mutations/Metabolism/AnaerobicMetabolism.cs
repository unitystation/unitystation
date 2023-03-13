using HealthV2.Living.PolymorphicSystems.Bodypart;
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
			public ReagentCirculatedComponent ReagentCirculatedComponent;

			public InAnaerobicMetabolism(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				ReagentCirculatedComponent = BodyPart.GetComponent<ReagentCirculatedComponent>();
				ReagentCirculatedComponent.SetIsBloodReagentConsumed(false);
			}

			public override void Remove()
			{
				ReagentCirculatedComponent.SetIsBloodReagentConsumed(true);
			}

		}
	}
}
