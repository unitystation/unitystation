using System;
using US13.HealthV2.Living.BodyParts;

namespace US13.HealthV2.Living.PolymorphicSystems.Bodypart
{
	[System.Serializable]
	public class BatteryPoweredComponent : BodyPartComponentBase<BatterySystem>
	{
		/// <summary>
		/// Modifier that multiplicatively reduces the efficiency Body part depending on battery power
		/// </summary>
		[NonSerialized]
		public Modifier PowerModifier = new Modifier();

		public float WattsConsumption = 0.25f;

		public override void OnInitialize()
		{
			base.OnInitialize();
			RelatedPart.AddModifier(PowerModifier);
		}
	}
}
