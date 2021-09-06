using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Systems.Electricity.NodeModules;

namespace Systems.Electricity
{
	[Serializable]
	public class IntrinsicElectronicData
	{
		public Connection WireEndB;
		public Connection WireEndA;

		public override string ToString()
		{
			if (Present != null)
			{
				return Present.name;
			}
			else
			{
				return (Categorytype.ToString());
			}
		}
		public PowerTypeCategory Categorytype;
		public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>();
		/// <summary>
		/// if the incoming input is from a certain type of  Machine/cable React differently
		/// </summary>
		public Dictionary<PowerTypeCategory, PowerInputReactions> ConnectionReaction = new Dictionary<PowerTypeCategory, PowerInputReactions>();
		public ElectricalNodeControl ControllingDevice;

		public ElectricalOIinheritance Present;
		public ElectricalMetaData MetaDataPresent;

		public ElectronicData Data = new ElectronicData();

		public bool DestroyQueueing = false;
		public bool DestroyAuthorised = false;

		public void SetDeadEnd()
		{
			Categorytype = PowerTypeCategory.DeadEndConnection;

		}

		public void SetUp(IntrinsicElectronicData indata)
		{
			Categorytype = indata.Categorytype;
			ConnectionReaction = indata.ConnectionReaction;
			Present = indata.Present;
			MetaDataPresent = indata.MetaDataPresent;
			CanConnectTo = indata.CanConnectTo;
			WireEndB = indata.WireEndB;
			WireEndA = indata.WireEndA;
		}

		public void SetUp(ElectricalCableTile electricalCableTile)
		{
			Categorytype = electricalCableTile.PowerType;
			CanConnectTo = new HashSet<PowerTypeCategory>(electricalCableTile.CanConnectTo);
			WireEndB = electricalCableTile.WireEndB;
			WireEndA = electricalCableTile.WireEndA;
		}

		public virtual void FindPossibleConnections()
		{
			if (MetaDataPresent != null)
			{
				MetaDataPresent.FindPossibleConnections();
			}
			else if (Present != null)
			{
				Present.FindPossibleConnections();
			}
		}

		/// <summary>
		///  Sets the upstream
		/// </summary>
		public virtual void DirectionInput(ElectricalOIinheritance SourceInstance, IntrinsicElectronicData ComingFrom, CableLine PassOn = null)
		{
			if (Present != null)
			{
				Present.DirectionInput(SourceInstance, ComingFrom);
			}
			else
			{
				InputOutputFunctions.DirectionInput(SourceInstance, ComingFrom, this);
			}
		}

		/// <summary>
		/// Sets the downstream and pokes the next one along
		/// </summary>
		public virtual void DirectionOutput(ElectricalOIinheritance SourceInstance)
		{
			if (Present != null)
			{
				Present.DirectionOutput(SourceInstance);
			}
			else
			{
				InputOutputFunctions.DirectionOutput(SourceInstance, this);
			}
		}

		/// <summary>
		/// Pass resistance with GameObject of the Machine it is heading toward
		/// </summary>
		public virtual void ResistanceInput(ResistanceWrap Resistance,
											ElectricalOIinheritance SourceInstance,
											IntrinsicElectronicData ComingFrom)
		{
			if (Present != null)
			{
				Present.ResistanceInput(Resistance, SourceInstance, ComingFrom);
			}
			else
			{
				InputOutputFunctions.ResistanceInput(Resistance, SourceInstance, ComingFrom, this);
			}
		}

		/// <summary>
		/// Passes it on to the next cable
		/// </summary>
		public virtual void ResistancyOutput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance)
		{
			if (Present != null)
			{
				Present.ResistancyOutput(Resistance, SourceInstance);
			}
			else
			{
				InputOutputFunctions.ResistancyOutput(Resistance, SourceInstance, this);
			}
		}

		/// <summary>
		/// Inputs a current from a device, with the supply
		/// </summary>
		public virtual void ElectricityInput(VIRCurrent Current,
											 ElectricalOIinheritance SourceInstance,
											 IntrinsicElectronicData ComingFrom)
		{
			if (Present != null)
			{
				Present.ElectricityInput(Current, SourceInstance, ComingFrom);
			}
			else
			{
				InputOutputFunctions.ElectricityInput(Current, SourceInstance, ComingFrom, this);
			}
		}

		/// <summary>
		///The function for out putting current into other nodes (Basically doing ElectricityInput On another one)
		/// </summary>
		public virtual void ElectricityOutput(VIRCurrent Current,
											  ElectricalOIinheritance SourceInstance)
		{
			if (Present != null)
			{
				Present.ElectricityOutput(Current, SourceInstance);
			}
			else
			{
				InputOutputFunctions.ElectricityOutput(Current, SourceInstance, this);
			}

		}

		public virtual void SetConnPoints(Connection DirectionEndin, Connection DirectionStartin)
		{
			WireEndA = DirectionEndin;
			WireEndB = DirectionStartin;
		}

