using UnityEngine;

namespace HealthV2.Living.Mutations.Surface
{
	[CreateAssetMenu(fileName = "ThermalEffect", menuName = "ScriptableObjects/Mutations/ThermalEffect")]
	public class ThermalEffect : MutationSO
	{

		[Tooltip("so, + Would decrease how much cold you can handle - increases how much Cold you can handle think of it as Delta temperature")]
		public float AddResistanceCold = 0;

		[Tooltip("so, - Would decrease how much Hot you can handle + increases how much Hot you can handle think of it as Delta temperature")]
		public float AddResistanceHot = 0;


		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InThermalEffect(BodyPart,_RelatedMutationSO, AddResistanceCold, AddResistanceHot);
		}

		private class InThermalEffect: Mutation
		{
			public float AddResistanceCold;
			public float AddResistanceHot;

			public InThermalEffect(BodyPart BodyPart,MutationSO _RelatedMutationSO, float inAddResistanceCold, float InAddResistanceHot ) : base(BodyPart,_RelatedMutationSO)
			{
				AddResistanceCold = inAddResistanceCold;
				AddResistanceHot = InAddResistanceHot;

			}

			public override void SetUp()
			{
				BodyPart.SelfArmor.TemperatureProtectionInK.x += AddResistanceCold;
				BodyPart.SelfArmor.TemperatureProtectionInK.y += AddResistanceHot;
			}

			public override void Remove()
			{
				BodyPart.SelfArmor.TemperatureProtectionInK.x -= AddResistanceCold;
				BodyPart.SelfArmor.TemperatureProtectionInK.y -= AddResistanceHot;
			}
		}
	}
}
