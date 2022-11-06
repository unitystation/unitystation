using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

[CreateAssetMenu(fileName = "XRayVision", menuName = "ScriptableObjects/Mutations/XRayVision")]
public class XRayVision  : MutationSO
{
	public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
	{
		return new InXRayVision(BodyPart,_RelatedMutationSO);
	}

	public class InXRayVision: Mutation
	{

		public Eye RelatedEye;

		public InXRayVision(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
		{

		}

		public override void SetUp()
		{
			RelatedEye = BodyPart.GetComponent<Eye>();
			RelatedEye.HasXray.RecordPosition(this, true);
		}

		public override void Remove()
		{
			RelatedEye.HasXray.RecordPosition(this, false);
		}

	}
}
