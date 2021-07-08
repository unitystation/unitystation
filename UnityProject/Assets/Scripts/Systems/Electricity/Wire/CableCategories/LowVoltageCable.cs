using System.Collections;
using System.Collections.Generic;

namespace Objects.Electrical
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
