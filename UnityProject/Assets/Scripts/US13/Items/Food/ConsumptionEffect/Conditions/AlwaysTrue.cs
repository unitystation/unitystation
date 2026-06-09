namespace US13.Items.Food.ConsumptionEffect.Conditions
{
	public class AlwaysTrue: IConsumptionEffectCondition
	{
		public bool IsValid(ConsumptionContext context)
		{
			return true;
		}
	}
}