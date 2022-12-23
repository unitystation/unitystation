using UnityEngine;

namespace HealthV2.Living.Mutations.Metabolism
{
	[CreateAssetMenu(fileName = "ToxicResistance", menuName = "ScriptableObjects/Mutations/ToxicResistance")]
	public class ToxicResistance : MutationSO
	{
		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InToxicResistance(BodyPart,_RelatedMutationSO);
		}

		private class InToxicResistance: Mutation
		{

			public BodyPart Related;

			public InToxicResistance(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				Related = BodyPart;
				Related.damageWeaknesses.Tox += -0.66f; // so if 1 == 0.33%
			}

			public override void Remove()
			{
				Related.damageWeaknesses.Tox -= -0.66f;
			}

		}
	}
}
