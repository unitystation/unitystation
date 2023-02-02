using UnityEngine;

namespace HealthV2.Living.Mutations.Brain
{
	[CreateAssetMenu(fileName = "AlcoholEffectMutation", menuName = "ScriptableObjects/Mutations/AlcoholEffectMutation")]
	public class AlcoholEffectMutation  : MutationSO
	{
		public float DrugResistanceAddFraction = 0.10f;
		//-0.5

		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InAlcoholEffectMutations(BodyPart,_RelatedMutationSO, DrugResistanceAddFraction);
		}

		private class InAlcoholEffectMutations: Mutation
		{

			public Items.Implants.Organs.Brain RelatedBrain;
			public float DrugResistanceAddFraction = 0;

			public InAlcoholEffectMutations(BodyPart BodyPart,MutationSO _RelatedMutationSO, float NewDrugResistanceAddFraction) : base(BodyPart,_RelatedMutationSO)
			{
				DrugResistanceAddFraction = NewDrugResistanceAddFraction;
			}

			public override void SetUp()
			{
				RelatedBrain = BodyPart.GetComponent<Items.Implants.Organs.Brain>();
				RelatedBrain.MaxDrunkAtPercentage += DrugResistanceAddFraction;
			}

			public override void Remove()
			{
				RelatedBrain.MaxDrunkAtPercentage -= DrugResistanceAddFraction;
			}

		}
	}
}
