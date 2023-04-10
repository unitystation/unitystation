using System;

namespace HealthV2.Living.PolymorphicSystems.Bodypart
{
	public class BatteryPoweredComponent : BodyPartComponentBase<BatterySystem>
	{
		/// <summary>
		/// Modifier that multiplicatively reduces the efficiency Body part depending on battery power
		/// </summary>
		[NonSerialized]
		public Modifier PowerModifier = new Modifier();

		public float WattConsumption = 1;

		public override void Awake()
		{
			base.Awake();
			RelatedPart.AddModifier(PowerModifier);
		}
	}
}
