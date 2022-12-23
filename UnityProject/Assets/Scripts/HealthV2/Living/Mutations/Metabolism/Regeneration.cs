using UnityEngine;
using UnityEngine.Serialization;

namespace HealthV2.Living.Mutations.Metabolism
{
	[CreateAssetMenu(fileName = "Regeneration", menuName = "ScriptableObjects/Mutations/Regeneration")]
	public class Regeneration  : MutationSO
	{
		public float HealingNutriment = 20f;

		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InRegeneration(BodyPart,_RelatedMutationSO);
		}

		private class InRegeneration: Mutation
		{
			public Regeneration Regeneration => (RelatedMutationSO as Regeneration);

			public BodyPart Related;

			public InRegeneration(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				Related = BodyPart;
				Related.HealingNutrimentMultiplier += Regeneration.HealingNutriment;
			}

			public override void Remove()
			{
				Related.HealingNutrimentMultiplier -= Regeneration.HealingNutriment;
			}

		}
	}
}
