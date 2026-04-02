using US13.HealthV2.Living.Metabolism;


namespace US13.HealthV2.Living.PolymorphicSystems.Hunger
{
	public interface IHungerCalculation
	{
		public HungerState CalculateHungerState(LivingHealthMasterBase creatureHealth, HungerSystem hungerSystem);
	}
}