using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricalPool
{
	public static void PoolsStatuses()
	{
		/*Logger.Log("PooledResistanceWraps" + PooledResistanceWraps.Count);
		Logger.Log("PooledVIRResistances" + PooledVIRResistances.Count);
		Logger.Log("PooledVIRCurrent" + PooledVIRCurrent.Count);
		Logger.Log("PooledWrapCurrent" + PooledWrapCurrent.Count);
		Logger.Log("PooledQEntry" + PooledQEntry.Count);*/
	}


	//ResistanceWrap
	public static List<ResistanceWrap> PooledResistanceWraps = new List<ResistanceWrap>();
	public static ResistanceWrap GetResistanceWrap()
	{
		if (PooledResistanceWraps.Count > 0)
		{
			var ResistanceWrap = PooledResistanceWraps[0];
			PooledResistanceWraps.RemoveAt(0);
			ResistanceWrap.inPool = false;
			return (ResistanceWrap);
		}
		else {
			return (new ResistanceWrap());
		}
	}


	//VIRResistances
	public static List<VIRResistances> PooledVIRResistances = new List<VIRResistances>();
	public static VIRResistances GetVIRResistances()
	{
		if (PooledVIRResistances.Count > 0)
		{
			var VIRResistances = PooledVIRResistances[0];
			PooledVIRResistances.RemoveAt(0);
			VIRResistances.inPool = false;
			return (VIRResistances);
		}
		else
		{
			//ElectricalDataCleanup.cs:197)Logger.Log("new VIRResistances");
			return (new VIRResistances());
		}
	}

	//VIRCurrent
	public static List<VIRCurrent> PooledVIRCurrent = new List<VIRCurrent>();
	public static VIRCurrent GetVIRCurrent()
	{
		if (PooledVIRCurrent.Count > 0)
		{
			var VIRCurrent = PooledVIRCurrent[0];
			PooledVIRCurrent.RemoveAt(0);
			VIRCurrent.inPool = false;
			return (VIRCurrent);
		}
		else {
			return (new VIRCurrent());
		}
	}


	//WrapCurrent
	public static List<WrapCurrent> PooledWrapCurrent = new List<WrapCurrent>();
	public static WrapCurrent GetWrapCurrent()
	{
		if (PooledWrapCurrent.Count > 0)
		{
			var WrapCurrent = PooledWrapCurrent[0];
			PooledWrapCurrent.RemoveAt(0);
			WrapCurrent.inPool = false;
			return (WrapCurrent);
		}
		else {
			return (new WrapCurrent());
		}
	}


	//Find possible connections
	public static List<List<IntrinsicElectronicData>> PooledFPCList = new List<List<IntrinsicElectronicData>>();
	public static List<IntrinsicElectronicData> GetFPCList()
	{
		if (PooledFPCList.Count > 0)
		{
			var FPCList = PooledFPCList[0];
			PooledFPCList.RemoveAt(0);
			FPCList.Clear();
			return (FPCList);
		}
		else {
			return (new List<IntrinsicElectronicData>());
		}
	}


	//ElectronicSupplyData
	public static List<ElectronicSupplyData> PooledElectronicSupplyData= new List<ElectronicSupplyData>();
	public static ElectronicSupplyData GetElectronicSupplyData()
	{
		if (PooledElectronicSupplyData.Count > 0)
		{
			var ElectronicSupplyData = PooledElectronicSupplyData[0];
			PooledElectronicSupplyData.RemoveAt(0);
			return (ElectronicSupplyData);
		}
		else {
			return (new ElectronicSupplyData());
		}
	}

	//KeyValuePair<ElectricalOIinheritance, IntrinsicElectronicData> edd
	public static List<ElectricalSynchronisation.QEntry> PooledQEntry
	 = new List<ElectricalSynchronisation.QEntry>();
	public static ElectricalSynchronisation.QEntry GetQEntry()
	{
		if (PooledQEntry.Count > 0)
		{
			var QEntry = PooledQEntry[0];
			PooledQEntry.RemoveAt(0);
			return (QEntry);
		}
		else {
			return (new ElectricalSynchronisation.QEntry());
		}
	}
}