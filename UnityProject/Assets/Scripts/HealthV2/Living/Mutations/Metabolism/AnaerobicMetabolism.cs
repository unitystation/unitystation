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
			public SaturationComponent SaturationComponent;

			public InAnaerobicMetabolism(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				SaturationComponent = BodyPart.GetComponent<SaturationComponent>();
				SaturationComponent.SetNotIsBloodReagentConsumed(true);
			}

			public override void Remove()
			{
				SaturationComponent.SetNotIsBloodReagentConsumed(false);
			}

		}
	}
}
