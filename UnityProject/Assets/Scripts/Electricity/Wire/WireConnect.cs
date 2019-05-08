using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class WireConnect : ElectricalOIinheritance
{
	public CableLine RelatedLine;

	public override void DirectionInput( GameObject SourceInstance, ElectricalOIinheritance ComingFrom, CableLine PassOn  = null){
		InputOutputFunctions.DirectionInput ( SourceInstance, ComingFrom, this);
		if (PassOn == null) {
			if (RelatedLine != null) {
				if (RelatedLine.TheEnd == this.GetComponent<ElectricalOIinheritance> ()) {
					//Logger.Log ("looc");
				} else if (RelatedLine.TheStart == this.GetComponent<ElectricalOIinheritance> ()) {
					//Logger.Log ("cool");
				} else {
					//Logger.Log ("hELP{!!!");
				}
			} else {
				if (!(Data.connections.Count > 2)) {
					RelatedLine = new CableLine ();
					if (RelatedLine == null) {
						Logger.Log ("HE:LP:::::::::::::::::::niniinininininin");
					}
					RelatedLine.InitialGenerator = SourceInstance;
					RelatedLine.TheStart = this;
					lineExplore (RelatedLine, SourceInstance);
					if (RelatedLine.TheEnd == null) {
						RelatedLine = null;
					}
				}
			}
		}
	}

	public override void DirectionOutput( GameObject SourceInstance){
		
		int SourceInstanceID = SourceInstance.GetInstanceID();
		//Logger.Log (this.gameObject.GetInstanceID().ToString() + " <ID | Downstream = "+Data.Downstream[SourceInstanceID].Count.ToString() + " Upstream = " + Data.Upstream[SourceInstanceID].Count.ToString () + this.name + " <  name! ", Category.Electrical);
		if (RelatedLine == null) {
			InputOutputFunctions.DirectionOutput (SourceInstance, this);
		} else {
			
			ElectricalOIinheritance GoingTo; 
			if (RelatedLine.TheEnd == this.GetComponent<ElectricalOIinheritance> ()) {
				GoingTo = RelatedLine.TheStart;
			} else if (RelatedLine.TheStart == this.GetComponent<ElectricalOIinheritance> ()) {
				GoingTo = RelatedLine.TheEnd;
			} else {
				GoingTo = null;
				return; 
			}

			if (!(Data.Upstream.ContainsKey(SourceInstanceID)))
			{
				Data.Upstream[SourceInstanceID] = new HashSet<ElectricalOIinheritance>();
			}
			if (!(Data.Downstream.ContainsKey(SourceInstanceID)))
			{
				Data.Downstream[SourceInstanceID] = new HashSet<ElectricalOIinheritance>();
			}

			if (GoingTo != null) {
				//Logger.Log ("to" + GoingTo.GameObject ().name);///wow
			}

			foreach (ElectricalOIinheritance bob in Data.Upstream[SourceInstanceID]){
				//Logger.Log("Upstream" + bob.GameObject ().name );
			}
			if (!(Data.Downstream [SourceInstanceID].Contains (GoingTo) || Data.Upstream [SourceInstanceID].Contains (GoingTo)) )  {
				Data.Downstream [SourceInstanceID].Add (GoingTo);

				RelatedLine.DirectionInput (SourceInstance, this);
			} else {
				InputOutputFunctions.DirectionOutput (SourceInstance, this,RelatedLine);
			}
		}
		Data.DownstreamCount = Data.Downstream[SourceInstanceID].Count;
		Data.UpstreamCount = Data.Upstream[SourceInstanceID].Count;
	}

	//public override void ElectricityOutput(float Current, GameObject SourceInstance)
	//{
	//	//Logger.Log (CurrentInWire.ToString () + " How much current", Category.Electrical);
	//	InputOutputFunctions.ElectricityOutput(Current, SourceInstance, this);
	//	Data.ActualCurrentChargeInWire = ElectricityFunctions.WorkOutActualNumbers(this);
	//	Data.CurrentInWire = Data.ActualCurrentChargeInWire.Current;
	//	Data.ActualVoltage = Data.ActualCurrentChargeInWire.Voltage;
	//	Data.EstimatedResistance = Data.ActualCurrentChargeInWire.EstimatedResistant;
	//	if (RelatedLine != null)
	//	{
	//		RelatedLine.UpdateCoveringCable(this);
	//	}
	//}

	//public void UpdateRelatedLine()
	//{
	//	if (RelatedLine != null)
	//	{
	//		RelatedLine.UpdateCoveringCable();

	//	}
	//}

	public void lineExplore(CableLine PassOn, GameObject SourceInstance = null)
	{

		if (Data.connections.Count <= 0)
		{
			FindPossibleConnections();
		}

		if (!(Data.connections.Count > 2))
		{
			RelatedLine = PassOn;
			if (!(this == PassOn.TheStart))
			{
				if (PassOn.TheEnd != null)
				{
					PassOn.Covering.Add(PassOn.TheEnd);
					PassOn.TheEnd = this;
				}
				else
				{
					PassOn.TheEnd = this;
				}
			}
			foreach (ElectricalOIinheritance Related in Data.connections)
			{
				if (!(RelatedLine.Covering.Contains(Related) || RelatedLine.TheStart == Related))
				{
					bool canpass = true;
					if (SourceInstance != null)
					{
						int SourceInstanceID = SourceInstance.GetInstanceID();
						if (Data.Upstream[SourceInstanceID].Contains(Related))
						{
							canpass = false;
						}
					}
					if (canpass)
					{
						if (Related.GameObject().GetComponent<WireConnect>() != null)
						{
							Related.GameObject().GetComponent<WireConnect>().lineExplore(RelatedLine);
						}
					}

				}
			}
		}
	}


	public override void FlushConnectionAndUp()
	{
		ElectricalDataCleanup.PowerSupplies.FlushConnectionAndUp(this);
		RelatedLine = null;
		//InData.ControllingDevice.PotentialDestroyed();
	}
	public override void FlushResistanceAndUp(GameObject SourceInstance = null)
	{
		Logger.LogError ("yes? not ues haha lol no");
		//yeham, Might need to work on in future but not used Currently
		ElectricalDataCleanup.PowerSupplies.FlushResistanceAndUp(this, SourceInstance);
	}
	public override void FlushSupplyAndUp(GameObject SourceInstance = null)
	{
		bool SendToline = false;
		if (RelatedLine != null)
		{
			if (SourceInstance == null)
			{
				if (Data.CurrentComingFrom.Count > 0)
				{
					SendToline = true;
				}
			}
			else
			{
				int InstanceID = SourceInstance.GetInstanceID();
				if (Data.CurrentComingFrom.ContainsKey(InstanceID))
				{
					SendToline = true;
				}
				else if (Data.CurrentGoingTo.ContainsKey(InstanceID))
				{
					SendToline = true;
				}
			}
		}
		ElectricalDataCleanup.PowerSupplies.FlushSupplyAndUp(this, SourceInstance);
		if (SendToline)
		{
			RelatedLine.PassOnFlushSupplyAndUp(this, SourceInstance);
		}
	}
	public override void RemoveSupply(GameObject SourceInstance = null)
	{
		bool SendToline = false;
		if (RelatedLine != null)
		{
			if (SourceInstance == null)
			{
				if (Data.Downstream.Count > 0 || Data.Upstream.Count > 0)
				{
					SendToline = true;
				}
			}
			else
			{
				int InstanceID = SourceInstance.GetInstanceID();
				if (Data.Downstream.ContainsKey(InstanceID))
				{
					SendToline = true;
				}
			}
		}
		ElectricalDataCleanup.PowerSupplies.RemoveSupply(this, SourceInstance);
		if (SendToline){
			RelatedLine.PassOnRemoveSupply (this, SourceInstance);
		}
	}
	[ContextMethod("Details", "Magnifying_glass")]
	public override void ShowDetails()
	{
		if (isServer)
		{
			Logger.Log("connections " + (Data.connections.Count.ToString()), Category.Electrical);
			Logger.Log("ID " + (this.GetInstanceID()), Category.Electrical);
			Logger.Log("Type " + (InData.Categorytype.ToString()), Category.Electrical);
			Logger.Log("Can connect to " + (string.Join(",", InData.CanConnectTo)), Category.Electrical);
			Logger.Log("UpstreamCount " + (Data.UpstreamCount.ToString()), Category.Electrical);
			Logger.Log("DownstreamCount " + (Data.DownstreamCount.ToString()), Category.Electrical);
			if (RelatedLine != null)
			{
				Logger.Log("line heree!!!");
				//RelatedLine.UpdateCoveringCable();
				Data.ActualVoltage = RelatedLine.TheEnd.Data.ActualVoltage;
				Data.CurrentInWire = RelatedLine.TheEnd.Data.CurrentInWire;
				Data.EstimatedResistance = RelatedLine.TheEnd.Data.EstimatedResistance;
			}

			Logger.Log("ActualVoltage " + (Data.ActualVoltage.ToString()), Category.Electrical);
			Logger.Log("CurrentInWire " + (Data.CurrentInWire.ToString()), Category.Electrical);
			Logger.Log("EstimatedResistance " + (Data.EstimatedResistance.ToString()), Category.Electrical);
			


		}

		RequestElectricalStats.Send(PlayerManager.LocalPlayer, gameObject);
	}
}