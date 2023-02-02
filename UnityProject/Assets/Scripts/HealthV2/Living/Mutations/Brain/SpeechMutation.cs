using Items.Implants.Organs;
using UnityEngine;

namespace HealthV2.Living.Mutations.Brain
{
	[CreateAssetMenu(fileName = "SpeechMutation", menuName = "ScriptableObjects/Mutations/SpeechMutation")]
	public class SpeechMutation : MutationSO
	{

		public ChatModifier ChatModifierToApply = ChatModifier.None;
		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InSpeechMutation(BodyPart,_RelatedMutationSO, ChatModifierToApply);
		}

		private class InSpeechMutation: Mutation
		{
			public ChatModifier ChatModifierToApply = ChatModifier.None;
			public Items.Implants.Organs.Brain RelatedBrain;
			public InSpeechMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO, ChatModifier chatModifier ) : base(BodyPart,_RelatedMutationSO)
			{
				ChatModifierToApply = chatModifier;
			}

			public override void SetUp()
			{
				RelatedBrain = BodyPart.GetComponent<Items.Implants.Organs.Brain>();
				RelatedBrain.BodyChatModifier |= ChatModifierToApply;
				RelatedBrain.UpdateChatModifier(true);
			}

			public override void Remove()
			{
				RelatedBrain.BodyChatModifier &= ~ChatModifierToApply;
				RelatedBrain.UpdateChatModifier(false);
			}
		}
	}
}
