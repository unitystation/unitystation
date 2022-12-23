using UnityEngine;

namespace HealthV2.Living.Mutations.Metabolism
{
	[CreateAssetMenu(fileName = "ToxicResistance", menuName = "ScriptableObjects/Mutations/ToxicResistance")]
	public class ToxicResistance : MutationSO
	{

		public float ToxResistancePercentage = 0.66f;

		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InToxicResistance(BodyPart,_RelatedMutationSO);
		}

		private class InToxicResistance: Mutation
		{

			public ToxicResistance ToxicResistance => (RelatedMutationSO as ToxicResistance);
			public BodyPart Related;

			public InToxicResistance(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				Related = BodyPart;
				Related.damageWeaknesses.Tox += ToxicResistance.ToxResistancePercentage; // so if 1 == 0.33%
			}

			public override void Remove()
			{
				Related.damageWeaknesses.Tox -= ToxicResistance.ToxResistancePercentage; ;
			}

		}
	}
}
