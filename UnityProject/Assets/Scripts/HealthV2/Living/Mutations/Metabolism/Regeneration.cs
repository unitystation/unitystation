using UnityEngine;

namespace HealthV2.Living.Mutations.Metabolism
{
	[CreateAssetMenu(fileName = "Regeneration", menuName = "ScriptableObjects/Mutations/Regeneration")]
	public class Regeneration  : MutationSO
	{
		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InRegeneration(BodyPart,_RelatedMutationSO);
		}

		private class InRegeneration: Mutation
		{

			public BodyPart Related;

			public InRegeneration(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				Related = BodyPart;
				Related.HealingNutrimentMultiplier += 20;
			}

			public override void Remove()
			{
				Related.HealingNutrimentMultiplier -= 20;
			}

		}
	}
}
