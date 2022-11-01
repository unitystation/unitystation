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
			//Stomach.StomachContents.SetMaxCapacity(99); //idk Custom thing, if it's preset custom
		}

		public override void Remove()
		{
			//Stomach.StomachContents.SetMaxCapacity(2);
		}

	}
}
