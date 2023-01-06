using Items.Implants.Organs;
using UnityEngine;

namespace HealthV2.Living.Mutations.Eyes
{
	[CreateAssetMenu(fileName = "XRayVision", menuName = "ScriptableObjects/Mutations/XRayVision")]
	public class XRayVision : MutationSO
	{
		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InXRayVision(BodyPart,_RelatedMutationSO);
		}

		private class InXRayVision: Mutation
		{

			public Eye RelatedEye;

			public InXRayVision(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				RelatedEye = BodyPart.GetComponent<Eye>();
				RelatedEye.SyncXrayState(RelatedEye.HasXray, true);
			}

			public override void Remove()
			{
				RelatedEye.SyncXrayState(RelatedEye.HasXray, false);
			}

		}
	}
}
