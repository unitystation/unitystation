using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectronicSupplyData
{
	public Dictionary<ElectricalOIinheritance, float> CurrentGoingTo = new Dictionary<ElectricalOIinheritance, float>();
	public Dictionary<ElectricalOIinheritance, float> CurrentComingFrom  = new Dictionary<ElectricalOIinheritance, float>();
	public Dictionary<ElectricalOIinheritance, float> ResistanceGoingTo = new Dictionary<ElectricalOIinheritance, float>();
	public Dictionary<ElectricalOIinheritance, float> ResistanceComingFrom = new Dictionary<ElectricalOIinheritance, float>();
	public float SourceVoltages;
	public HashSet<ElectricalOIinheritance> Downstream = new HashSet<ElectricalOIinheritance>();
	public HashSet<ElectricalOIinheritance> Upstream = new HashSet<ElectricalOIinheritance>();
}
