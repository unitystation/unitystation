using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

public class WireConnect : ElectricalOIinheritance
{
	public CableInheritance ControllingCable;
	public CableLine RelatedLine;

	public MachineConnectorSpriteHandler SpriteHandler;
	//public override void OnStartServer()
	//{
	//	base.OnStartServer();
	//	StartCoroutine(WaitForLoad());
	//}

	//IEnumerator WaitForLoad()
	//{
	//	yield return WaitFor.Seconds(7);
	//	if (SpriteHandler != null)
	//	{
	//		SpriteHandler.Check();
	//	}
	//	FindPossibleConnections();
	//}

	public override void FindPossibleConnections()
	{
		base.FindPossibleConnections();
		if (SpriteHandler != null)
		{
			SpriteHandler.Check();
		}
	}

	public override void DirectionInput(GameObject SourceInstance, ElectricalOIinheritance ComingFrom,
		CableLine PassOn = null)
	{
		InputOutputFunctions.DirectionInput(SourceInstance, ComingFrom, this);
		if (PassOn == null)
		{
			if (RelatedLine != null)
			{
				//if (RelatedLine.TheEnd == this.GetComponent<ElectricalOIinheritance> ()) {
				//	//Logger.Log ("looc");
				//} else if (RelatedLine.TheStart == this.GetComponent<ElectricalOIinheritance> ()) {
				//	//Logger.Log ("cool");
				//} else {
				//	//Logger.Log ("hELP{!!!");
				//}
			}
			else
			{
				if (!(Data.connections.Count > 2))
				{
					RelatedLine = new CableLine();
					if (RelatedLine == null)
					{
						Logger.Log("DirectionInput: RelatedLine is null.", Category.Power);
					}

					RelatedLine.InitialGenerator = SourceInstance;
					RelatedLine.TheStart = this;
					lineExplore(RelatedLine, SourceInstance);
					if (RelatedLine.TheEnd == null)
					{
						RelatedLine = null;
					}
				}
			}
		}
	}

