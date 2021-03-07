using System.Collections.Generic;

public class ElectricalPool
{
	//ResistanceWrap
	public static List<ResistanceWrap> PooledResistanceWraps = new List<ResistanceWrap>();

	//VIRResistances
	public static List<VIRResistances> PooledVIRResistances = new List<VIRResistances>();

	//VIRCurrent
	public static List<VIRCurrent> PooledVIRCurrent = new List<VIRCurrent>();

	//WrapCurrent
	public static List<WrapCurrent> PooledWrapCurrent = new List<WrapCurrent>();

	//Find possible connections
	public static List<List<IntrinsicElectronicData>> PooledFPCList = new List<List<IntrinsicElectronicData>>();

	//ElectronicSupplyData
	public static List<ElectronicSupplyData> PooledElectronicSupplyData = new List<ElectronicSupplyData>();

	public static List<SupplyBool> PooledSupplyBool = new List<SupplyBool>();

	public static void PoolsStatuses()
	{
		Logger.Log("PooledResistanceWraps" + PooledResistanceWraps.Count);
		Logger.Log("PooledVIRResistances" + PooledVIRResistances.Count);
		Logger.Log("PooledVIRCurrent" + PooledVIRCurrent.Count);
		Logger.Log("PooledWrapCurrent" + PooledWrapCurrent.Count);
	}

	public static ResistanceWrap GetResistanceWrap()
	{
		if (PooledResistanceWraps.Count > 0)
		{
			var ResistanceWrap = PooledResistanceWraps[0];
			PooledResistanceWraps.RemoveAt(0);
			ResistanceWrap.inPool = false;
			return (ResistanceWrap);
		}
		return (new ResistanceWrap());
	}

	public static VIRResistances GetVIRResistances()
	{
		if (PooledVIRResistances.Count > 0)
		{
			var VIRResistances = PooledVIRResistances[0];
			PooledVIRResistances.RemoveAt(0);
			VIRResistances.inPool = false;
			return (VIRResistances);
		}
		return (new VIRResistances());
	}

	public static VIRCurrent GetVIRCurrent()
	{
		if (PooledVIRCurrent.Count > 0)
		{
			var VIRCurrent = PooledVIRCurrent[0];
			PooledVIRCurrent.RemoveAt(0);
			VIRCurrent.inPool = false;
			return (VIRCurrent);
		}
		return (new VIRCurrent());
	}

	public static WrapCurrent GetWrapCurrent()
	{
		if (PooledWrapCurrent.Count > 0)
		{
			var WrapCurrent = PooledWrapCurrent[0];
			PooledWrapCurrent.RemoveAt(0);
			WrapCurrent.inPool = false;
			return (WrapCurrent);
		}
		return (new WrapCurrent());
	}

	public static List<IntrinsicElectronicData> GetFPCList()
	{
		lock (PooledFPCList)
		{
			if (PooledFPCList.Count > 0)
			{
				var FPCList = PooledFPCList[0];
				PooledFPCList.RemoveAt(0);
				FPCList.Clear();
				return (FPCList);
			}
			return (new List<IntrinsicElectronicData>());
		}
	}

	public static ElectronicSupplyData GetElectronicSupplyData()
	{
		if (PooledElectronicSupplyData.Count > 0)
		{
			var ElectronicSupplyData = PooledElectronicSupplyData[0];
			PooledElectronicSupplyData.RemoveAt(0);
			return (ElectronicSupplyData);
		}
		return (new ElectronicSupplyData());
	}

	public static SupplyBool GetSupplyBool()
	{
		if (PooledSupplyBool.Count > 0)
		{
			var QEntry = PooledSupplyBool[0];
			PooledSupplyBool.RemoveAt(0);
			return (QEntry);
		}
		return (new SupplyBool());
	}

}