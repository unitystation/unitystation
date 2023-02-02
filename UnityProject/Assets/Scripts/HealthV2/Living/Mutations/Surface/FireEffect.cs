using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2.Living.Mutations.Surface
{
	[CreateAssetMenu(fileName = "FireEffect", menuName = "ScriptableObjects/Mutations/FireEffect")]
	public class FireEffect : MutationSO
	{
		[Tooltip("Fire protection in percentage. Negative numbers increase incoming damage.")]
		[Range(-100,100)] public float AddFireProtection = 0;


		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InFireEffect(BodyPart,_RelatedMutationSO, AddFireProtection);
		}

		private class InFireEffect: Mutation
		{
			public float AddFireProtection = 0;

			public InFireEffect(BodyPart BodyPart,MutationSO _RelatedMutationSO, float inAddFireProtection) : base(BodyPart,_RelatedMutationSO)
			{
				AddFireProtection = inAddFireProtection;

			}

			public override void SetUp()
			{
				BodyPart.SelfArmor.Fire += AddFireProtection;
			}

			public override void Remove()
			{
				BodyPart.SelfArmor.Fire -= AddFireProtection;
			}
		}
	}
}