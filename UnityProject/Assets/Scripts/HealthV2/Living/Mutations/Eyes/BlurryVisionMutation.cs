using Items.Implants.Organs;
using UnityEngine;

namespace HealthV2.Living.Mutations.Eyes
{
	[CreateAssetMenu(fileName = "BlurryVisionMutation", menuName = "ScriptableObjects/Mutations/BlurryVisionMutation")]
	public class BlurryVisionMutation  : MutationSO
	{

		public int BlurrinessStrength = 30;
		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InBlurryVisionMutation(BodyPart,_RelatedMutationSO, BlurrinessStrength);
		}

		private class InBlurryVisionMutation : Mutation
		{

			public Eye RelatedEye;
			public int Strength;

			public InBlurryVisionMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO,  int inMode) : base(BodyPart,_RelatedMutationSO)
			{
				Strength = inMode;
			}

			public override void SetUp()
			{
				RelatedEye = BodyPart.GetComponent<Eye>();
				RelatedEye.BaseBlurryVision = Strength;
				RelatedEye.UpdateBlurryEye();
			}

			public override void Remove()
			{
				RelatedEye.BaseBlurryVision = 0;
				RelatedEye.UpdateBlurryEye();
			}
		}
	}
}
