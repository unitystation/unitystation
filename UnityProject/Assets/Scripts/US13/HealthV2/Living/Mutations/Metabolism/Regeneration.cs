using UnityEngine;
using US13.HealthV2.Living.BodyParts;
using US13.HealthV2.Living.CirculatorySystem;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;

namespace US13.HealthV2.Living.Mutations.Metabolism
{
	[CreateAssetMenu(fileName = "Regeneration", menuName = "ScriptableObjects/Mutations/Regeneration")]
	public class Regeneration  : MutationSO
	{
		public float HealingNutriment = 20f;
		public float Healing= 20f;
		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InRegeneration(BodyPart,_RelatedMutationSO);
		}

		private class InRegeneration: Mutation
		{
			public Regeneration Regeneration => (RelatedMutationSO as Regeneration);

			public HungerComponent Related;

			public InRegeneration(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				Related = BodyPart.GetComponent<HungerComponent>();
				if (Related == null) return;
				Related.HealingNutrimentMultiplier += Regeneration.HealingNutriment;
				Related.ActualHealingNutrimentMultiplier += Regeneration.Healing;

			}

			public override void Remove()
			{
				if (Related == null) return;
				Related.HealingNutrimentMultiplier -= Regeneration.HealingNutriment;
				Related.ActualHealingNutrimentMultiplier -= Regeneration.Healing;
			}

		}
	}
}
