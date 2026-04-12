using System;
using US13.Objects.Engineering;

namespace US13.Systems.Electricity.Interfaces
{
	public interface INodeControl
	{
		public event Action<PowerState, PowerState> OnStateChangeEvent;
		void PowerNetworkUpdate();
		PowerState SetPowerStateFromVoltage();
	}
}