	public override void DirectionOutput(GameObject SourceInstance)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		//Logger.Log (this.gameObject.GetInstanceID().ToString() + " <ID | Downstream = "+Data.Downstream[SourceInstanceID].Count.ToString() + " Upstream = " + Data.Upstream[SourceInstanceID].Count.ToString () + this.name + " <  name! ", Category.Electrical);
		if (RelatedLine == null)
		{
			InputOutputFunctions.DirectionOutput(SourceInstance, this);
		}
		else
		{
			ElectricalOIinheritance GoingTo;
			if (RelatedLine.TheEnd == this.GetComponent<ElectricalOIinheritance>())
			{
				GoingTo = RelatedLine.TheStart;
			}
			else if (RelatedLine.TheStart == this.GetComponent<ElectricalOIinheritance>())
			{
				GoingTo = RelatedLine.TheEnd;
			}
			else
			{
				GoingTo = null;
				return;
			}

			if (!(Data.SupplyDependent[SourceInstanceID].Downstream.Contains(GoingTo) ||
			      Data.SupplyDependent[SourceInstanceID].Upstream.Contains(GoingTo)))
			{
				Data.SupplyDependent[SourceInstanceID].Downstream.Add(GoingTo);

				RelatedLine.DirectionInput(SourceInstance, this);
			}
			else
			{
				InputOutputFunctions.DirectionOutput(SourceInstance, this, RelatedLine);
			}
		}
	}

	public override void ElectricityOutput(float Current, GameObject SourceInstance)
	{
		//Logger.Log (Current.ToString () + " How much current", Category.Electrical);
		InputOutputFunctions.ElectricityOutput(Current, SourceInstance, this);
		if (ControllingCable != null)
		{
			ElectricalSynchronisation.CableUpdates.Add(ControllingCable);
		}
	}

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
						if (Data.SupplyDependent[SourceInstanceID].Upstream.Contains(Related))
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
	}

	public override void FlushResistanceAndUp(GameObject SourceInstance = null)
	{
		//TODO: yeham, Might need to work on in future but not used Currently
		ElectricalDataCleanup.PowerSupplies.FlushResistanceAndUp(this, SourceInstance);
	}

	public override void FlushSupplyAndUp(GameObject SourceInstance = null)
	{
		bool SendToline = false;
		if (RelatedLine != null)
		{
			if (SourceInstance == null)
			{
				foreach (var Supply in Data.SupplyDependent)
				{
					if (Supply.Value.CurrentComingFrom.Count > 0 || Supply.Value.CurrentGoingTo.Count > 0)
					{
						SendToline = true;
					}
				}
			}
			else
			{
				int InstanceID = SourceInstance.GetInstanceID();
				if (Data.SupplyDependent.ContainsKey(InstanceID))
				{
					if (Data.SupplyDependent[InstanceID].CurrentComingFrom.Count > 0)
					{
						SendToline = true;
					}
					else if (Data.SupplyDependent[InstanceID].CurrentGoingTo.Count > 0)
					{
						SendToline = true;
					}
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
				foreach (var Supply in Data.SupplyDependent)
				{
					if (Supply.Value.Downstream.Count > 0 || Supply.Value.Upstream.Count > 0)
					{
						SendToline = true;
					}
				}
			}
			else
			{
				int InstanceID = SourceInstance.GetInstanceID();
				if (Data.SupplyDependent[InstanceID].Downstream.Count > 0 ||
				    Data.SupplyDependent[InstanceID].Upstream.Count > 0)
				{
					SendToline = true;
				}
			}
		}

		ElectricalDataCleanup.PowerSupplies.RemoveSupply(this, SourceInstance);
		if (SendToline)
		{
			RelatedLine.PassOnRemoveSupply(this, SourceInstance);
		}
	}

	protected override void ShowDetails()
	{
		if (isServer)
		{
			ElectricityFunctions.WorkOutActualNumbers(this);
			Logger.Log("connections " + (string.Join(",", Data.connections)), Category.Electrical);
			Logger.Log("ID " + (this.GetInstanceID()), Category.Electrical);
			Logger.Log("Type " + (InData.Categorytype.ToString()), Category.Electrical);
			Logger.Log("Can connect to " + (string.Join(",", InData.CanConnectTo)), Category.Electrical);
			foreach (var Supply in Data.SupplyDependent)
			{
				LogSupply(Supply);
			}

			if (RelatedLine != null)
			{
				Logger.Log("line heree!!!");
				ElectricityFunctions.WorkOutActualNumbers(RelatedLine.TheEnd);
				Data.ActualVoltage = RelatedLine.TheEnd.Data.ActualVoltage;
				Data.CurrentInWire = RelatedLine.TheEnd.Data.CurrentInWire;
				Data.EstimatedResistance = RelatedLine.TheEnd.Data.EstimatedResistance;

				foreach (var Supply in RelatedLine.TheEnd.Data.SupplyDependent) LogSupply(Supply);
			}

			Logger.Log(
				" ActualVoltage > " + Data.ActualVoltage + " CurrentInWire > " + Data.CurrentInWire +
				" EstimatedResistance >  " + Data.EstimatedResistance, Category.Electrical);
		}

		RequestElectricalStats.Send(PlayerManager.LocalPlayer, gameObject);
	}

	private void LogSupply(KeyValuePair<int, ElectronicSupplyData> supply)
	{
		var sv = supply.Value;
		StringBuilder logMessage = new StringBuilder();
		logMessage.AppendLine("Supply > " + supply.Key);
		logMessage.Append("Upstream > ");
		logMessage.AppendLine(string.Join(",", sv.Upstream));
		logMessage.Append("Downstream > ");
		logMessage.AppendLine(string.Join(",", sv.Downstream));
		logMessage.Append("ResistanceGoingTo > ");
		logMessage.AppendLine(string.Join(",", sv.ResistanceGoingTo));
		logMessage.Append("ResistanceComingFrom > ");
		logMessage.AppendLine(string.Join(",", sv.ResistanceComingFrom));
		logMessage.Append("CurrentComingFrom > ");
		logMessage.AppendLine(string.Join(",", sv.CurrentComingFrom));
		logMessage.Append("CurrentGoingTo > ");
		logMessage.AppendLine(string.Join(",", sv.CurrentGoingTo));
		logMessage.Append(sv.SourceVoltages.ToString());
		Logger.Log(logMessage.ToString(), Category.Electrical);
	}
}