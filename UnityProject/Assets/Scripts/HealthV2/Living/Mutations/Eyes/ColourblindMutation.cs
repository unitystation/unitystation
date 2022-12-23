using Items.Implants.Organs;
using UnityEngine;

namespace HealthV2.Living.Mutations.Eyes
{
	[CreateAssetMenu(fileName = "Colourblindness", menuName = "ScriptableObjects/Mutations/Colourblindness")]
	public class ColourblindMutation : MutationSO
	{


		public ColourBlindMode ColourBlindMode;
		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InColourblindMutation(BodyPart,_RelatedMutationSO, ColourBlindMode);
		}

		private class InColourblindMutation : Mutation
		{

			public Eye RelatedEye;
			public ColourBlindMode Mode;

			public InColourblindMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO,  ColourBlindMode inMode) : base(BodyPart,_RelatedMutationSO)
			{
				Mode = inMode;
			}

			public override void SetUp()
			{
				RelatedEye = BodyPart.GetComponent<Eye>();
				RelatedEye.CurrentColourblindness = RelatedEye.CurrentColourblindness | Mode;

			}

			public override void Remove()
			{
				RelatedEye.CurrentColourblindness = RelatedEye.CurrentColourblindness & ~Mode;
			}
		}
	}
}
