using System;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using Mirror;
using Messages.Client;

namespace Systems.Electricity
{
	///<summary> Is the base class that every node inherits from </summary>
	[Serializable]
	public class ElectricalOIinheritance : NetworkBehaviour, IServerDespawn
	{

		[NonSerialized] public IntrinsicElectronicData InData = new IntrinsicElectronicData();
		[NonSerialized] public HashSet<IntrinsicElectronicData> connectedDevices = new HashSet<IntrinsicElectronicData>();

		public RegisterTile registerTile;
		public Matrix Matrix => registerTile.Matrix;
		public bool connected;

		public bool Logall;

		private void Start()
		{
			EnsureInit();
		}

		private void EnsureInit()
		{
			if (registerTile == null)
			{
				registerTile = GetComponent<RegisterTile>();
			}
			if (registerTile == null)
			{
				Loggy.LogError("Confused screaming! > " + this.name, Category.Electrical);
			}
			else
			{
				registerTile.SetElectricalData(this);
			}
			ElectricalManager.Instance.electricalSync.StructureChange = true;
			InData.Present = this;
		}

		public override void OnStartClient()
		{
			EnsureInit();
		}

		public override void OnStartServer()
		{
			EnsureInit();
			base.OnStartServer();

		}

		public virtual void FindPossibleConnections()
		{
			InData.Data.connections.Clear();
			if (registerTile != null)
			{
				ElectricityFunctions.FindPossibleConnections(
				   Matrix,
				   InData.CanConnectTo,
				   GetConnPoints(),
				   InData,
				   InData.Data.connections
			   );
			}
		}

		public virtual ConnPoint GetConnPoints()
		{
			return (InData.GetConnPoints());
		}

		/// <summary>
		/// Sets the downstream and pokes the next one along
		/// </summary>
		public virtual void DirectionOutput(ElectricalOIinheritance SourceInstance)
		{

			InputOutputFunctions.DirectionOutput(SourceInstance, InData);
		}

		///// <summary>
		/////  Sets the upstream
		///// </summary>
		public virtual void DirectionInput(ElectricalOIinheritance SourceInstance, IntrinsicElectronicData ComingFrom, CableLine PassOn = null)
		{
			if (Logall)
			{
				Loggy.Log("this > " + this + "DirectionInput SourceInstance > " + SourceInstance + " ComingFrom > " + ComingFrom + "  PassOn > " + PassOn, Category.Electrical);
			}
			InputOutputFunctions.DirectionInput(SourceInstance, ComingFrom, InData);
		}



		/// <summary>
		/// Pass resistance with GameObject of the Machine it is heading toward
		/// </summary>
		public virtual void ResistanceInput(ResistanceWrap Resistance,
											ElectricalOIinheritance SourceInstance,
											IntrinsicElectronicData ComingFrom)
		{
			if (Logall)
			{
				Loggy.Log("this > " + this
						   + "ResistanceInput, Resistance > " + Resistance
						   + " SourceInstance  > " + SourceInstance
						   + " ComingFrom > " + ComingFrom, Category.Electrical);
			}
			InputOutputFunctions.ResistanceInput(Resistance, SourceInstance, ComingFrom, InData);
		}

		/// <summary>
		/// Passes it on to the next cable
		/// </summary>
		public virtual void ResistancyOutput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance)
		{
			if (Logall)
			{
				Loggy.Log("this > " + this
						   + "ResistancyOutput, Resistance > " + Resistance
						   + " SourceInstance  > " + SourceInstance, Category.Electrical);
			}
			InputOutputFunctions.ResistancyOutput(Resistance, SourceInstance, InData);
		}

		/// <summary>
		/// Inputs a current from a device, with the supply
		/// </summary>
		public virtual void ElectricityInput(VIRCurrent Current,
											 ElectricalOIinheritance SourceInstance,
											 IntrinsicElectronicData ComingFrom)
		{
			if (Logall)
			{
				Loggy.Log("this > " + this
						   + "ElectricityInput, Current > " + Current
						   + " SourceInstance  > " + SourceInstance
						   + " ComingFrom > " + ComingFrom, Category.Electrical);
			}
			InputOutputFunctions.ElectricityInput(Current, SourceInstance, ComingFrom, InData);
		}

		/// <summary>
		///The function for out putting current into other nodes (Basically doing ElectricityInput On another one)
		/// </summary>
		public virtual void ElectricityOutput(VIRCurrent Current,
											  ElectricalOIinheritance SourceInstance)
		{
			if (Logall)
			{
				Loggy.Log("this > " + this
						   + "ElectricityOutput, Current > " + Current
						   + " SourceInstance  > " + SourceInstance, Category.Electrical);
			}
			InputOutputFunctions.ElectricityOutput(Current, SourceInstance, InData);

		}

		public virtual void SetConnPoints(Connection DirectionEndin, Connection DirectionStartin)
		{
			InData.WireEndA = DirectionEndin;
			InData.WireEndB = DirectionStartin;
		}

		[RightClickMethod]
		public virtual void ShowDetails()
		{
			if (isServer)
			{
				InData.ShowDetails();
			}

			RequestElectricalStats.Send(PlayerManager.LocalPlayerObject, gameObject);
		}

		public void DestroyThisPlease()
		{
			InData.DestroyQueueing = true;
			ElectricalManager.Instance.electricalSync.NUElectricalObjectsToDestroy.Add(InData);
		}

		public void DestroyingThisNow()
		{
			if (InData.DestroyQueueing)
			{
				InData.FlushConnectionAndUp();
				FindPossibleConnections();
				InData.FlushConnectionAndUp();

				registerTile.UnregisterClient();
				registerTile.UnregisterServer();
				if (this != null)
				{
					ElectricalManager.Instance.electricalSync.StructureChange = true;
					InData.DestroyAuthorised = true;
					_ = Despawn.ServerSingle(gameObject);
				}
			}
		}

		/// <summary>
		/// is the function to denote that it will be pooled or destroyed immediately after this function is finished, Used for cleaning up anything that needs to be cleaned up before this happens
		/// </summary>
		public void OnDespawnServer(DespawnInfo info)
		{
			if (!InData.DestroyQueueing || !InData.DestroyAuthorised)
			{
				Loggy.Log("REEEEEEEEEEEEEE Wait your turn to destroy, Electrical thread is busy!!", Category.Electrical);
				DestroyThisPlease();
				var yy = InData.ConnectionReaction[PowerTypeCategory.Transformer];
			}
		}

		[RightClickMethod]
		public void StructureChange()
		{
			ElectricalManager.Instance.electricalSync.StructureChange = true;
		}
	}
}
