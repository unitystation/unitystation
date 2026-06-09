using System.Collections.Generic;
using US13.Managers;
using US13.Systems.Electricity.FunctionsAndClasses;
using US13.Systems.Electricity.Inheritance;

namespace US13.Systems.Electricity.Wire.CableCategories
{
	public class HighVoltageCable : CableInheritance
	{
		public new HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>()
		{
			PowerTypeCategory.PowerGenerator,
			PowerTypeCategory.RadiationCollector,
			PowerTypeCategory.HighVoltageCable,
			PowerTypeCategory.Transformer,
			PowerTypeCategory.PowerSink,
			PowerTypeCategory.VoltageProbe,
		};

		void Awake()
		{
			ApplianceType = PowerTypeCategory.HighVoltageCable;
			CableType = WiringColor.high;
			wireConnect.InData.CanConnectTo = CanConnectTo;
			wireConnect.InData.Categorytype = ApplianceType;
		}
	}
}
