using US13.Objects.Engineering;

namespace US13.Systems.Electricity.Interfaces
{
	public interface IAPCPowerable
	{
		void PowerNetworkUpdate(float voltage);

		void StateUpdate(PowerState state);
	}
}