		/// <summary>
		/// Flushs the connection and up. Flushes out everything
		/// </summary>
		public virtual void FlushConnectionAndUp()
		{
			ElectricalDataCleanup.PowerSupplies.FlushConnectionAndUp(this);
		}

		/// <summary>
		/// Flushs the resistance and up. Cleans out resistance and current, SourceInstance is the Gameobject Of the supply,
		/// This will be used to clean up the data from only a particular Supply
		/// </summary>
		public virtual void FlushResistanceAndUp(ElectricalOIinheritance SourceInstance = null)
		{
			ElectricalDataCleanup.PowerSupplies.FlushResistanceAndUp(this, SourceInstance);
		}

		/// <summary>
		/// Flushs the supply and up. Cleans out the current
		/// </summary>
		public virtual void FlushSupplyAndUp(ElectricalOIinheritance SourceInstance = null)
		{
			ElectricalDataCleanup.PowerSupplies.FlushSupplyAndUp(this, SourceInstance);
		}

		public virtual void RemoveSupply(ElectricalOIinheritance SourceInstance = null)
		{
			ElectricalDataCleanup.PowerSupplies.RemoveSupply(this, SourceInstance);
		}

		public virtual Vector2 GetLocation()
		{
			if (MetaDataPresent != null)
			{
				return (Vector2Int)MetaDataPresent.NodeLocation;
			}
			else if (Present != null)
			{
				return (Present.registerTile.LocalPosition.To2Int());
			}
			return (new Vector2());
		}

		public virtual ConnPoint GetConnPoints()
		{
			ConnPoint conns = new ConnPoint();
			conns.pointA = WireEndB;
			conns.pointB = WireEndA;
			return conns;
		}

		public string ShowInGameDetails()
		{
			ElectricityFunctions.WorkOutActualNumbers(this);
			return $"{Categorytype}: {Data.ActualVoltage.ToEngineering("V")}, {Data.CurrentInWire.ToEngineering("A")}";
		}

		public virtual string ShowDetails()
		{
			StringBuilder SB = new StringBuilder();
			ElectricityFunctions.WorkOutActualNumbers(this);
			SB.Append("connections " + (string.Join(",", Data.connections)));
			SB.Append("\n");
			SB.Append("Type " + (Categorytype.ToString()));
			SB.Append("\n");
			SB.Append("Can connect to " + (string.Join(",", CanConnectTo)));
			SB.Append("\n");
			SB.Append("WireEndA > " + WireEndA + " WireEndB > " + WireEndB);
			foreach (var Supply in Data.SupplyDependent)
			{

				SB.Append("Supply > " + Supply.Key + "\n");
				SB.Append("Upstream > ");
				SB.Append(string.Join(",", Supply.Value.Upstream) + "\n");
				SB.Append("Downstream > ");
				SB.Append(string.Join(",", Supply.Value.Downstream) + "\n");
				SB.Append("ResistanceGoingTo > ");
				SB.Append(string.Join(",", Supply.Value.ResistanceGoingTo) + "\n");
				SB.Append("ResistanceComingFrom > ");
				SB.Append(string.Join(",", Supply.Value.ResistanceComingFrom) + "\n");
				SB.Append( "CurrentComingFrom > ");
				SB.Append(string.Join(",", Supply.Value.CurrentComingFrom) + "\n");
				SB.Append("CurrentGoingTo > ");
				SB.Append(string.Join(",", Supply.Value.CurrentGoingTo) + "\n");
				SB.Append("SourceVoltages > ");
				SB.Append(string.Join(",", Supply.Value.SourceVoltage) + "\n");
			}
			SB.Append(" ActualVoltage > " + Data.ActualVoltage
			                              + " CurrentInWire > " + Data.CurrentInWire
			                              + " EstimatedResistance >  " + Data.EstimatedResistance);
			return SB.ToString();
		}

		public void DestroyThisPlease()
		{
			if (Present != null)
			{
				Present.DestroyThisPlease();
			}
			else
			{
				InternalDestroyThisPlease();
			}
		}

		private void InternalDestroyThisPlease()
		{
			DestroyQueueing = true;
			ElectricalManager.Instance.electricalSync.NUElectricalObjectsToDestroy.Add(this);
		}

		public void DestroyingThisNow()
		{
			if (Present != null)
			{
				Present.DestroyingThisNow();
				Present = null;
			}
			else
			{
				InternalDestroyingThisNow();
			}
		}

		private void InternalDestroyingThisNow()
		{
			if (DestroyQueueing)
			{
				FlushConnectionAndUp();
				FindPossibleConnections();
				FlushConnectionAndUp();
				MetaDataPresent.IsOn.ElectricalData.Remove(MetaDataPresent);
				ElectricalManager.Instance.electricalSync.StructureChange = true;
				MetaDataPresent.Locatedon.TileChangeManager.RemoveTile(MetaDataPresent.NodeLocation, LayerType.Underfloor);

			}
		}
	}
}
