using UnityEngine;
using US13.HealthV2.Living.BodyParts;
using US13.HealthV2.Living.CirculatorySystem;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;

namespace US13.HealthV2.Living.Mutations.Metabolism
{
	[CreateAssetMenu(fileName = "HeightenedChemicalMetabolism", menuName = "ScriptableObjects/Mutations/HeightenedChemicalMetabolism")]
	public class HeightenedChemicalMetabolism : MutationSO
	{

		public float ReagentMetabolism = 3;


		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InHeightenedChemicalMetabolism(BodyPart,_RelatedMutationSO);
		}

		private class InHeightenedChemicalMetabolism: Mutation
		{
			public HeightenedChemicalMetabolism HeightenedChemicalMetabolism => RelatedMutationSO as HeightenedChemicalMetabolism;

			public MetabolismComponent Related;

			public InHeightenedChemicalMetabolism(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				Related = BodyPart.GetComponent<MetabolismComponent>();
				Related.ReagentMetabolism += HeightenedChemicalMetabolism.ReagentMetabolism;
			}

			public override void Remove()
			{
				Related.ReagentMetabolism -= HeightenedChemicalMetabolism.ReagentMetabolism;
			}

		}
	}
}
