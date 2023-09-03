using System.Collections.Generic;
using Logs;

namespace Systems.Electricity
{
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

		/// <summary>
		/// Find possible connections (FPC). List of electrical connections.
		/// </summary>
		public static List<IntrinsicElectronicDataList> PooledFPCList = new List<IntrinsicElectronicDataList>();

		//ElectronicSupplyData
		public static List<ElectronicSupplyData> PooledElectronicSupplyData = new List<ElectronicSupplyData>();

		public static List<SupplyBool> PooledSupplyBool = new List<SupplyBool>();

		public static void PoolsStatuses()
		{
			Loggy.Log("PooledResistanceWraps" + PooledResistanceWraps.Count, Category.Electrical);
			Loggy.Log("PooledVIRResistances" + PooledVIRResistances.Count, Category.Electrical);
			Loggy.Log("PooledVIRCurrent" + PooledVIRCurrent.Count, Category.Electrical);
			Loggy.Log("PooledWrapCurrent" + PooledWrapCurrent.Count, Category.Electrical);
		}

		public static ResistanceWrap GetResistanceWrap()
		{
			if (PooledResistanceWraps.Count > 0)
			{
				var ResistanceWrap = PooledResistanceWraps[0];
				PooledResistanceWraps.RemoveAt(0);
				ResistanceWrap.inPool = false;
				return ResistanceWrap;
			}

			return new ResistanceWrap();
		}

		public static VIRResistances GetVIRResistances()
		{
			if (PooledVIRResistances.Count > 0)
			{
				var VIRResistances = PooledVIRResistances[0];
				PooledVIRResistances.RemoveAt(0);
				VIRResistances.inPool = false;
				return VIRResistances;
			}

			return new VIRResistances();
		}

		public static VIRCurrent GetVIRCurrent()
		{
			if (PooledVIRCurrent.Count > 0)
			{
				var VIRCurrent = PooledVIRCurrent[0];
				PooledVIRCurrent.RemoveAt(0);
				VIRCurrent.inPool = false;
				return VIRCurrent;
			}

			return new VIRCurrent();
		}

		public static WrapCurrent GetWrapCurrent()
		{
			if (PooledWrapCurrent.Count > 0)
			{
				var WrapCurrent = PooledWrapCurrent[0];
				PooledWrapCurrent.RemoveAt(0);
				WrapCurrent.inPool = false;
				return WrapCurrent;
			}

			return new WrapCurrent();
		}

		public static IntrinsicElectronicDataList GetFPCList()
		{
			lock (PooledFPCList)
			{
				if (PooledFPCList.Count > 0)
				{
					var FPCList = PooledFPCList[0];
					PooledFPCList.RemoveAt(0);
					return FPCList;
				}

				return new IntrinsicElectronicDataList();
			}
		}


		public class IntrinsicElectronicDataList
		{
			public List<IntrinsicElectronicData> List = new List<IntrinsicElectronicData>();

			public void Pool()
			{
				List.Clear();
				ElectricalPool.PooledFPCList.Add(this);
			}
		}

		public static ElectronicSupplyData GetElectronicSupplyData()
		{
			if (PooledElectronicSupplyData.Count > 0)
			{
				var ElectronicSupplyData = PooledElectronicSupplyData[0];
				PooledElectronicSupplyData.RemoveAt(0);
				return ElectronicSupplyData;
			}

			return new ElectronicSupplyData();
		}

		public static SupplyBool GetSupplyBool()
		{
			if (PooledSupplyBool.Count > 0)
			{
				var QEntry = PooledSupplyBool[0];
				PooledSupplyBool.RemoveAt(0);
				return QEntry;
			}

			return new SupplyBool();
		}
	}
}