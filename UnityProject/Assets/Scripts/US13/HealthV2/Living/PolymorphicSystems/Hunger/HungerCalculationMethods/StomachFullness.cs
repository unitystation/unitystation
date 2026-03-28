using US13.HealthV2.Living.Metabolism;

namespace US13.HealthV2.Living.PolymorphicSystems.Hunger.HungerCalculationMethods
{
	public class StomachFullness : IHungerCalculation
	{
		public HungerState CalculateHungerState(LivingHealthMasterBase creatureHealth, HungerSystem hungerSystem)
		{
			var stomachs = creatureHealth.GetStomachs();
			foreach (var stomach in stomachs)
			{
				if (stomach.StomachContents.SpareCapacity <= stomach.StomachIsConsideredFullWhenSpareCapacityIsLessThan)
				{
					return HungerState.Full;
				}

				if (stomach.StomachContents.SpareCapacity <=
				    stomach.StomachIsConsideredFullWhenSpareCapacityIsLessThan * 2)
				{
					return HungerState.Normal;
				}
				if (stomach.StomachContents.SpareCapacity <=
				    stomach.StomachIsConsideredFullWhenSpareCapacityIsLessThan * 4)
				{
					return HungerState.Malnourished;
				}
				else
				{
					return HungerState.Starving;
				}
			}
			return HungerState.Normal;
		}
	}
}