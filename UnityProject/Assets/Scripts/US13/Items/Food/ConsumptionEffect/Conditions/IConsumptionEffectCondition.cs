namespace US13.Items.Food.ConsumptionEffect.Conditions
{
	/// <summary>
	/// Interface gate for <see cref="EffectOnConsumed"/>.
	/// Concrete subclasses decide whether an effect should fire for a given consumption event.
	/// </summary>
	public interface IConsumptionEffectCondition
	{
		/// <returns><see langword="true"/> if the associated effect should be applied.</returns>
		bool IsValid(ConsumptionContext context);
	}
}