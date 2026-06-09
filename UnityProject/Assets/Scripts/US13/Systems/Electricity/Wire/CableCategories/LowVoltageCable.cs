using System.Collections.Generic;
using US13.Managers;
using US13.Systems.Electricity.FunctionsAndClasses;
using US13.Systems.Electricity.Inheritance;

namespace US13.Systems.Electricity.Wire.CableCategories
{
	public class LowVoltageCable : CableInheritance
	{
		public new HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>()
		{
			PowerTypeCategory.LowMachineConnector,
			PowerTypeCategory.LowVoltageCable,
			PowerTypeCategory.SolarPanelController,
			PowerTypeCategory.SolarPanel,
			PowerTypeCategory.PowerSink,
		};

		private void Awake()
		{
			ApplianceType = PowerTypeCategory.LowVoltageCable;
			CableType = WiringColor.low;
			wireConnect.InData.CanConnectTo = CanConnectTo;
			wireConnect.InData.Categorytype = ApplianceType;
		}
	}
}
