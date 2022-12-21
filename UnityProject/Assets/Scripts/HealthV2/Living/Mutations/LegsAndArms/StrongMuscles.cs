using System.Collections;
using System.Collections.Generic;
using HealthV2;
using HealthV2.Limbs;
using Items.Implants.Organs;
using UnityEngine;

[CreateAssetMenu(fileName = "StrongMuscles", menuName = "ScriptableObjects/Mutations/StrongMuscles")]
public class StrongMuscles : MutationSO
{
	public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
	{
		return new InStrongMuscles(BodyPart,_RelatedMutationSO);
	}

	private class InStrongMuscles: Mutation
	{

		public HumanoidLeg Leg;
		public HumanoidArm HumanoidArm;

		public InStrongMuscles(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
		{

		}

		public override void SetUp()
		{
			Leg = BodyPart.GetComponent<HumanoidLeg>();
			HumanoidArm = BodyPart.GetComponent<HumanoidArm>();

			if (Leg != null)
			{
				Leg.SetNewEfficiency(Leg.LegEfficiency + 1);
			}

			if (HumanoidArm != null)
			{
				HumanoidArm.SetNewEfficiency(HumanoidArm.ArmEfficiency + 1);
			}

		}

		public override void Remove()
		{
			if (Leg != null)
			{
				Leg.SetNewEfficiency(Leg.LegEfficiency - 1);
			}

			if (HumanoidArm != null)
			{
				HumanoidArm.SetNewEfficiency(HumanoidArm.ArmEfficiency - 1);
			}
		}

	}
}
