using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Items.Implants.Organs;
using UnityEngine;

[CreateAssetMenu(fileName = "Blindness", menuName = "ScriptableObjects/Mutations/Blindness")]
public class BlindnessMutation  : MutationSO
{
	public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
	{
		return new InBlindnessMutation(BodyPart,_RelatedMutationSO);
	}

	public class InBlindnessMutation: Mutation
	{

		public Eye RelatedEye;

		public InBlindnessMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
		{

		}

		public override void SetUp()
		{
			RelatedEye = BodyPart.GetComponent<Eye>();
			RelatedEye.PreventsBlindness = true;
		}

		public override void Remove()
		{
			RelatedEye.PreventsBlindness = false;
		}

	}
}
