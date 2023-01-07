using Items.Implants.Organs;
using UnityEngine;

namespace HealthV2.Living.Mutations.Brain
{
	[CreateAssetMenu(fileName = "MutationTelekinesis", menuName = "ScriptableObjects/Mutations/MutationTelekinesis")]
	public class MutationTelekinesis : MutationSO
	{
		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InTelekinesis(BodyPart,_RelatedMutationSO);
		}

		private class InTelekinesis: Mutation
		{

			public Items.Implants.Organs.Brain RelatedBrain;

			public InTelekinesis(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				RelatedBrain = BodyPart.GetComponent<Items.Implants.Organs.Brain>();
				RelatedBrain.SyncTelekinesis(RelatedBrain.HasTelekinesis,true);
			}

			public override void Remove()
			{
				RelatedBrain.SyncTelekinesis(RelatedBrain.HasTelekinesis,false);
			}

		}
	}
}
