using HealthV2.Limbs;
using UnityEngine;

namespace HealthV2.Living.Mutations.LegsAndArms
{
	[CreateAssetMenu(fileName = "WeakMuscles", menuName = "ScriptableObjects/Mutations/WeakMuscles")]
	public class WeakMuscles : MutationSO
	{

		public float RemovedEfficiency = 0.75f;

		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InWeakMuscles(BodyPart,_RelatedMutationSO);
		}

		private class InWeakMuscles: Mutation
		{

			public WeakMuscles WeakMuscles => RelatedMutationSO as WeakMuscles;

			public HumanoidLeg Leg;
			public HumanoidArm HumanoidArm;

			public InWeakMuscles(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				Leg = BodyPart.GetComponent<HumanoidLeg>();
				HumanoidArm = BodyPart.GetComponent<HumanoidArm>();

				if (Leg != null)
				{
					Leg.SetNewEfficiency(WeakMuscles.RemovedEfficiency, this);
				}

				if (HumanoidArm != null)
				{
					HumanoidArm.SetNewEfficiency(WeakMuscles.RemovedEfficiency, this);
				}

			}

			public override void Remove()
			{
				if (Leg != null)
				{
					Leg.SetNewEfficiency(WeakMuscles.RemovedEfficiency, this);
				}

				if (HumanoidArm != null)
				{
					HumanoidArm.SetNewEfficiency(WeakMuscles.RemovedEfficiency, this);
				}
			}

		}
	}
}