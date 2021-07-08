using System.Collections;
using System.Collections.Generic;

namespace Systems.Electricity
{
	public class ElectronicSupplyData
	{
		public Dictionary<IntrinsicElectronicData, VIRCurrent> CurrentGoingTo = new Dictionary<IntrinsicElectronicData, VIRCurrent>();
		public Dictionary<IntrinsicElectronicData, VIRCurrent> CurrentComingFrom = new Dictionary<IntrinsicElectronicData, VIRCurrent>();
		public Dictionary<IntrinsicElectronicData, VIRResistances> ResistanceGoingTo = new Dictionary<IntrinsicElectronicData, VIRResistances>();
		public Dictionary<IntrinsicElectronicData, VIRResistances> ResistanceComingFrom = new Dictionary<IntrinsicElectronicData, VIRResistances>();
		public float SourceVoltage = 0;
		public HashSet<IntrinsicElectronicData> Downstream = new HashSet<IntrinsicElectronicData>();
		public HashSet<IntrinsicElectronicData> Upstream = new HashSet<IntrinsicElectronicData>();

		public override string ToString()
		{
			return string.Join(",", CurrentGoingTo);
		}

		public void Pool()
		{
			ElectricalDataCleanup.Pool(CurrentGoingTo);
			ElectricalDataCleanup.Pool(CurrentComingFrom);
			ElectricalDataCleanup.Pool(ResistanceGoingTo);
			ElectricalDataCleanup.Pool(ResistanceComingFrom);
			Downstream.Clear();
			Upstream.Clear();
			SourceVoltage = 0;
			ElectricalPool.PooledElectronicSupplyData.Add(this);
		}
	}
}
