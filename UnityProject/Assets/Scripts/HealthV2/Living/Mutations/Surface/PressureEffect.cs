using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2.Living.Mutations.Surface
{

	[CreateAssetMenu(fileName = "PressureEffect", menuName = "ScriptableObjects/Mutations/PressureEffect")]
	public class PressureEffect : MutationSO
	{

		[Tooltip("so, + Would decrease how much Pressure you can handle - increases how much Pressure you can handle think of it as Delta Pressure")]
		public float AddMinimumPressure = 0;

		[Tooltip("so, - Would decrease how much Higherpressure you can handle + increases how much Higherpressure you can handle think of it as Delta pressure")]
		public float AddHigherpressure = 0;


		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InPressureEffect(BodyPart,_RelatedMutationSO, AddMinimumPressure, AddHigherpressure);
		}

		private class InPressureEffect: Mutation
		{
			public float AddMinimumPressure;
			public float AddHigherpressure;

			public InPressureEffect(BodyPart BodyPart,MutationSO _RelatedMutationSO, float inAddMinimumPressure, float InAddHigherpressure) : base(BodyPart,_RelatedMutationSO)
			{
				AddMinimumPressure = inAddMinimumPressure;
				AddHigherpressure = InAddHigherpressure;

			}

			public override void SetUp()
			{
				BodyPart.SelfArmor.PressureProtectionInKpa.x += AddMinimumPressure;
				BodyPart.SelfArmor.PressureProtectionInKpa.y += AddHigherpressure;
			}

			public override void Remove()
			{
				BodyPart.SelfArmor.PressureProtectionInKpa.x -= AddMinimumPressure;
				BodyPart.SelfArmor.PressureProtectionInKpa.y -= AddHigherpressure;
			}
		}
	}
}