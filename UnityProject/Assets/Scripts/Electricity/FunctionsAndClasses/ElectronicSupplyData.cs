using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectronicSupplyData
{
	public Dictionary<IntrinsicElectronicData, VIRCurrent> CurrentGoingTo = new Dictionary<IntrinsicElectronicData, VIRCurrent>();
	public Dictionary<IntrinsicElectronicData, VIRCurrent> CurrentComingFrom  = new Dictionary<IntrinsicElectronicData, VIRCurrent>();
	public Dictionary<IntrinsicElectronicData, VIRResistances> ResistanceGoingTo = new Dictionary<IntrinsicElectronicData, VIRResistances>();
	public Dictionary<IntrinsicElectronicData, VIRResistances> ResistanceComingFrom = new Dictionary<IntrinsicElectronicData, VIRResistances>();
	public List<double> SourceVoltages = new List<double>();
	public HashSet<ElectricalOIinheritance> Downstream = new HashSet<ElectricalOIinheritance>();
	public HashSet<ElectricalOIinheritance> Upstream = new HashSet<ElectricalOIinheritance>();
}
