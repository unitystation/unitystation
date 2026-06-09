using UnityEngine;
using US13.HealthV2.Living.BodyParts;
using US13.HealthV2.Living.CirculatorySystem;
using Util;

namespace US13.HealthV2.Living.Mutations.Brain
{
	[CreateAssetMenu(fileName = "SillyWalk", menuName = "ScriptableObjects/Mutations/SillyWalk")]
	public class SillyWalkMutation : MutationSO
	{

		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InSillyWalk(BodyPart,_RelatedMutationSO);
		}

		private class InSillyWalk: Mutation
		{

			public SillyWalkMutation Anemia => RelatedMutationSO as SillyWalkMutation;

			public Items.Implants.Organs.Brain Brain;

			public InSillyWalk(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				Brain = SweetExtensions.GetCachedComponent<Items.Implants.Organs.Brain>((Component)BodyPart);
				Brain.SetSillyWalk(true);

			}

			public override void Remove()
			{
				Brain.SetSillyWalk(false);
			}

		}
	}
}
