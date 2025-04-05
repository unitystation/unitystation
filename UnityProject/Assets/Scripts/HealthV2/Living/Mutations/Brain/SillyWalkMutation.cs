using UnityEngine;

namespace HealthV2.Living.Mutations.Bones
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
				Brain = BodyPart.GetComponentCustom<Items.Implants.Organs.Brain>();
				Brain.SetSillyWalk(true);

			}

			public override void Remove()
			{
				Brain.SetSillyWalk(false);
			}

		}
	}
}
