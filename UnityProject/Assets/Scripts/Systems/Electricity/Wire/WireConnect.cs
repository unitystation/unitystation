using System.Collections;
using System.Collections.Generic;
using Objects.Electrical;

namespace Systems.Electricity
{
	public class WireConnect : ElectricalOIinheritance
	{
		public CableInheritance ControllingCable;
		public CableLine RelatedLine;
		public MachineConnectorSpriteHandler SpriteHandler;

		/// <summary>
		///The function for out putting current into other nodes (Basically doing ElectricityInput On another one)
		/// </summary>
		/*public override void ElectricityOutput(WrapCurrent Current, ElectricalOIinheritance SourceInstance, ElectricalOIinheritance ComingFrom, ElectricalDirectionStep Path)
		{
			if (Logall)
			{
				Logger.Log("this > " + this + "ElectricityOutput, Current > " + Current + " SourceInstance  > " + SourceInstance, Category.Electrical);
			}
			InputOutputFunctions.ElectricityOutput(Current, SourceInstance, ComingFrom, this, Path);

			if (ControllingCable != null)
			{
				ElectricalSynchronisation.CableUpdates.Add(ControllingCable);
			}

		}*/

		//public void LineExplore(CableLine PassOn, HashSet<IntrinsicElectronicData> VisitedSteps)
		//{
		//	if (Data.connections.Count == 0)
		//	{
		//		FindPossibleConnections();
		//	}
		//	//Logger.Log("LineExplore > " + InData.Categorytype);
		//	if (Data.connections.Count < 3)
		//	{
		//		if (this.InData != PassOn.TheStart)
		//		{
		//			if (PassOn.TheEnd != null)
		//			{
		//				if (PassOn.TheEnd != this.InData)
		//				{
		//					VisitedSteps.Add(PassOn.TheEnd);
		//					PassOn.Covering.Add(PassOn.TheEnd);
		//					RelatedLine = PassOn;
		//					PassOn.TheEnd = this.InData;
		//				}
		//			}
		//			else {
		//				RelatedLine = PassOn;
		//				PassOn.TheEnd = this.InData;
		//			}
		//		}
		//		foreach (var OutConnectionIndata in Data.connections)
		//		{
		//			var OutConnection = OutConnectionIndata.Present;
		//			//Logger.Log("Data.connections >" + OutConnection.InData.Categorytype);
		//			if (!VisitedSteps.Contains(OutConnection.InData)
		//				&& !PassOn.Covering.Contains(OutConnection.InData)
		//				&& OutConnection.InData != PassOn.TheStart)
		//			{
		//				var wire = OutConnection as WireConnect;
		//				if (wire != null)
		//				{
		//					wire.LineExplore(PassOn, VisitedSteps);
		//				}
		//			}

		//		}

		//	}
		//}

		//public override void FlushConnectionAndUp()
		//{
		//	ElectricalDataCleanup.PowerSupplies.FlushConnectionAndUp(this);
		//	if (RelatedLine != null)
		//	{
		//		var _RelatedLine = RelatedLine;
		//		RelatedLine = null;
		//		_RelatedLine.FlushConnectionAndUp(this.InData);
		//		_RelatedLine.Kill();
		//	}

		//}
	}
}
