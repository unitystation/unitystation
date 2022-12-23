using Items.Implants.Organs;
using UnityEngine;

namespace HealthV2.Living.Mutations.Speech
{
	[CreateAssetMenu(fileName = "Mute", menuName = "ScriptableObjects/Mutations/Mute")]
	public class Mute : MutationSO
	{
		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InMute(BodyPart,_RelatedMutationSO);
		}

		private class InMute: Mutation
		{

			public Tongue RelatedTongue;
			public Brain RelatedBrain;
			public InMute(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{

				RelatedTongue = BodyPart.GetComponent<Tongue>();
				RelatedBrain = BodyPart.GetComponent<Brain>();

				if (RelatedTongue != null)
				{
					RelatedTongue.SetCannotSpeak( true);
				}

				if (RelatedBrain != null)
				{
					RelatedBrain.SetCannotSpeak(true);
				}
			}

			public override void Remove()
			{
				if (RelatedTongue != null)
				{
					RelatedTongue.SetCannotSpeak( false);
				}

				if (RelatedBrain != null)
				{
					RelatedBrain.SetCannotSpeak(false);
				}
			}

		}
	}
}
